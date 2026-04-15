namespace Inventory.Domain.Exceptions
{
    public class EmptyOrderException : Exception
    {
        public EmptyOrderException()
            : base("Cannot complete an empty order. Ensure items are added first.")
        {
        }
    }
}
