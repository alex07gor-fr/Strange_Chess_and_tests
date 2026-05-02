using Xunit;
using ChessExample;

namespace CheckerBoardPosition.Test;

public class CheckerBoardPositionTests
{
    // 🔸 Конструктор — корректные значения
    [Theory]
    [InlineData(1, 1)]
    [InlineData(8, 8)]
    [InlineData(5, 3)]
    public void Constructor_ValidValues_ShouldCreateObject(byte x, byte y)
    {
        var pos = new CheckerBoardPosition(x, y);

        Assert.Equal(x, pos.X);
        Assert.Equal(y, pos.Y);
    }

    // 🔸 Конструктор — ошибки
    [Theory]
    [InlineData(0, 1)]
    [InlineData(9, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 9)]
    public void Constructor_InvalidValues_ShouldThrow(byte x, byte y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CheckerBoardPosition(x, y));
    }

    // 🔸 ToString
    [Theory]
    [InlineData(1, 1, "A1")]
    [InlineData(5, 2, "E2")]
    [InlineData(8, 8, "H8")]
    public void ToString_ShouldReturnCorrectFormat(byte x, byte y, string expected)
    {
        var pos = new CheckerBoardPosition(x, y);

        Assert.Equal(expected, pos.ToString());
    }

    // 🔸 TryParse — успех
    [Theory]
    [InlineData("A1", 1, 1)]
    [InlineData("E2", 5, 2)]
    [InlineData("H8", 8, 8)]
    public void TryParse_ValidInput_ShouldReturnTrue(string input, byte x, byte y)
    {
        var result = CheckerBoardPosition.TryParse(input, null, out var pos);

        Assert.True(result);
        Assert.NotNull(pos);
        Assert.Equal(x, pos!.X);
        Assert.Equal(y, pos.Y);
    }

    // 🔸 TryParse — ошибка
    [Theory]
    [InlineData("")]
    [InlineData("Z9")]
    [InlineData("A9")]
    [InlineData("I1")]
    [InlineData("11")]
    [InlineData("AA")]
    public void TryParse_InvalidInput_ShouldReturnFalse(string input)
    {
        var result = CheckerBoardPosition.TryParse(input, null, out var pos);

        Assert.False(result);
        Assert.Null(pos);
    }

    // 🔸 Parse — успех
    [Fact]
    public void Parse_ValidInput_ShouldReturnObject()
    {
        var pos = CheckerBoardPosition.Parse("E2", null);

        Assert.Equal(5, pos.X);
        Assert.Equal(2, pos.Y);
    }

    // 🔸 Parse — ошибка
    [Fact]
    public void Parse_InvalidInput_ShouldThrow()
    {
        Assert.Throws<FormatException>(() =>
            CheckerBoardPosition.Parse("Z9", null));
    }
}