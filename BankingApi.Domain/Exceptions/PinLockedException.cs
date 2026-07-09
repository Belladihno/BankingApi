namespace BankingApi.Domain.Exceptions
{
    public class PinLockedException : DomainException
    {
        public PinLockedException(string message = "Transaction PIN is locked")
            : base(message, "PIN_LOCKED")
        {
        }
    }
}
