namespace FastBiteGroupMCA.Domain.Exceptions
{
    public class DomainException :Exception
    {
        public DomainException(string message) : base(message)
        {
        }
        public DomainException(string message, Exception innerException) : base(message, innerException)
        {
        }
        public DomainException() : base("An error occurred in the domain layer.")
        {
        }
    }
}
