using System.ComponentModel.DataAnnotations;
using ShopFloor.Domain.Machines;

namespace ShopFloor.Api.Features.Machines;

public sealed record CreateMachineRequest(
    [Required, StringLength(Machine.CodeMaxLength)] string Code,
    [Required, StringLength(Machine.NameMaxLength)] string Name,
    [Range(0.001, 3600.0)] double IdealCycleTimeSeconds);

public sealed record MachineResponse(
    Guid Id,
    Guid LineId,
    string Code,
    string Name,
    double IdealCycleTimeSeconds,
    DateTimeOffset CreatedAt)
{
    public static MachineResponse From(Machine machine) => new(
        machine.Id,
        machine.LineId,
        machine.Code,
        machine.Name,
        machine.IdealCycleTime.TotalSeconds,
        machine.CreatedAt);
}
