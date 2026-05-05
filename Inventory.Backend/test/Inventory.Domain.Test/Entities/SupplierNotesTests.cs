using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Domain.Test.Entities
{
    public class SupplierNotesTests
    {
        [Fact]
        public void CanCreateSupplierNotes()
        {
            var notes = new SupplierNotes();
            Assert.NotNull(notes);
        }

        [Fact]
        public void SupplierNotes_DefaultValues_ShouldBeCorrect()
        {
            var notes = new SupplierNotes();
            Assert.Equal(0, notes.Id);
            Assert.Equal(string.Empty, notes.Note);
            Assert.True((System.DateTime.UtcNow - notes.CreatedAt).TotalSeconds < 2);
            Assert.Equal(0, notes.SupplierId);
            Assert.Null(notes.Supplier);
        }

        [Fact]
        public void SupplierNotes_SetProperties_ShouldWork()
        {
            var notes = new SupplierNotes
            {
                Id = 1,
                Note = "Test note",
                SupplierId = 2
            };
            Assert.Equal(1, notes.Id);
            Assert.Equal("Test note", notes.Note);
            Assert.Equal(2, notes.SupplierId);
        }
    }
}
