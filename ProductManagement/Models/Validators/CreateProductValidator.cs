using FluentValidation;
using ProductManagement.Models.DTOs;

namespace ProductManagement.Models.Validators
{
    public class CreateProductValidator : AbstractValidator<CreateProductDTO>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than 0");


            RuleFor(x => x.Amount)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Amount must be at least 1");

        }
    }
}
