namespace Inventory.Domain.Exceptions
{
    public class InvalidDiscountException : Exception
    {
        public decimal Percentage { get; }

        public InvalidDiscountException(decimal percentage)
            : base($"Discount percentage must be between 0 and 70. Provided: {percentage}.")
        {
            Percentage = percentage;
        }
    }
}
