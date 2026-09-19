using PharmaTrust.Models;

namespace PharmaTrust.Services
{
    public interface ICartService
    {
        Cart GetCart();
        Cart AddToCart(int productId, int quantity = 1);
        Cart UpdateQuantity(int productId, int quantity);
        Cart RemoveFromCart(int productId);
        void ClearCart();
    }
}
