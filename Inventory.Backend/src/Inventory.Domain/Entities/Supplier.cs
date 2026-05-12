using System;
using System.Collections.Generic;

namespace Inventory.Domain.Entities;

public class Supplier
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public string? ContactInfo { get; private set; }

    public string? Address { get; private set; }

    public int TotalRating { get; private set; } = 0;

    public int RatingCount { get; private set; } = 0;

    public double AvgRating =>
        RatingCount > 0
            ? (double)TotalRating / RatingCount
            : 0;

    public int DeliveryCount { get; private set; } = 0;

    public double AvgDeliveryTime { get; private set; } = 0;

    public bool IsDeleted { get; private set; } = false;

    // Navigations
    public ICollection<SupplierNotes> SupplierNotes { get; private set; } = new List<SupplierNotes>();

    public ICollection<PurchaseOrder> PurchaseOrders { get; private set; } = new List<PurchaseOrder>();

    public ICollection<StockBatch> StockBatches { get; private set; } = new List<StockBatch>();


    private Supplier()
    {
    }

    public Supplier(
        string name,
        string phoneNumber,
        string? contactInfo = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Name cannot be empty.",
                nameof(name));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException(
                "Phone number cannot be empty.",
                nameof(phoneNumber));

        Name = name.Trim();
        PhoneNumber = phoneNumber.Trim();
        ContactInfo = contactInfo?.Trim();
        Address = address?.Trim();
    }

    public void UpdateContactInfo(
        string phoneNumber,
        string? contactInfo,
        string? address)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException(
                "Phone number cannot be empty.",
                nameof(phoneNumber));

        PhoneNumber = phoneNumber.Trim();
        ContactInfo = contactInfo?.Trim();
        Address = address?.Trim();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Name cannot be empty.",
                nameof(name));

        Name = name.Trim();
    }

    public void AddRating(int rating, string? note = null)
    {
        if (rating < 0 || rating > 5)
            throw new ArgumentOutOfRangeException(
                nameof(rating),
                "Rating must be between 0 and 5.");

        TotalRating += rating;
        RatingCount++;

        if (!string.IsNullOrWhiteSpace(note))
        {
            SupplierNotes.Add(new SupplierNotes
            {
                Note = note.Trim(),
                SupplierId = Id
            });
        }
    }

    public void RegisterDelivery(double deliveryTime)
    {
        if (deliveryTime < 0)
            throw new ArgumentOutOfRangeException(
                nameof(deliveryTime));

        AvgDeliveryTime =
            ((AvgDeliveryTime * DeliveryCount) + deliveryTime)
            / (++DeliveryCount);
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }

    public void Restore()
    {
        IsDeleted = false;
    }
}