using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;
using Inventory.Application.DTOs.Order;

namespace Inventory.Application.Validation.Order
{
    public class UpdateOrderItemQuantityValidator : AbstractValidator<UpdateOrderItemQuantityDto>
    {
        public UpdateOrderItemQuantityValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero");
        }
    }
}