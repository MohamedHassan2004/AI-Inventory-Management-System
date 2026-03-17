using FluentValidation;
using Inventory.Application.DTOs.StockBatch;
using Inventory.Application.Interfaces;
using System;

namespace Inventory.Application.Validation.StockBatch
{
    public class CreateStockBatchValidator : AbstractValidator<CreateStockBatchDto>
    {
        public CreateStockBatchValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage(localizationService.GetMessage("BatchProductRequired"));

            RuleFor(x => x.SupplierId)
                .GreaterThan(0).WithMessage(localizationService.GetMessage("BatchSupplierRequired"));

            RuleFor(x => x.PurchaseDate)
                .NotEmpty().WithMessage(localizationService.GetMessage("BatchPurchaseDateRequired"));

            RuleFor(x => x.ExpireDate)
                .NotEmpty().WithMessage(localizationService.GetMessage("BatchExpireDateRequired"))
                .GreaterThan(x => x.PurchaseDate).WithMessage(localizationService.GetMessage("BatchExpireDateInvalid"));

            RuleFor(x => x.UnitCost)
                .GreaterThanOrEqualTo(0).WithMessage(localizationService.GetMessage("BatchCostInvalid"));

            RuleFor(x => x.OriginalQuantity)
                .GreaterThanOrEqualTo(0).WithMessage(localizationService.GetMessage("BatchQuantityInvalid"));
        }
    }
}
