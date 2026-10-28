namespace Ordering.Domain.Common;

/// <summary>
/// A business rule was violated, for example confirming an order that is already cancelled.
/// The API turns it into HTTP 409 Conflict.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
