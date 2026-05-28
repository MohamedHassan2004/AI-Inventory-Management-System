using Inventory.Application.DTOs.Supplier;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;

namespace Inventory.Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISupplierRepository _supplierRepository;
        private readonly ILocalizationService _localizationService;

        public SupplierService(IUnitOfWork unitOfWork, ISupplierRepository supplierRepository, ILocalizationService localizationService)
        {
            _unitOfWork = unitOfWork;
            _supplierRepository = supplierRepository;
            _localizationService = localizationService;
        }

        public async Task<Result<IEnumerable<SupplierDto>>> GetAllSuppliersAsync(CancellationToken cancellationToken = default)
        {
            var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);
            var dtos = suppliers.Select(s => new SupplierDto(s.Id, s.Name, s.PhoneNumber, s.ContactInfo, s.Address, s.TotalRating, s.RatingCount, s.AvgRating, s.DeliveryCount, s.AvgDeliveryTime));
            return Result.Success(dtos);
        }

        public async Task<Result<SupplierDto>> GetSupplierByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier == null)
                return Result.Failure<SupplierDto>(new Error("Supplier.NotFound", _localizationService.GetMessage("SupplierNotFoundWithId", id), ErrorType.NotFound));

            var dto = new SupplierDto(supplier.Id, supplier.Name, supplier.PhoneNumber, supplier.ContactInfo, supplier.Address, supplier.TotalRating, supplier.RatingCount, supplier.AvgRating, supplier.DeliveryCount, supplier.AvgDeliveryTime);
            return Result.Success(dto);
        }

        public async Task<Result<SupplierDto>> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken cancellationToken = default)
        {
            if (await _supplierRepository.ExistsAsync(dto.Name, cancellationToken))
                return Result.Failure<SupplierDto>(new Error("Supplier.AlreadyExists", _localizationService.GetMessage("SupplierAlreadyExists", dto.Name), ErrorType.Conflict));

            var supplier = new Supplier(dto.Name, dto.PhoneNumber, dto.ContactInfo, dto.Address);
            await _supplierRepository.AddAsync(supplier, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = new SupplierDto(supplier.Id, supplier.Name, supplier.PhoneNumber, supplier.ContactInfo, supplier.Address, supplier.TotalRating, supplier.RatingCount, supplier.AvgRating, supplier.DeliveryCount, supplier.AvgDeliveryTime);
            return Result.Success(resultDto);
        }

        public async Task<Result<SupplierDto>> UpdateSupplierAsync(int id, UpdateSupplierDto dto, CancellationToken cancellationToken = default)
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier == null)
                return Result.Failure<SupplierDto>(new Error("Supplier.NotFound", _localizationService.GetMessage("SupplierNotFoundWithId", id), ErrorType.NotFound));

            if (!string.Equals(supplier.Name, dto.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (await _supplierRepository.ExistsAsync(dto.Name, cancellationToken))
                    return Result.Failure<SupplierDto>(new Error("Supplier.AlreadyExists", _localizationService.GetMessage("SupplierAlreadyExists", dto.Name), ErrorType.Conflict));
            }

            supplier.Rename(dto.Name);
            supplier.UpdateContactInfo(dto.PhoneNumber, dto.ContactInfo, dto.Address);

            _supplierRepository.Update(supplier);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var resultDto = new SupplierDto(supplier.Id, supplier.Name, supplier.PhoneNumber, supplier.ContactInfo, supplier.Address, supplier.TotalRating, supplier.RatingCount, supplier.AvgRating, supplier.DeliveryCount, supplier.AvgDeliveryTime);
            return Result.Success(resultDto);
        }

        public async Task<Result> DeleteSupplierAsync(int id, CancellationToken cancellationToken = default)
        {
            var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken);
            if (supplier == null)
                return Result.Failure(new Error("Supplier.NotFound", _localizationService.GetMessage("SupplierNotFoundWithId", id), ErrorType.NotFound));

            if (await _supplierRepository.HasRelatedStockBatchesAsync(id, cancellationToken))
                return Result.Failure(new Error("Supplier.HasStockBatches", _localizationService.GetMessage("SupplierHasStockBatches"), ErrorType.Conflict));

            supplier.MarkAsDeleted();
            _supplierRepository.Update(supplier);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> RestoreSupplierAsync(int id, CancellationToken cancellationToken = default)
        {
            var supplier = await _supplierRepository.GetByIdWithDeletedAsync(id, cancellationToken);
            if (supplier == null)
                return Result.Failure(new Error("Supplier.NotFound", _localizationService.GetMessage("SupplierNotFoundWithId", id), ErrorType.NotFound));

            supplier.Restore();
            _supplierRepository.Update(supplier);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result<IEnumerable<SupplierNoteDto>>> GetSupplierNotesAsync(int supplierId, CancellationToken cancellationToken = default)
        {
            var supplier = await _supplierRepository.GetSupplierWithNotesAsync(supplierId, cancellationToken);
            if (supplier == null)
                return Result.Failure<IEnumerable<SupplierNoteDto>>(new Error("Supplier.NotFound", _localizationService.GetMessage("SupplierNotFoundWithId", supplierId), ErrorType.NotFound));

            var dtos = supplier.SupplierNotes.Select(n => new SupplierNoteDto(n.Id, n.Note, n.CreatedAt));
            return Result.Success(dtos);
        }

        public async Task<Result<SupplierDto>> AddSupplierRatingAsync(int supplierId, AddSupplierRatingDto dto, CancellationToken cancellationToken = default)
        {
            var supplier = await _supplierRepository.GetSupplierWithNotesAsync(supplierId, cancellationToken);
            if (supplier == null)
                return Result.Failure<SupplierDto>(new Error("Supplier.NotFound", _localizationService.GetMessage("SupplierNotFoundWithId", supplierId), ErrorType.NotFound));

            try
            {
                supplier.AddRating(dto.Rating, dto.Note);

                _supplierRepository.Update(supplier);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var resultDto = new SupplierDto(supplier.Id, supplier.Name, supplier.PhoneNumber, supplier.ContactInfo, supplier.Address, supplier.TotalRating, supplier.RatingCount, supplier.AvgRating, supplier.DeliveryCount, supplier.AvgDeliveryTime);
                return Result.Success(resultDto);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<SupplierDto>(new Error("Supplier.InvalidRating", ex.Message, ErrorType.Validation));
            }
        }

        public async Task<Result<IEnumerable<SupplierDto>>> GetDeletedSuppliersAsync(CancellationToken cancellationToken = default)
        {
            var suppliers = await _supplierRepository.GetDeletedSuppliersAsync(cancellationToken);
            var dtos = suppliers.Select(s => new SupplierDto(s.Id, s.Name, s.PhoneNumber, s.ContactInfo, s.Address, s.TotalRating, s.RatingCount, s.AvgRating, s.DeliveryCount, s.AvgDeliveryTime));
            return Result.Success(dtos);
        }
    }
}