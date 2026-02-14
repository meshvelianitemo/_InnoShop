using FluentValidation;
using ProductManagement.Models.DTOs;

namespace ProductManagement.Models.Validators
{
    public class UpdateProductValidator : AbstractValidator<CreateProductDTO>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0");
            RuleFor(x => x.Amount).GreaterThanOrEqualTo(1).WithMessage("Amount cannot be negative");
        }
    }
}
