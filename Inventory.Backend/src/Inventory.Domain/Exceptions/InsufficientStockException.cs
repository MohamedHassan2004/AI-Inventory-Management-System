namespace Inventory.Domain.Exceptions
{
    
    public class InsufficientStockException : Exception
    {
        public string ProductName { get; }
        public decimal Requested { get; }
        public decimal Available { get; }

        public InsufficientStockException(string productName, decimal requested, decimal available)
            : base($"Insufficient stock for product '{productName}'. Requested: {requested}, Available: {available}.")
        {
            ProductName = productName;
            Requested = requested;
            Available = available;
        }
    }
}
