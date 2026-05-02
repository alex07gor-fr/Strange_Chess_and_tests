using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// Простая консольная шахматная игра с базовой валидацией ходов,
// рокировкой, en-passant, превращением пешки, проверкой шаха/мата/пата.

enum PieceType { Pawn, Knight, Bishop, Rook, Queen, King }
record Piece(PieceType Type, bool IsWhite, bool HasMoved = false);

struct Pos
{
    public int X; // 0..7 for a..h
    public int Y; // 0..7 for 1..8 (0 => rank1)
    public Pos(int x, int y) { X = x; Y = y; }
    public static bool TryParse(string s, out Pos pos)
    {
        pos = default;
        if (string.IsNullOrEmpty(s) || s.Length < 2) return false;
        s = s.Trim().ToLowerInvariant();
        char file = s[0];
        char rank = s[1];
        if (file < 'a' || file > 'h') return false;
        if (rank < '1' || rank > '8') return false;
        int x = file - 'a';
        int y = rank - '1';
        pos = new Pos(x, y);
        return true;
    }
    public override string ToString() => $"{(char)('a' + X)}{Y + 1}";
    public bool InBounds() => X >= 0 && X < 8 && Y >= 0 && Y < 8;
}

class Board
{
    public Piece?[,] Cells = new Piece?[8, 8];
    public (Pos From, Pos To, Piece PieceMoved)? LastMove = null; // для en-passant и истории
    public Board() { }
    public Board Clone()
    {
        var b = new Board();
        for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++) b.Cells[x, y] = Cells[x, y];
        b.LastMove = LastMove;
        return b;
    }

    public void InitStart()
    {
        // Clear
        Cells = new Piece?[8, 8];

        // White pawns
        for (int x = 0; x < 8; x++) Cells[x, 1] = new Piece(PieceType.Pawn, true, false);
        // Black pawns
        for (int x = 0; x < 8; x++) Cells[x, 6] = new Piece(PieceType.Pawn, false, false);

        // Rooks
        Cells[0, 0] = new Piece(PieceType.Rook, true, false);
        Cells[7, 0] = new Piece(PieceType.Rook, true, false);
        Cells[0, 7] = new Piece(PieceType.Rook, false, false);
        Cells[7, 7] = new Piece(PieceType.Rook, false, false);

        // Knights
        Cells[1, 0] = new Piece(PieceType.Knight, true, false);
        Cells[6, 0] = new Piece(PieceType.Knight, true, false);
        Cells[1, 7] = new Piece(PieceType.Knight, false, false);
        Cells[6, 7] = new Piece(PieceType.Knight, false, false);

        // Bishops
        Cells[2, 0] = new Piece(PieceType.Bishop, true, false);
        Cells[5, 0] = new Piece(PieceType.Bishop, true, false);
        Cells[2, 7] = new Piece(PieceType.Bishop, false, false);
        Cells[5, 7] = new Piece(PieceType.Bishop, false, false);

        // Queens
        Cells[3, 0] = new Piece(PieceType.Queen, true, false);
        Cells[3, 7] = new Piece(PieceType.Queen, false, false);

        // Kings
        Cells[4, 0] = new Piece(PieceType.King, true, false);
        Cells[4, 7] = new Piece(PieceType.King, false, false);
    }

    public Piece? Get(Pos p) => Cells[p.X, p.Y];
    public void Set(Pos p, Piece? piece) => Cells[p.X, p.Y] = piece;
}

static class Renderer
{
    static readonly string[,] Uni = new string[2, 6]
    {
        // white: P N B R Q K
        { "Pw","Nw","Bw","Rw","Qw","Kw"},
        // black
        { "Pb","Nb","Bb","Rb","Qb","Kb"}
    };

    public static void Render(Board board)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Clear();
        for (int rank = 7; rank >= 0; rank--)
        {
            Console.Write($"{rank + 1} ");
            for (int file = 0; file < 8; file++)
            {
                var p = board.Cells[file, rank];
                if (p == null) Console.Write(". ");
                else
                {
                    int color = p.IsWhite ? 0 : 1;
                    int idx = p.Type switch
                    {
                        PieceType.Pawn => 0,
                        PieceType.Knight => 1,
                        PieceType.Bishop => 2,
                        PieceType.Rook => 3,
                        PieceType.Queen => 4,
                        PieceType.King => 5,
                        _ => 0
                    };
                    Console.Write(Uni[color, idx] + " ");
                }
            }
            Console.WriteLine();
        }
        Console.WriteLine("  a b c d e f g h");
    }
}

