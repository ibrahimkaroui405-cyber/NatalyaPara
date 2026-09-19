namespace PharmaTrust.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new();

        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal Subtotal => Items.Sum(i => i.Subtotal);
        public decimal ShippingFee => Subtotal > 35 || TotalItems == 0 ? 0 : 4.99m;
        public decimal GrandTotal => Subtotal + ShippingFee;
    }
}
