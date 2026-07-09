using BankingApi.Application.DTOs.Transactions;
using FluentValidation;

namespace BankingApi.Application.Validators
{
    public class DepositRequestValidator : AbstractValidator<DepositRequest>
    {
        public DepositRequestValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty().WithMessage("Account ID is required");

            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(100.00m).WithMessage("Minimum deposit is ₦100.00")
                .LessThanOrEqualTo(5000000.00m).WithMessage("Maximum deposit is ₦5,000,000.00");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MinimumLength(3).WithMessage("Description must be at least 3 characters");
        }
    }
}