record Move(Pos From, Pos To, Piece? Promotion = null);

class Game
{
    public Board Board = new Board();
    public bool WhiteToMove = true;

    public Game()
    {
        Board.InitStart();
    }

    // Возвращает true, если клетка атакована цветом isWhiteAttacker
    public bool IsSquareAttacked(Board b, Pos square, bool byWhite)
    {
        // Пешки атакуют по диагонали
        int dir = byWhite ? +1 : -1;
        foreach (var dx in new[] { -1, 1 })
        {
            var p = new Pos(square.X - dx, square.Y - dir);
            if (p.InBounds())
            {
                var pc = b.Get(p);
                if (pc != null && pc.IsWhite == byWhite && pc.Type == PieceType.Pawn) return true;
            }
        }

        // Knights
        int[] kdx = { -2, -2, -1, -1, 1, 1, 2, 2 };
        int[] kdy = { -1, 1, -2, 2, -2, 2, -1, 1 };
        for (int i = 0; i < 8; i++)
        {
            var p = new Pos(square.X + kdx[i], square.Y + kdy[i]);
            if (!p.InBounds()) continue;
            var pc = b.Get(p);
            if (pc != null && pc.IsWhite == byWhite && pc.Type == PieceType.Knight) return true;
        }

        // Sliding pieces: rook/queen (orthogonal), bishop/queen (diagonal)
        (int dx, int dy)[] dirs = { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
        foreach (var (dx2, dy2) in dirs)
        {
            var p = new Pos(square.X + dx2, square.Y + dy2);
            while (p.InBounds())
            {
                var pc = b.Get(p);
                if (pc != null)
                {
                    if (pc.IsWhite == byWhite)
                    {
                        if (dx2 == 0 || dy2 == 0)
                        {
                            if (pc.Type == PieceType.Rook || pc.Type == PieceType.Queen) return true;
                        }
                        if (dx2 != 0 && dy2 != 0)
                        {
                            if (pc.Type == PieceType.Bishop || pc.Type == PieceType.Queen) return true;
                        }
                        // king one-step attack (adjacent)
                        if (Math.Abs(p.X - square.X) <= 1 && Math.Abs(p.Y - square.Y) <= 1 && pc.Type == PieceType.King) return true;
                    }
                    break; // blocked
                }
                p = new Pos(p.X + dx2, p.Y + dy2);
            }
        }

        return false;
    }

    // Получить позицию короля данного цвета
    public Pos? FindKing(Board b, bool white)
    {
        for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++)
            {
                var pc = b.Cells[x, y];
                if (pc != null && pc.Type == PieceType.King && pc.IsWhite == white) return new Pos(x, y);
            }
        return null;
    }

    // Возвращает true, если цвет white находится под шахом
    public bool IsInCheck(Board b, bool white)
    {
        var kingPos = FindKing(b, white);
        if (kingPos == null) return false; // нет короля — странная позиция
        return IsSquareAttacked(b, kingPos.Value, !white);
    }

    // Генерация всех легальных ходов для игрока white
    public List<Move> GenerateAllLegalMoves(Board b, bool white)
    {
        var moves = new List<Move>();
        for (int x = 0; x < 8; x++) for (int y = 0; y < 8; y++)
            {
                var from = new Pos(x, y);
                var pc = b.Get(from);
                if (pc == null || pc.IsWhite != white) continue;
                foreach (var move in GeneratePseudoLegalMoves(b, from))
                {
                    // симуляция хода и проверка, не остается ли король под шахом
                    var nb = b.Clone();
                    ApplyMove(nb, move, simulate: true);
                    if (!IsInCheck(nb, white))
                        moves.Add(move);
                }
            }
        return moves;
    }

