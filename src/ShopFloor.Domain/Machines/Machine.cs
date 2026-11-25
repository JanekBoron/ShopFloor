namespace ShopFloor.Domain.Machines;

/// <summary>
/// A machine on a production line. Its ideal cycle time is the reference for
/// the Performance part of OEE.
/// </summary>
public sealed class Machine
{
    public const int CodeMaxLength = 30;
    public const int NameMaxLength = 100;

    // Used by EF Core
    private Machine()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>The line the machine belongs to. Lines are referenced by id, not by navigation property.</summary>
    public Guid LineId { get; private set; }

    /// <summary>Unique within its line, stored upper case (for example "FILLER-1").</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Time the machine needs for one part when running at nominal speed.</summary>
    public TimeSpan IdealCycleTime { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Machine Create(Guid lineId, string code, string name, TimeSpan idealCycleTime, DateTimeOffset createdAt)
    {
        if (lineId == Guid.Empty)
        {
            throw new ArgumentException("Line id is required.", nameof(lineId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedCode = code.Trim().ToUpperInvariant();
        var trimmedName = name.Trim();

        if (normalizedCode.Length > CodeMaxLength)
        {
            throw new ArgumentException($"Code cannot be longer than {CodeMaxLength} characters.", nameof(code));
        }

        if (trimmedName.Length > NameMaxLength)
        {
            throw new ArgumentException($"Name cannot be longer than {NameMaxLength} characters.", nameof(name));
        }

        if (idealCycleTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(idealCycleTime), "Ideal cycle time must be positive.");
        }

        return new Machine
        {
            Id = Guid.CreateVersion7(createdAt),
            LineId = lineId,
            Code = normalizedCode,
            Name = trimmedName,
            IdealCycleTime = idealCycleTime,
            CreatedAt = createdAt,
        };
    }
}
