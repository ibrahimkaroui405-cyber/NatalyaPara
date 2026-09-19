using Microsoft.AspNetCore.Mvc;
using PharmaTrust.Models;
using PharmaTrust.Services;

namespace PharmaTrust.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;

        public ProductsController(IProductService productService, ICartService cartService)
        {
            _productService = productService;
            _cartService = cartService;
        }

        // GET: /Products (Catalog)
        public IActionResult Index(string? category, string? q, string sort = "featured", string? brand = null, string? subCategory = null, int page = 1, int pageSize = 12)
        {
            ViewData["CartCount"] = _cartService.GetCart().TotalItems;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;

            var filteredProducts = _productService.FilterProducts(category, q, sort, brand, subCategory);
            var categories = _productService.GetCategories();

            var totalProducts = filteredProducts.Count;
            var pagedProducts = filteredProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new ProductCatalogViewModel
            {
                Products = pagedProducts,
                Categories = categories,
                SelectedCategory = category,
                SelectedSubCategory = subCategory,
                SelectedBrand = brand,
                SearchQuery = q,
                SortOrder = sort,
                TotalProducts = totalProducts,
                CurrentPage = page,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: /Marques or /Products/Marques
        [HttpGet("Marques")]
        [HttpGet("Products/Marques")]
        public IActionResult Marques(string? q, string? letter, string sort = "popular", int page = 1, int pageSize = 24)
        {
            ViewData["CartCount"] = _cartService.GetCart().TotalItems;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 24;

            var allBrands = _productService.GetBrands();

            var queryableBrands = allBrands.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                queryableBrands = queryableBrands.Where(b => b.Name.ToLower().Contains(term) || 
                                                             b.Tagline.ToLower().Contains(term) ||
                                                             b.Description.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(letter) && letter.ToLower() != "all" && letter.ToLower() != "tous")
            {
                var let = letter.Trim().ToUpper();
                if (let == "0-9" || let == "#")
                {
                    queryableBrands = queryableBrands.Where(b => !string.IsNullOrEmpty(b.Name) && char.IsDigit(b.Name[0]));
                }
                else
                {
                    queryableBrands = queryableBrands.Where(b => !string.IsNullOrEmpty(b.Name) && b.Name.StartsWith(let, StringComparison.OrdinalIgnoreCase));
                }
            }

            queryableBrands = sort?.ToLower() switch
            {
                "az" => queryableBrands.OrderBy(b => b.Name),
                "za" => queryableBrands.OrderByDescending(b => b.Name),
                "products-high" => queryableBrands.OrderByDescending(b => b.ProductCount).ThenBy(b => b.Name),
                _ => queryableBrands.OrderByDescending(b => b.ProductCount > 0).ThenByDescending(b => b.ProductCount).ThenBy(b => b.Name)
            };

            var filteredBrandsList = queryableBrands.ToList();
            var totalBrands = filteredBrandsList.Count;
            var pagedBrands = filteredBrandsList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new BrandsPageViewModel
            {
                Brands = pagedBrands,
                AllBrands = allBrands,
                SearchQuery = q,
                SelectedLetter = letter,
                SortOrder = sort ?? "popular",
                TotalBrands = totalBrands,
                CurrentPage = page,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: /Products/Details/1
        public IActionResult Details(int id)
        {
            ViewData["CartCount"] = _cartService.GetCart().TotalItems;

            var product = _productService.GetProductById(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewData["RelatedProducts"] = _productService.GetRelatedProducts(id, 4);

            return View(product);
        }
    }
}
