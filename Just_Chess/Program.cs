using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace ChessExample;

public class CheckerBoardPosition(byte x, byte y) : IParsable<CheckerBoardPosition>
{
    [AllowedValues(1, 2, 3, 4, 5, 6, 7, 8)]
    public byte X { get; } = x is > 0 and <= 8 ? x : throw new ArgumentOutOfRangeException(nameof(x));

    [AllowedValues(1, 2, 3, 4, 5, 6, 7, 8)]
    public byte Y { get; } = y is > 0 and <= 8 ? y : throw new ArgumentOutOfRangeException(nameof(y));

    private const char LetterOffset = '@'; // 'A' - 1
    public char XLetter => (char)(LetterOffset + X);

    public override string ToString() => $"{XLetter}{Y}";

    public static CheckerBoardPosition Parse(string s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
            ? result
            : throw new FormatException($"Invalid {nameof(CheckerBoardPosition)}: {s}");

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [NotNullWhen(true)] out CheckerBoardPosition? result)
    {
        if (s is [var x and >= 'A' and <= 'H', var y and >= '1' and <= '8'])
        {
            result = new CheckerBoardPosition((byte)(x - LetterOffset), byte.Parse([y]));
            return true;
        }

        result = null;
        return false;
    }
}
// общ. прав. фигур
public abstract class ChessFigure
{
    public string Name { get; }

    protected ChessFigure(string name) => Name = name;

    public abstract bool IsValidMove(CheckerBoardPosition from, CheckerBoardPosition to);
}

// фигуры
public class Pawn : ChessFigure
{
    public bool IsWhite { get; }

    public Pawn(bool isWhite) : base("Пешка") => IsWhite = isWhite;

    public override bool IsValidMove(CheckerBoardPosition from, CheckerBoardPosition to)
    {
        int dx = Math.Abs(from.X - to.X);
        int dySigned = to.Y - from.Y;

        if (IsWhite)
        {
            if (dySigned <= 0) return false;
            if (dySigned == 1) return true;
            if (dySigned == 2 && from.Y == 2) return true;
            return false;
        }
        else
        {
            if (dySigned >= 0) return false;
            if (dySigned == -1) return true;
            if (dySigned == -2 && from.Y == 7) return true;
            return false;
        }
    }
}

public class Rook : ChessFigure
{
    public Rook() : base("Ладья") { }

    public override bool IsValidMove(CheckerBoardPosition from, CheckerBoardPosition to)
        => from.X == to.X || from.Y == to.Y;
}

public class Bishop : ChessFigure
{
    public Bishop() : base("Слон") { }

    public override bool IsValidMove(CheckerBoardPosition from, CheckerBoardPosition to)
        => Math.Abs(from.X - to.X) == Math.Abs(from.Y - to.Y);
}

public class Queen : ChessFigure
{
    public Queen() : base("Ферзь") { }

    public override bool IsValidMove(CheckerBoardPosition from, CheckerBoardPosition to)
        => from.X == to.X || from.Y == to.Y ||
           Math.Abs(from.X - to.X) == Math.Abs(from.Y - to.Y);
}

public class Knight : ChessFigure
{
    public Knight() : base("Конь") { }

    public override bool IsValidMove(CheckerBoardPosition from, CheckerBoardPosition to)
    {
        int dx = Math.Abs(from.X - to.X);
        int dy = Math.Abs(from.Y - to.Y);
        return (dx == 1 && dy == 2) || (dx == 2 && dy == 1);
    }
}

public class King : ChessFigure
{
    public King() : base("Король") { }

    public override bool IsValidMove(CheckerBoardPosition from, CheckerBoardPosition to)
    {
        int dx = Math.Abs(from.X - to.X);
        int dy = Math.Abs(from.Y - to.Y);
        return dx <= 1 && dy <= 1;
    }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Добро пожаловать в проверку шахматных ходов!");
        Console.WriteLine("Доступные фигуры: Пешка, Конь, Слон, Ладья, Ферзь, Король");
        Console.WriteLine();

        Console.Write("Введите фигуру: ");
        string? figureName = Console.ReadLine()?.Trim().ToLower();
        Console.Write("Введите цвет фигуры (белый или чёрный): ");
        string? colorInput = Console.ReadLine()?.Trim().ToLower();

        bool? isWhite = colorInput switch
        {
            "белый" => true,
            "чёрный" => false,
            "черный" => false, // на всякий случай
            _ => null
        };

        if (figureName == "пешка")
        {
            if (isWhite is null) { Console.WriteLine("Некорректный цвет!"); return; }
        }

        ChessFigure? figure = figureName switch
        {
            "пешка" => new Pawn(isWhite.Value),
            "конь" => new Knight(),
            "слон" => new Bishop(),
            "ладья" => new Rook(),
            "ферзь" => new Queen(),
            "король" => new King(),
            _ => null
        };

        if (figure is null)
        {
            Console.WriteLine("Неизвестная фигура!");
            return;
        }

        Console.Write("Введите начальную позицию (например, E2): ");
        if (!CheckerBoardPosition.TryParse(Console.ReadLine(), null, out var from))
        {
            Console.WriteLine("Невозможная позиция!");
            return;
        }

        Console.Write("Введите конечную позицию (например, E4): ");
        if (!CheckerBoardPosition.TryParse(Console.ReadLine(), null, out var to))
        {
            Console.WriteLine("Невозможная позиция!");
            return;
        }

        bool valid = figure.IsValidMove(from!, to!);

        Console.WriteLine();
        Console.WriteLine($"Ход {figure.Name} из {from} в {to} — {(valid ? "валидный" : "невалидный")}");
    }
}