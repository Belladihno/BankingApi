namespace BankingApi.Domain.Exceptions
{
    public class AccountClosedException : DomainException
    {
        public AccountClosedException(string message = "Account is closed")
            : base(message, "ACCOUNT_CLOSED")
        {
        }
    }
}
