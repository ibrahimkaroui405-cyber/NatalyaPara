using System.Text.Json;
using PharmaTrust.Models;

namespace PharmaTrust.Services
{
    public class CartService : ICartService
    {
        private const string CartSessionKey = "PharmaTrust_CartSession";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService;

        public CartService(IHttpContextAccessor httpContextAccessor, IProductService productService)
        {
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session 
            ?? throw new InvalidOperationException("Session state is unavailable.");

        public Cart GetCart()
        {
            var cartJson = Session.GetString(CartSessionKey);
            Cart cart;
            if (string.IsNullOrEmpty(cartJson))
            {
                cart = new Cart();
            }
            else
            {
                try
                {
                    cart = JsonSerializer.Deserialize<Cart>(cartJson) ?? new Cart();
                }
                catch
                {
                    cart = new Cart();
                }
            }

            // Sync with latest product details & stock
            bool changed = false;
            for (int i = cart.Items.Count - 1; i >= 0; i--)
            {
                var item = cart.Items[i];
                var product = _productService.GetProductById(item.Product.Id);
                if (product == null)
                {
                    cart.Items.RemoveAt(i);
                    changed = true;
                }
                else
                {
                    item.Product = product;
                    if (product.StockQuantity <= 0)
                    {
                        // Product went out of stock
                        cart.Items.RemoveAt(i);
                        changed = true;
                    }
                    else if (item.Quantity > product.StockQuantity)
                    {
                        // Cap to maximum available stock
                        item.Quantity = product.StockQuantity;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                SaveCart(cart);
            }

            return cart;
        }

        private void SaveCart(Cart cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            Session.SetString(CartSessionKey, cartJson);
        }

        public Cart AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;
            var cart = GetCart();
            var product = _productService.GetProductById(productId);
            if (product == null || product.StockQuantity <= 0)
            {
                return cart;
            }

            var item = cart.Items.FirstOrDefault(i => i.Product.Id == productId);

            if (item != null)
            {
                int newQty = item.Quantity + quantity;
                if (newQty > product.StockQuantity)
                {
                    newQty = product.StockQuantity;
                }
                item.Quantity = newQty;
            }
            else
            {
                int initialQty = Math.Min(quantity, product.StockQuantity);
                cart.Items.Add(new CartItem { Product = product, Quantity = initialQty });
            }

            SaveCart(cart);
            return cart;
        }

        public Cart UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.Product.Id == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Items.Remove(item);
                }
                else
                {
                    var product = _productService.GetProductById(productId);
                    int maxStock = product?.StockQuantity ?? quantity;
                    item.Quantity = Math.Min(quantity, maxStock);
                }
            }

            SaveCart(cart);
            return cart;
        }

        public Cart RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.Items.FirstOrDefault(i => i.Product.Id == productId);

            if (item != null)
            {
                cart.Items.Remove(item);
            }

            SaveCart(cart);
            return cart;
        }

        public void ClearCart()
        {
            Session.Remove(CartSessionKey);
        }
    }
}
