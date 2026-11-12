using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PharmaTrust.Models;
using PharmaTrust.Services;

namespace PharmaTrust.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IProductService productService, 
            ICartService cartService,
            ILogger<HomeController> logger)
        {
            _productService = productService;
            _cartService = cartService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewData["CartCount"] = _cartService.GetCart().TotalItems;

            var allProducts = _productService.GetAllProducts();
            var discountedProducts = allProducts.Where(p => p.OriginalPrice > p.Price).Take(4).ToList();

            var viewModel = new HomeViewModel
            {
                Categories = _productService.GetCategories(),
                FeaturedProducts = _productService.GetFeaturedProducts(),
                DiscountedProducts = discountedProducts,
                PromotionalOffers = _productService.GetActiveSpecialOffers(),
                StoryReels = _productService.GetActiveStoryReels()
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("Home/Error")]
        [Route("Error")]
        public IActionResult Error(int? statusCode = null)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "inconnu";
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            int code = statusCode ?? HttpContext.Response.StatusCode;
            if (code <= 200 && exceptionFeature != null) code = 500;

            var model = new ErrorViewModel
            {
                RequestId = requestId,
                StatusCode = code
            };

            if (code == 404)
            {
                model.Title = "Page ou Soin Introuvable (404)";
                model.Description = "Le produit ou la page demandée n'existe pas ou a été déplacée. Explorez notre catalogue pour retrouver vos soins favoris.";
                _logger.LogWarning("Page introuvable (404) depuis l'adresse IP {ClientIp}: {Path}", clientIp, HttpContext.Request.Path);
            }
            else if (code == 403 || code == 401)
            {
                model.Title = $"Accès Non Autorisé ({code})";
                model.Description = "Vous n'avez pas les autorisations nécessaires pour accéder à cette zone privée.";
                _logger.LogWarning("Accès refusé ({StatusCode}) depuis l'adresse IP {ClientIp}: {Path}", code, clientIp, HttpContext.Request.Path);
            }
            else
            {
                model.StatusCode = 500;
                model.Title = "Interruption Momentanée (500)";
                model.Description = "Une erreur inattendue est survenue. Vos données et votre panier sont conservés en toute sécurité. Veuillez réessayer dans quelques instants.";

                if (exceptionFeature?.Error != null)
                {
                    _logger.LogError(exceptionFeature.Error, "Erreur non gérée (500) sur le chemin '{Path}' depuis l'adresse IP {ClientIp} [RequestId: {RequestId}]", 
                        exceptionFeature.Path, clientIp, requestId);
                }
            }

            try
            {
                ViewData["CartCount"] = _cartService.GetCart().TotalItems;
            }
            catch
            {
                ViewData["CartCount"] = 0;
            }

            return View("~/Views/Shared/Error.cshtml", model);
        }
    }
}
