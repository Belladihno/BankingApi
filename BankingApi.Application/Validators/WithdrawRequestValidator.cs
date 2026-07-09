using BankingApi.Application.DTOs.Transactions;
using FluentValidation;

namespace BankingApi.Application.Validators
{
    public class WithdrawRequestValidator : AbstractValidator<WithdrawRequest>
    {
        public WithdrawRequestValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty().WithMessage("Account ID is required");

            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(100.00m).WithMessage("Minimum withdrawal is ₦100.00")
                .LessThanOrEqualTo(5000000.00m).WithMessage("Maximum withdrawal is ₦5,000,000.00");

            RuleFor(x => x.TransactionPin)
                .NotEmpty().WithMessage("Transaction PIN is required")
                .Length(4).WithMessage("PIN must be exactly 4 digits")
                .Matches("^[0-9]{4}$").WithMessage("PIN must be numeric only");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MinimumLength(3).WithMessage("Description must be at least 3 characters");
        }
    }
}
