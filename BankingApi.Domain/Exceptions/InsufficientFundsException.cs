namespace BankingApi.Domain.Exceptions
{
    public class InsufficientFundsException : DomainException
    {
        public InsufficientFundsException(string message = "Insufficient funds")
            : base(message, "INSUFFICIENT_FUNDS")
        {
        }
    }
}