    // Генерация псевдозаконных ходов (без проверки шаха)
    public List<Move> GeneratePseudoLegalMoves(Board b, Pos from)
    {
        var res = new List<Move>();
        var pc = b.Get(from);
        if (pc == null) return res;
        bool white = pc.IsWhite;

        int dir = white ? +1 : -1; // направление пешек по y

        if (pc.Type == PieceType.Pawn)
        {
            // forward 1
            var one = new Pos(from.X, from.Y + dir);
            if (one.InBounds() && b.Get(one) == null)
            {
                // promotion?
                if ((white && one.Y == 7) || (!white && one.Y == 0))
                {
                    // добавим варианты преобразования (Queen default)
                    res.Add(new Move(from, one, new Piece(PieceType.Queen, white, true)));
                    res.Add(new Move(from, one, new Piece(PieceType.Rook, white, true)));
                    res.Add(new Move(from, one, new Piece(PieceType.Bishop, white, true)));
                    res.Add(new Move(from, one, new Piece(PieceType.Knight, white, true)));
                }
                else
                    res.Add(new Move(from, one));
                // forward 2
                bool startRank = (white && from.Y == 1) || (!white && from.Y == 6);
                var two = new Pos(from.X, from.Y + 2 * dir);
                if (startRank && two.InBounds() && b.Get(two) == null)
                {
                    res.Add(new Move(from, two));
                }
            }

            // captures
            foreach (int dx in new[] { -1, 1 })
            {
                var to = new Pos(from.X + dx, from.Y + dir);
                if (!to.InBounds()) continue;
                var target = b.Get(to);
                if (target != null && target.IsWhite != white)
                {
                    if ((white && to.Y == 7) || (!white && to.Y == 0))
                    {
                        res.Add(new Move(from, to, new Piece(PieceType.Queen, white, true)));
                        res.Add(new Move(from, to, new Piece(PieceType.Rook, white, true)));
                        res.Add(new Move(from, to, new Piece(PieceType.Bishop, white, true)));
                        res.Add(new Move(from, to, new Piece(PieceType.Knight, white, true)));
                    }
                    else res.Add(new Move(from, to));
                }

                // en-passant
                if (b.LastMove != null && b.LastMove.Value.PieceMoved.Type == PieceType.Pawn)
                {
                    var last = b.LastMove.Value;
                    // last moved pawn did 2 steps if abs(from.y - last.To.y) == 0 and abs(last.From.y - last.To.y) == 2
                    if (Math.Abs(last.From.Y - last.To.Y) == 2 && last.To.Y == from.Y && Math.Abs(last.To.X - from.X) == 1)
                    {
                        // target square is the square the pawn passed through:
                        int passedY = last.To.Y + (last.PieceMoved.IsWhite ? -1 : +1);
                        var ep = new Pos(last.To.X, passedY);
                        if (ep.InBounds() && ep.X == to.X && ep.Y == to.Y && b.Get(ep) == null)
                        {
                            res.Add(new Move(from, ep)); // capture en-passant; removal handled in ApplyMove
                        }
                    }
                }
            }
        }
        else if (pc.Type == PieceType.Knight)
        {
            int[] kdx = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] kdy = { -1, 1, -2, 2, -2, 2, -1, 1 };
            for (int i = 0; i < 8; i++)
            {
                var to = new Pos(from.X + kdx[i], from.Y + kdy[i]);
                if (!to.InBounds()) continue;
                var t = b.Get(to);
                if (t == null || t.IsWhite != white) res.Add(new Move(from, to));
            }
        }
        else if (pc.Type == PieceType.Bishop || pc.Type == PieceType.Rook || pc.Type == PieceType.Queen)
        {
            (int dx, int dy)[] dirs;
            if (pc.Type == PieceType.Bishop) dirs = new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) };
            else if (pc.Type == PieceType.Rook) dirs = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            else dirs = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };

            foreach (var (dx, dy) in dirs)
            {
                var to = new Pos(from.X + dx, from.Y + dy);
                while (to.InBounds())
                {
                    var t = b.Get(to);
                    if (t == null)
                    {
                        res.Add(new Move(from, to));
                    }
                    else
                    {
                        if (t.IsWhite != white) res.Add(new Move(from, to));
                        break;
                    }
                    to = new Pos(to.X + dx, to.Y + dy);
                }
            }
        }
        else if (pc.Type == PieceType.King)
        {
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var to = new Pos(from.X + dx, from.Y + dy);
                    if (!to.InBounds()) continue;
                    var t = b.Get(to);
                    if (t == null || t.IsWhite != white) res.Add(new Move(from, to));
                }

            // castling: king must not have moved
            if (!pc.HasMoved)
            {
                // king-side rook at x=7
                if (CanCastle(b, from, true)) res.Add(new Move(from, new Pos(from.X + 2, from.Y)));
                if (CanCastle(b, from, false)) res.Add(new Move(from, new Pos(from.X - 2, from.Y)));
            }
        }

        return res;
    }

    // Проверка условий для рокировки (без проверки шаха в текущей позиции — в GenerateAllLegalMoves движение короля будет дополнительно проверки на шах)
    bool CanCastle(Board b, Pos kingPos, bool kingSide)
    {
        var king = b.Get(kingPos);
        if (king == null || king.Type != PieceType.King || king.HasMoved) return false;
        int rookX = kingSide ? 7 : 0;
        var rook = b.Get(new Pos(rookX, kingPos.Y));
        if (rook == null || rook.Type != PieceType.Rook || rook.IsWhite != king.IsWhite || rook.HasMoved) return false;

        int dir = kingSide ? 1 : -1;
        // cells between king and rook must be empty
        for (int x = kingPos.X + dir; kingSide ? x < rookX : x > rookX; x += dir)
        {
            if (b.Get(new Pos(x, kingPos.Y)) != null) return false;
        }

        // king cannot be in check, and cannot pass through or land on attacked square
        if (IsInCheck(b, king.IsWhite)) return false;
        // check squares king passes through
        for (int step = 1; step <= 2; step++)
        {
            var checkPos = new Pos(kingPos.X + dir * step, kingPos.Y);
            // temporarily move king to that square and test attack
            var nb = b.Clone();
            nb.Set(kingPos, null);
            nb.Set(checkPos, king with { HasMoved = true });
            if (IsSquareAttacked(nb, checkPos, !king.IsWhite)) return false;
        }

        return true;
    }

    // Применить ход (если simulate == true, не обновлять LastMove для истории, но мы всё равно обновим, потому что симуляция может зависеть от него — в этом коде будем обновлять LastMove)
    public void ApplyMove(Board b, Move move, bool simulate = false)
    {
        var piece = b.Get(move.From);
        if (piece == null) return;
        // castling
        if (piece.Type == PieceType.King && Math.Abs(move.To.X - move.From.X) == 2)
        {
            int dir = move.To.X > move.From.X ? 1 : -1;
            // move king
            b.Set(move.To, piece with { HasMoved = true });
            b.Set(move.From, null);
            // move rook: from either 7 or 0 to the square next to king
            int rookFromX = dir == 1 ? 7 : 0;
            int rookToX = move.To.X - dir;
            var rook = b.Get(new Pos(rookFromX, move.From.Y));
            b.Set(new Pos(rookToX, move.From.Y), rook is not null ? rook with { HasMoved = true } : null);
            b.Set(new Pos(rookFromX, move.From.Y), null);
            b.LastMove = (move.From, move.To, piece with { HasMoved = true });
            return;
        }

        // en-passant capture: if pawn moves to an empty square diagonally and last move was opponent pawn two-step adjacent
        if (piece.Type == PieceType.Pawn)
        {
            if (move.To.X != move.From.X && b.Get(move.To) == null)
            {
                // capture en-passant: remove pawn at last move's destination
                if (b.LastMove != null && b.LastMove.Value.PieceMoved.Type == PieceType.Pawn)
                {
                    var last = b.LastMove.Value;
                    if (Math.Abs(last.From.Y - last.To.Y) == 2 && last.To.X == move.To.X && last.To.Y == move.From.Y)
                    {
                        // remove
                        b.Set(last.To, null);
                    }
                }
            }
        }

        // normal capture / move
        Piece toSet;
        if (move.Promotion != null)
            toSet = move.Promotion with { HasMoved = true };
        else
            toSet = piece with { HasMoved = true };

        b.Set(move.To, toSet);
        b.Set(move.From, null);
        b.LastMove = (move.From, move.To, piece with { HasMoved = true });
    }

    // Основной цикл игры
    public void Run()
    {
        while (true)
        {
            Renderer.Render(Board);
            Console.WriteLine(WhiteToMove ? "Ход белых" : "Ход чёрных");

            // проверим состояние
            bool inCheck = IsInCheck(Board, WhiteToMove);
            var legal = GenerateAllLegalMoves(Board, WhiteToMove);
            if (legal.Count == 0)
            {
                if (inCheck)
                {
                    Console.WriteLine(WhiteToMove ? "Мат. Победили чёрные." : "Мат. Победили белые.");
                }
                else
                {
                    Console.WriteLine("Пат.");
                }
                Console.WriteLine("Нажмите Enter чтобы выйти.");
                Console.ReadLine();
                break;
            }
            else if (inCheck)
            {
                Console.WriteLine("Вы под шахом!");
            }

            Console.Write("Введите ход (пример e2e4, e7e8=Q, e1g1 для рокировки) или 'exit': ");
            var line = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            // простой парсер: поддерживает e2e4, e2 e4, e2-e4, или e7e8=Q
            var cleaned = line.Replace("-", " ").Replace("=", " ").Replace(",", " ").Replace("->", " ").Trim();
            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string moveStr = parts.Length == 1 ? parts[0] : (parts.Length >= 2 ? parts[0] + parts[1] : parts[0]);

            // support notation like e7e8q (promotion), or e7e8=Q handled above
            string promoPart = null;
            if (moveStr.Length >= 5)
            {
                // e7e8Q or e7e8=q
                promoPart = moveStr.Substring(4).Trim();
                moveStr = moveStr.Substring(0, 4);
            }

            if (moveStr.Length != 4)
            {
                Console.WriteLine("Неправильный формат хода.");
                Console.WriteLine("Примеры: e2e4, e7e8Q, e1g1");
                Console.WriteLine("Нажмите Enter чтобы продолжить.");
                Console.ReadLine();
                continue;
            }

            if (!Pos.TryParse(moveStr.Substring(0, 2), out var from) || !Pos.TryParse(moveStr.Substring(2, 2), out var to))
            {
                Console.WriteLine("Ошибка разбора координат.");
                Console.ReadLine();
                continue;
            }

            var piece = Board.Get(from);
            if (piece == null) { Console.WriteLine("Нет фигуры в указанной клетке."); Console.ReadLine(); continue; }
            if (piece.IsWhite != WhiteToMove) { Console.WriteLine("Не ваш цвет."); Console.ReadLine(); continue; }

            // попробуем сопоставить с легальными ходами
            var possible = GenerateAllLegalMoves(Board, WhiteToMove)
                .Where(m => m.From.X == from.X && m.From.Y == from.Y && m.To.X == to.X && m.To.Y == to.Y)
                .ToList();

            if (possible.Count == 0) { Console.WriteLine("Ход не легален."); Console.ReadLine(); continue; }

            Move chosen;
            if (possible.Count == 1)
            {
                chosen = possible[0];
            }
            else
            {
                // несколько вариантов — обычно происходит при промоции (разные фигуры)
                if (!string.IsNullOrEmpty(promoPart))
                {
                    char pch = promoPart.ToUpperInvariant()[0];
                    PieceType pt = pch switch { 'Q' => PieceType.Queen, 'R' => PieceType.Rook, 'B' => PieceType.Bishop, 'N' => PieceType.Knight, _ => PieceType.Queen };
                    var found = possible.FirstOrDefault(m => m.Promotion != null && m.Promotion.Type == pt);
                    if (found == null) chosen = possible[0]; else chosen = found;
                }
                else
                {
                    // попросим пользователя выбрать
                    Console.WriteLine("Выберите вариант промоции:");
                    for (int i = 0; i < possible.Count; i++)
                    {
                        var m = possible[i];
                        Console.WriteLine($"{i + 1}: {m.From}->{m.To} {(m.Promotion != null ? m.Promotion.Type.ToString() : "")}");
                    }
                    Console.Write("Номер: ");
                    if (!int.TryParse(Console.ReadLine(), out int idx) || idx < 1 || idx > possible.Count) { Console.WriteLine("Отменено."); Console.ReadLine(); continue; }
                    chosen = possible[idx - 1];
                }
            }

            // если продвижение пешки без явной промоции — попросим пользователя, если нужно
            if (chosen.Promotion != null && (chosen.Promotion.Type == PieceType.Queen || chosen.Promotion.Type == PieceType.Rook || chosen.Promotion.Type == PieceType.Bishop || chosen.Promotion.Type == PieceType.Knight))
            {
                // промоция уже определена в Move (возможны варианты)
            }

            // применяем ход
            ApplyMove(Board, chosen);
            WhiteToMove = !WhiteToMove;
        }
    }
}

public class Program
{
    public static void Main()
    {
        var game = new Game();
        game.Run();
    }
}