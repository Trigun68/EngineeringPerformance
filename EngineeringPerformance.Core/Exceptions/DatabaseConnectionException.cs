namespace EngineeringPerformance.Core.Exceptions;

public sealed class DatabaseConnectionException : Exception
{
    public DatabaseConnectionException(string message) : base(message)
    {
    }

    public DatabaseConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
