using BankingApi.Application.DTOs.Transactions;
using FluentValidation;

namespace BankingApi.Application.Validators
{
    public class TransferRequestValidator : AbstractValidator<TransferRequest>
    {
        public TransferRequestValidator()
        {
            RuleFor(x => x.SourceAccountId)
                .NotEmpty().WithMessage("Source account ID is required");

            RuleFor(x => x.DestinationAccountId)
                .NotEmpty().WithMessage("Destination account ID is required");

            RuleFor(x => x)
                .Must(x => x.SourceAccountId != x.DestinationAccountId)
                .WithMessage("Source and destination accounts must be different");

            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(100.00m).WithMessage("Minimum transfer is ₦100.00")
                .LessThanOrEqualTo(5000000.00m).WithMessage("Maximum transfer is ₦5,000,000.00");

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
