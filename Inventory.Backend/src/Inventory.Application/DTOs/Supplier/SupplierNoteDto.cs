namespace Inventory.Application.DTOs.Supplier
{
    public class SupplierNoteDto
    {
        public int Id { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public SupplierNoteDto(int id, string note, DateTime createdAt)
        {
            Id = id;
            Note = note;
            CreatedAt = createdAt;
        }
    }
}
