using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Entities
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
        public string? Address { get; set; }
        public int TotalRating { get; private set; } = 0;
        public int RatingCount { get; private set; } = 0;
        public double AvgRating => RatingCount > 0 ? (double)TotalRating / RatingCount: 0;
        public int DeliveryCount { get; set; } = 0;
        public double AvgDeliveryTime { get; set; } = 0;
        public bool IsDeleted { get; private set; } = false;

        public List<SupplierNotes> SupplierNotes { get; set; } = new();

        public Supplier()
        {
        }

        public Supplier(string name, string phoneNumber, string? contactInfo = null, string? address = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (string.IsNullOrEmpty(phoneNumber))
                throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));
            Name = name;
            PhoneNumber = phoneNumber;
            ContactInfo = contactInfo;
            Address = address;
        }

        public void AddRating(int rating, string? note)
        {
            if (rating < 0 || rating > 5)
                throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 0 and 5.");
            TotalRating += rating;
            RatingCount++;

            if (!string.IsNullOrEmpty(note))
                SupplierNotes.Add(new SupplierNotes { Note = note, SupplierId = this.Id });
        }

        public void UpdatePhoneNumber(string newPhoneNumber)
        {
            if (string.IsNullOrEmpty(newPhoneNumber))
                throw new ArgumentException("Phone number cannot be null or empty.", nameof(newPhoneNumber));
            PhoneNumber = newPhoneNumber;
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
}
