using ShopFloor.Domain.Lines;

namespace ShopFloor.Domain.Tests.Lines;

public class ProductionLineTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_code_and_trims_name()
    {
        var line = ProductionLine.Create("  pack-2 ", "  Packaging line 2  ", Now);

        Assert.Equal("PACK-2", line.Code);
        Assert.Equal("Packaging line 2", line.Name);
        Assert.Equal(Now, line.CreatedAt);
        Assert.NotEqual(Guid.Empty, line.Id);
    }

    [Theory]
    [InlineData("", "Line")]
    [InlineData("   ", "Line")]
    [InlineData("L1", "")]
    [InlineData("L1", "   ")]
    public void Create_rejects_blank_code_or_name(string code, string name)
    {
        Assert.ThrowsAny<ArgumentException>(() => ProductionLine.Create(code, name, Now));
    }

    [Fact]
    public void Create_rejects_too_long_code()
    {
        var code = new string('X', ProductionLine.CodeMaxLength + 1);

        Assert.Throws<ArgumentException>(() => ProductionLine.Create(code, "Line", Now));
    }

    [Fact]
    public void Create_rejects_too_long_name()
    {
        var name = new string('X', ProductionLine.NameMaxLength + 1);

        Assert.Throws<ArgumentException>(() => ProductionLine.Create("L1", name, Now));
    }
}
