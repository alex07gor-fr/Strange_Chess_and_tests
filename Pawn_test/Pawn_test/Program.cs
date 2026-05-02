using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;

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
    public Pawn() : base("Пешка") { }

    public override bool IsValidMove(CheckerBoardPosition from, CheckerBoardPosition to)
    {
        int dx = Math.Abs(from.X - to.X);
        int dy = Math.Abs(from.Y - to.Y);
        return dx == 0 && (dy <= 1 || dy == 2);
    }
}

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Добро пожаловать в проверку шахматных ходов!");
        Console.WriteLine("Доступные фигуры: Пешка");
        Console.WriteLine();

        Console.Write("Введите фигуру: ");
        string? figureName = Console.ReadLine()?.Trim().ToLower();

        ChessFigure? figure = figureName switch
        {
            "пешка" => new Pawn(),
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
            Console.WriteLine("Некорректная позиция!");
            return;
        }

        Console.Write("Введите конечную позицию (например, E4): ");
        if (!CheckerBoardPosition.TryParse(Console.ReadLine(), null, out var to))
        {
            Console.WriteLine("Некорректная позиция!");
            return;
        }

        bool valid = figure.IsValidMove(from!, to!);

        Console.WriteLine();
        Console.WriteLine($"Ход {figure.Name} из {from} в {to} — {(valid ? "валидный" : "невалидный")}");
    }
}