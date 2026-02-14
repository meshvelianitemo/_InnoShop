using FluentValidation;
using UserManagement.Models.DTOs;

namespace UserManagement.Models.Validators
{
    public class PasswordRecoveryValidator : AbstractValidator<PasswordRecoveryDTO>
    {
        public PasswordRecoveryValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New Password is required.")
                .MinimumLength(6).WithMessage("New Password must be at least 6 characters long.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm Password is required.");
        }
    }
}
