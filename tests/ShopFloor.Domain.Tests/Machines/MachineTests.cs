using ShopFloor.Domain.Machines;

namespace ShopFloor.Domain.Tests.Machines;

public class MachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 6, 0, 0, TimeSpan.Zero);
    private static readonly Guid LineId = Guid.CreateVersion7();

    [Fact]
    public void Create_normalizes_code_and_keeps_cycle_time()
    {
        var machine = Machine.Create(LineId, " filler-1 ", " Bottle filler ", TimeSpan.FromSeconds(1.5), Now);

        Assert.Equal(LineId, machine.LineId);
        Assert.Equal("FILLER-1", machine.Code);
        Assert.Equal("Bottle filler", machine.Name);
        Assert.Equal(TimeSpan.FromSeconds(1.5), machine.IdealCycleTime);
        Assert.Equal(Now, machine.CreatedAt);
    }

    [Fact]
    public void Create_requires_a_line()
    {
        Assert.Throws<ArgumentException>(() => Machine.Create(Guid.Empty, "M1", "Machine", TimeSpan.FromSeconds(1), Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_non_positive_cycle_time(double seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Machine.Create(LineId, "M1", "Machine", TimeSpan.FromSeconds(seconds), Now));
    }

    [Fact]
    public void Create_rejects_too_long_code()
    {
        var code = new string('X', Machine.CodeMaxLength + 1);

        Assert.Throws<ArgumentException>(() => Machine.Create(LineId, code, "Machine", TimeSpan.FromSeconds(1), Now));
    }
}
