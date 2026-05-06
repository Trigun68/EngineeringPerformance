namespace EngineeringPerformance.Core.Exceptions;

public sealed class AccessDeniedException : Exception
{
    public AccessDeniedException(string message) : base(message)
    {
    }
}
