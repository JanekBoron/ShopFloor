namespace ShopFloor.Domain;

/// <summary>
/// A business rule was broken, for example completing a work order that was never started.
/// The input itself is valid; the operation is not allowed in the current state.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
