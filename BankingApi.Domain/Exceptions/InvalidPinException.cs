namespace BankingApi.Domain.Exceptions
{
    public class InvalidPinException : DomainException
    {
        public InvalidPinException(string message = "Invalid transaction PIN")
            : base(message, "INVALID_PIN")
        {
        }
    }
}
