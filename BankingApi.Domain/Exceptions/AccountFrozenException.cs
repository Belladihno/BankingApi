namespace BankingApi.Domain.Exceptions
{
    public class AccountFrozenException : DomainException
    {
        public AccountFrozenException(string message = "Account is frozen")
            : base(message, "ACCOUNT_FROZEN")
        {
        }
    }
}
