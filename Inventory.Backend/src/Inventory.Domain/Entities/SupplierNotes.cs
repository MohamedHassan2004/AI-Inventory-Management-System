namespace Inventory.Domain.Entities
{
    public class SupplierNotes
    {
        public int Id { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
    }
}