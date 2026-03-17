using FluentValidation;
using Inventory.Application.DTOs.StockBatch;
using Inventory.Application.Interfaces;
using System;

namespace Inventory.Application.Validation.StockBatch
{
    public class UpdateStockBatchValidator : AbstractValidator<UpdateStockBatchDto>
    {
        public UpdateStockBatchValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.ExpireDate)
                .NotEmpty().WithMessage(localizationService.GetMessage("BatchExpireDateRequired"));

            RuleFor(x => x.UnitCost)
                .GreaterThanOrEqualTo(0).WithMessage(localizationService.GetMessage("BatchCostInvalid"));

            RuleFor(x => x.RemainingQuantity)
                .GreaterThanOrEqualTo(0).WithMessage(localizationService.GetMessage("BatchQuantityInvalid"));
        }
    }
}
