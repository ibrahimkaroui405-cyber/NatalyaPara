using Microsoft.AspNetCore.Mvc;
using PharmaTrust.Models;
using PharmaTrust.Services;

namespace PharmaTrust.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;

        public CartController(ICartService cartService, IProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            ViewData["CartCount"] = cart.TotalItems;
            return View(cart);
        }

        // GET: /Cart/GetDrawer
        [HttpGet]
        public IActionResult GetDrawer()
        {
            var cart = _cartService.GetCart();
            return Json(new
            {
                success = true,
                items = cart.Items.Select(i => new
                {
                    id = i.Product.Id,
                    name = i.Product.Name,
                    brand = i.Product.Brand,
                    price = i.Product.Price,
                    imageUrl = i.Product.ImageUrl,
                    quantity = i.Quantity,
                    availableStock = i.Product.StockQuantity,
                    subtotal = i.Subtotal,
                    rxRequired = i.Product.RxRequired
                }),
                totalItems = cart.TotalItems,
                subtotal = cart.Subtotal,
                shippingFee = cart.ShippingFee,
                grandTotal = cart.GrandTotal
            });
        }

        // POST: /Cart/Add
        [HttpPost]
        public IActionResult Add(int id, int quantity = 1)
        {
            var product = _productService.GetProductById(id);
            if (product == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Soin introuvable." });
                }
                TempData["ErrorMessage"] = "Soin introuvable.";
                return RedirectToAction("Index");
            }

            if (product.StockQuantity <= 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"« {product.Name} » est actuellement en rupture de stock." });
                }
                TempData["ErrorMessage"] = $"« {product.Name} » est actuellement en rupture de stock.";
                return RedirectToAction("Index");
            }

            var cart = _cartService.AddToCart(id, quantity);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = $"« {product.Name} » a été ajouté à votre sélection.",
                    items = cart.Items.Select(i => new
                    {
                        id = i.Product.Id,
                        name = i.Product.Name,
                        brand = i.Product.Brand,
                        price = i.Product.Price,
                        imageUrl = i.Product.ImageUrl,
                        quantity = i.Quantity,
                        availableStock = i.Product.StockQuantity,
                        subtotal = i.Subtotal,
                        rxRequired = i.Product.RxRequired
                    }),
                    totalItems = cart.TotalItems,
                    subtotal = cart.Subtotal,
                    shippingFee = cart.ShippingFee,
                    grandTotal = cart.GrandTotal
                });
            }
            return RedirectToAction("Index");
        }

        // POST: /Cart/Update
        [HttpPost]
        public IActionResult Update(int id, int quantity)
        {
            var product = _productService.GetProductById(id);
            if (product != null && quantity > product.StockQuantity)
            {
                quantity = product.StockQuantity;
            }

            var cart = _cartService.UpdateQuantity(id, quantity);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    items = cart.Items.Select(i => new
                    {
                        id = i.Product.Id,
                        name = i.Product.Name,
                        brand = i.Product.Brand,
                        price = i.Product.Price,
                        imageUrl = i.Product.ImageUrl,
                        quantity = i.Quantity,
                        availableStock = i.Product.StockQuantity,
                        subtotal = i.Subtotal,
                        rxRequired = i.Product.RxRequired
                    }),
                    totalItems = cart.TotalItems,
                    subtotal = cart.Subtotal,
                    shippingFee = cart.ShippingFee,
                    grandTotal = cart.GrandTotal
                });
            }
            return RedirectToAction("Index");
        }

        // POST: /Cart/Remove
        [HttpPost]
        public IActionResult Remove(int id)
        {
            var cart = _cartService.RemoveFromCart(id);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    items = cart.Items.Select(i => new
                    {
                        id = i.Product.Id,
                        name = i.Product.Name,
                        brand = i.Product.Brand,
                        price = i.Product.Price,
                        imageUrl = i.Product.ImageUrl,
                        quantity = i.Quantity,
                        availableStock = i.Product.StockQuantity,
                        subtotal = i.Subtotal,
                        rxRequired = i.Product.RxRequired
                    }),
                    totalItems = cart.TotalItems,
                    subtotal = cart.Subtotal,
                    shippingFee = cart.ShippingFee,
                    grandTotal = cart.GrandTotal
                });
            }
            return RedirectToAction("Index");
        }

        // GET: /Cart/Checkout
        public IActionResult Checkout()
        {
            var cart = _cartService.GetCart();
            if (cart.TotalItems == 0)
            {
                TempData["ErrorMessage"] = "Votre panier est vide. Veuillez ajouter des produits avant de finaliser votre commande.";
                return RedirectToAction("Index", "Products");
            }
            ViewData["CartCount"] = cart.TotalItems;
            return View(cart);
        }

        // POST: /Cart/ProcessCheckout
        [HttpPost]
        public IActionResult ProcessCheckout(string? fullName, string? phone, string? email, string? city, string? postalCode, string? address, string? deliveryNotes)
        {
            var cart = _cartService.GetCart();
            if (cart.TotalItems == 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Votre panier est vide." });
                }
                TempData["ErrorMessage"] = "Votre panier est vide.";
                return RedirectToAction("Index", "Products");
            }

            // Validate required customer information
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length < 2)
            {
                var msg = "Veuillez renseigner votre prénom et nom.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Checkout");
            }

            if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length < 8)
            {
                var msg = "Veuillez renseigner un numéro de téléphone valide (au moins 8 chiffres) pour le livreur.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Checkout");
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                var msg = "Veuillez indiquer votre ville de livraison.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Checkout");
            }

            if (string.IsNullOrWhiteSpace(address) || address.Trim().Length < 5)
            {
                var msg = "Veuillez indiquer votre adresse de livraison complète (numéro, rue, quartier...).";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Checkout");
            }

            // Verify stock availability
            foreach (var item in cart.Items)
            {
                var product = _productService.GetProductById(item.Product.Id);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    var msg = product == null 
                        ? "Un produit de votre panier n'est plus disponible." 
                        : $"Stock insuffisant pour « {product.Name} » (seulement {product.StockQuantity} unité(s) restante(s)).";

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = msg });
                    }
                    TempData["ErrorMessage"] = msg;
                    return RedirectToAction("Index");
                }
            }

            // Create Order and automatically decrement stock with audit log
            var order = _productService.CreateOrder(
                cart,
                fullName.Trim(),
                phone.Trim(),
                string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                city.Trim(),
                string.IsNullOrWhiteSpace(postalCode) ? "4000" : postalCode.Trim(),
                address.Trim(),
                string.IsNullOrWhiteSpace(deliveryNotes) ? null : deliveryNotes.Trim()
            );

            // Clear session cart
            _cartService.ClearCart();

            TempData["SuccessMessage"] = $"Merci pour votre commande {order.OrderNumber} ! Vos soins sont en cours de préparation à l'officine.";
            TempData["OrderNumber"] = order.OrderNumber;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    orderNumber = order.OrderNumber,
                    redirectUrl = Url.Action("OrderConfirmation", "Cart", new { id = order.OrderNumber })
                });
            }

            return RedirectToAction("OrderConfirmation", new { id = order.OrderNumber });
        }

        public IActionResult OrderConfirmation(string? id)
        {
            ViewData["CartCount"] = 0;
            Order? order = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                order = _productService.GetOrderByNumber(id);
            }
            else if (TempData["OrderNumber"] is string tempNum)
            {
                order = _productService.GetOrderByNumber(tempNum);
            }

            return View(order);
        }

        // GET: /Cart/TrackOrder or /Suivi
        [HttpGet("Suivi")]
        [HttpGet("Cart/TrackOrder")]
        [HttpGet("Cart/Suivi")]
        public IActionResult TrackOrder(string? orderNumber, string? phone)
        {
            ViewData["CartCount"] = _cartService.GetCart().TotalItems;

            Order? order = null;
            List<Order> matchingOrders = new();

            if (!string.IsNullOrWhiteSpace(orderNumber))
            {
                order = _productService.GetOrderByNumber(orderNumber);
                if (order != null)
                {
                    matchingOrders.Add(order);
                }
            }
            else if (!string.IsNullOrWhiteSpace(phone))
            {
                matchingOrders = _productService.GetOrdersByPhone(phone);
                order = matchingOrders.FirstOrDefault();
            }

            ViewBag.SearchOrderNumber = orderNumber?.Trim();
            ViewBag.SearchPhone = phone?.Trim();
            ViewBag.MatchingOrders = matchingOrders;

            return View(order);
        }
    }
}
