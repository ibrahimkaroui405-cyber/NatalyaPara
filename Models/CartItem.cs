namespace PharmaTrust.Models
{
    public class CartItem
    {
        public Product Product { get; set; } = new Product();
        public int Quantity { get; set; } = 1;

        public decimal Subtotal => Product.Price * Quantity;
    }
}
