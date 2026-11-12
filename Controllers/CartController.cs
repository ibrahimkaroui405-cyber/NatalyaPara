using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PharmaTrust.Models;
using PharmaTrust.Services;

namespace PharmaTrust.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly ITelegramService _telegramService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService, 
            IProductService productService,
            ITelegramService telegramService,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _productService = productService;
            _telegramService = telegramService;
            _logger = logger;
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
        [EnableRateLimiting("cart-api")]
        public IActionResult Add(int id, int quantity = 1)
        {
            if (id <= 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Identifiant de soin invalide." });
                }
                return RedirectToAction("Index");
            }

            quantity = Math.Clamp(quantity, 1, 50);

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
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnu";
            _logger.LogInformation("Ajout au panier: Produit #{ProductId} '{ProductName}' (Qté: {Quantity}) depuis l'adresse IP {ClientIp}", product.Id, product.Name, quantity, clientIp);
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
        [EnableRateLimiting("cart-api")]
        public IActionResult Update(int id, int quantity)
        {
            if (id <= 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Identifiant de soin invalide." });
                }
                return RedirectToAction("Index");
            }

            quantity = Math.Clamp(quantity, 0, 50);

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
        [EnableRateLimiting("cart-api")]
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
            ViewBag.DeliveryRegions = _productService.GetAllDeliveryRegions(onlyActive: true);
            return View(cart);
        }

        // GET: /Cart/GetDeliveryRegions
        [HttpGet]
        public IActionResult GetDeliveryRegions()
        {
            var regions = _productService.GetAllDeliveryRegions(onlyActive: true)
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    fee = r.DeliveryFee,
                    deliveryTime = r.EstimatedDeliveryTime
                });
            return Json(regions);
        }

        // POST: /Cart/ProcessCheckout
        [HttpPost]
        [EnableRateLimiting("checkout")]
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

            // Sanitize & Validate full name
            var cleanFullName = fullName?.Trim() ?? "";
            if (cleanFullName.Length < 2 || cleanFullName.Length > 80)
            {
                var msg = "Veuillez renseigner un prénom et nom valide (entre 2 et 80 caractères).";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Checkout");
            }

            // Clean phone digits & validate Tunisian phone number
            var digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
            if (digits.StartsWith("216") && digits.Length == 11)
            {
                digits = digits.Substring(3);
            }

            if (digits.Length != 8 || !(digits.StartsWith("2") || digits.StartsWith("3") || digits.StartsWith("4") || digits.StartsWith("5") || digits.StartsWith("7") || digits.StartsWith("9")))
            {
                var msg = "Veuillez renseigner un numéro de téléphone tunisien valide à 8 chiffres pour le livreur (ex: 21 040 266).";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Checkout");
            }
            var cleanPhone = digits;

            // Validate optional email format
            string? cleanEmail = null;
            if (!string.IsNullOrWhiteSpace(email))
            {
                cleanEmail = email.Trim();
                if (cleanEmail.Length > 120 || !System.Text.RegularExpressions.Regex.IsMatch(cleanEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    var msg = "Veuillez renseigner une adresse email valide ou laisser le champ vide.";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = msg });
                    }
                    TempData["ErrorMessage"] = msg;
                    return RedirectToAction("Checkout");
                }
            }

            // Validate city
            var cleanCity = city?.Trim() ?? "";
            if (cleanCity.Length < 2 || cleanCity.Length > 60)
            {
                var msg = "Veuillez indiquer votre ville ou région de livraison.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Checkout");
            }

            // Validate address
            var cleanAddress = address?.Trim() ?? "";
            if (cleanAddress.Length < 5 || cleanAddress.Length > 200)
            {
                var msg = "Veuillez indiquer votre adresse de livraison complète (numéro, rue, quartier... entre 5 et 200 caractères).";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Checkout");
            }

            var cleanNotes = string.IsNullOrWhiteSpace(deliveryNotes) ? null : deliveryNotes.Trim();
            if (cleanNotes != null && cleanNotes.Length > 400)
            {
                cleanNotes = cleanNotes.Substring(0, 400);
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
                cleanFullName,
                cleanPhone,
                cleanEmail,
                cleanCity,
                string.IsNullOrWhiteSpace(postalCode) ? "4000" : postalCode.Trim(),
                cleanAddress,
                cleanNotes
            );

            // Clear session cart
            _cartService.ClearCart();

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnu";
            _logger.LogInformation("Nouvelle commande {OrderNumber} VALIDÉE avec succès ! Client: '{CustomerName}', Tél: '{Phone}', Ville: '{City}', Total: {GrandTotal:F3} DT, Articles: {ItemCount}, IP: {ClientIp}",
                order.OrderNumber, order.CustomerName, order.Phone, order.City, order.TotalAmount, order.Items.Count, clientIp);

            // Automatic push notification to Telegram
            _ = Task.Run(async () =>
            {
                try
                {
                    await _telegramService.SendOrderNotificationAsync(order);
                }
                catch { }
            });

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

            ViewBag.WhatsAppUrl = order != null ? _telegramService.BuildWhatsAppUrl(order) : "";
            ViewBag.OwnerPhone = "21040266";

            return View(order);
        }

        // GET: /Cart/TrackOrder or /Suivi
        [HttpGet("Suivi")]
        [HttpGet("Cart/TrackOrder")]
        [HttpGet("Cart/Suivi")]
        [EnableRateLimiting("cart-api")]
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
