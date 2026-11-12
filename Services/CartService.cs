using System.Text.Json;
using PharmaTrust.Models;

namespace PharmaTrust.Services
{
    /// <summary>
    /// Stateless Cart Service: Cart state is persisted client-side in an HTTP-only cookie
    /// containing only minimal item references (ProductId, Quantity).
    /// All product metadata, prices, stock limits and discounts are always resolved server-side.
    /// This makes the application 100% stateless (resilient across server restarts and horizontally scalable).
    /// </summary>
    public class CartService : ICartService
    {
        private const string CartCookieName = "NatalyaPara_Cart";
        private const string RequestCartCacheKey = "__NatalyaPara_Request_Cart";
        private const string LegacySessionKey = "PharmaTrust_CartSession";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService;

        public CartService(IHttpContextAccessor httpContextAccessor, IProductService productService)
        {
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
        }

        private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

        public Cart GetCart()
        {
            if (HttpContext == null) return new Cart();

            // Cache within the current HTTP request to prevent redundant DB reads
            if (HttpContext.Items.TryGetValue(RequestCartCacheKey, out var cached) && cached is Cart cachedCart)
            {
                return cachedCart;
            }

            var itemDtos = LoadCartItemDtos();
            var cart = new Cart();
            bool changed = false;

            for (int i = itemDtos.Count - 1; i >= 0; i--)
            {
                var dto = itemDtos[i];
                var product = _productService.GetProductById(dto.ProductId);

                if (product == null || product.StockQuantity <= 0)
                {
                    itemDtos.RemoveAt(i);
                    changed = true;
                }
                else
                {
                    int qty = dto.Quantity;
                    if (qty <= 0)
                    {
                        itemDtos.RemoveAt(i);
                        changed = true;
                        continue;
                    }

                    if (qty > product.StockQuantity)
                    {
                        qty = product.StockQuantity;
                        dto.Quantity = qty;
                        changed = true;
                    }

                    cart.Items.Insert(0, new CartItem
                    {
                        Product = product,
                        Quantity = qty
                    });
                }
            }

            if (changed)
            {
                SaveCartItemDtos(itemDtos);
            }

            HttpContext.Items[RequestCartCacheKey] = cart;
            return cart;
        }

        public Cart AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var product = _productService.GetProductById(productId);
            if (product == null || product.StockQuantity <= 0)
            {
                return GetCart();
            }

            var itemDtos = LoadCartItemDtos();
            var existing = itemDtos.FirstOrDefault(x => x.ProductId == productId);

            if (existing != null)
            {
                int newQty = existing.Quantity + quantity;
                if (newQty > product.StockQuantity)
                {
                    newQty = product.StockQuantity;
                }
                existing.Quantity = newQty;
            }
            else
            {
                itemDtos.Add(new CartItemCookieDto
                {
                    ProductId = productId,
                    Quantity = Math.Min(quantity, product.StockQuantity)
                });
            }

            SaveCartItemDtos(itemDtos);
            InvalidateRequestCache();
            return GetCart();
        }

        public Cart UpdateQuantity(int productId, int quantity)
        {
            var itemDtos = LoadCartItemDtos();
            var existing = itemDtos.FirstOrDefault(x => x.ProductId == productId);

            if (existing != null)
            {
                if (quantity <= 0)
                {
                    itemDtos.Remove(existing);
                }
                else
                {
                    var product = _productService.GetProductById(productId);
                    int maxStock = product?.StockQuantity ?? quantity;
                    existing.Quantity = Math.Min(quantity, maxStock);
                }

                SaveCartItemDtos(itemDtos);
                InvalidateRequestCache();
            }

            return GetCart();
        }

        public Cart RemoveFromCart(int productId)
        {
            var itemDtos = LoadCartItemDtos();
            int removed = itemDtos.RemoveAll(x => x.ProductId == productId);

            if (removed > 0)
            {
                SaveCartItemDtos(itemDtos);
                InvalidateRequestCache();
            }

            return GetCart();
        }

        public void ClearCart()
        {
            InvalidateRequestCache();

            if (HttpContext == null) return;

            // Delete cookie
            HttpContext.Response.Cookies.Delete(CartCookieName);

            // Also clean legacy session if present
            try
            {
                HttpContext.Session?.Remove(LegacySessionKey);
            }
            catch { }
        }

        private void InvalidateRequestCache()
        {
            if (HttpContext != null)
            {
                HttpContext.Items.Remove(RequestCartCacheKey);
            }
        }

        private List<CartItemCookieDto> LoadCartItemDtos()
        {
            if (HttpContext == null) return new List<CartItemCookieDto>();

            // 1. Try reading stateless cookie first
            if (HttpContext.Request.Cookies.TryGetValue(CartCookieName, out var cookieValue) && !string.IsNullOrWhiteSpace(cookieValue))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<CartItemCookieDto>>(cookieValue);
                    if (items != null) return items;
                }
                catch
                {
                    // Fall back if corrupted
                }
            }

            // 2. Migration fallback: check legacy session if available
            try
            {
                var sessionJson = HttpContext.Session?.GetString(LegacySessionKey);
                if (!string.IsNullOrEmpty(sessionJson))
                {
                    var legacyCart = JsonSerializer.Deserialize<Cart>(sessionJson);
                    if (legacyCart?.Items != null)
                    {
                        var migrated = legacyCart.Items
                            .Where(i => i.Product != null && i.Product.Id > 0 && i.Quantity > 0)
                            .Select(i => new CartItemCookieDto { ProductId = i.Product.Id, Quantity = i.Quantity })
                            .ToList();

                        // Persist to cookie immediately
                        SaveCartItemDtos(migrated);
                        // Clean session
                        HttpContext.Session?.Remove(LegacySessionKey);
                        return migrated;
                    }
                }
            }
            catch { }

            return new List<CartItemCookieDto>();
        }

        private void SaveCartItemDtos(List<CartItemCookieDto> itemDtos)
        {
            if (HttpContext == null) return;

            if (itemDtos == null || itemDtos.Count == 0)
            {
                HttpContext.Response.Cookies.Delete(CartCookieName);
                return;
            }

            var json = JsonSerializer.Serialize(itemDtos);
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                Secure = HttpContext.Request.IsHttps
            };

            HttpContext.Response.Cookies.Append(CartCookieName, json, cookieOptions);
        }

        private class CartItemCookieDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
