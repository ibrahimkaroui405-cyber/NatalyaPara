using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PharmaTrust.Models;
using PharmaTrust.Services;

namespace PharmaTrust.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;

        public HomeController(IProductService productService, ICartService cartService)
        {
            _productService = productService;
            _cartService = cartService;
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
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
