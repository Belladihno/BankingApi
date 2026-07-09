using BankingApi.Application.DTOs.Accounts;
using FluentValidation;

namespace BankingApi.Application.Validators
{
    public class OpenAccountRequestValidator : AbstractValidator<OpenAccountRequest>
    {
        public OpenAccountRequestValidator()
        {
            RuleFor(x => x.OwnerId)
                .NotEmpty().WithMessage("Owner ID is required");

            RuleFor(x => x.AccountType)
                .IsInEnum().WithMessage("Invalid account type");

            RuleFor(x => x.OpeningDeposit)
                .GreaterThanOrEqualTo(500.00m).WithMessage("Opening deposit must be at least ₦500.00");
        }
    }
}
