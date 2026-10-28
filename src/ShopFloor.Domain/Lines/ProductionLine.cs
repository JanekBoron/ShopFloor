namespace ShopFloor.Domain.Lines;

/// <summary>
/// A production line on the shop floor. Machines will be assigned to a line,
/// and OEE is reported per line and shift.
/// </summary>
public sealed class ProductionLine
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 100;

    // Used by EF Core
    private ProductionLine()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Short unique identifier, stored upper case (for example "L1" or "PACK-2").</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static ProductionLine Create(string code, string name, DateTimeOffset createdAt)
    {
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

        return new ProductionLine
        {
            Id = Guid.CreateVersion7(createdAt),
            Code = normalizedCode,
            Name = trimmedName,
            CreatedAt = createdAt,
        };
    }
}
