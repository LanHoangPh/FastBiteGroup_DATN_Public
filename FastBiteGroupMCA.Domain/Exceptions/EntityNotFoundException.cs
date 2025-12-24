namespace FastBiteGroupMCA.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message)
    {
    }
    public EntityNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
    public EntityNotFoundException() : base("Entity not found.")
    {
    }
}
