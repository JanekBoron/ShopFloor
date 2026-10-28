using System.ComponentModel.DataAnnotations;
using ShopFloor.Domain.Lines;

namespace ShopFloor.Api.Features.Lines;

public sealed record CreateLineRequest(
    [Required, StringLength(ProductionLine.CodeMaxLength)] string Code,
    [Required, StringLength(ProductionLine.NameMaxLength)] string Name);

public sealed record LineResponse(Guid Id, string Code, string Name, DateTimeOffset CreatedAt)
{
    public static LineResponse From(ProductionLine line) => new(line.Id, line.Code, line.Name, line.CreatedAt);
}
