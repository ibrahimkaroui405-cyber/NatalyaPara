using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaTrust.Models;
using PharmaTrust.Services;
using System.Security.Claims;

namespace PharmaTrust.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            IProductService productService, 
            ICartService cartService, 
            IAuthService authService,
            IWebHostEnvironment webHostEnvironment)
        {
            _productService = productService;
            _cartService = cartService;
            _authService = authService;
            _webHostEnvironment = webHostEnvironment;
        }

        #region Authentication Actions

        // GET: /Admin/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new AdminLoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Admin/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var admin = await _authService.ValidateAdminCredentialsAsync(model.UsernameOrEmail, model.Password);
            if (admin == null)
            {
                ModelState.AddModelError("", "Identifiants invalides. Veuillez vérifier votre nom d'utilisateur/email et mot de passe.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.GivenName, string.IsNullOrEmpty(admin.FullName) ? admin.Username : admin.FullName),
                new Claim(ClaimTypes.Role, admin.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            await _authService.RecordLoginAsync(admin.Id);

            TempData["SuccessMessage"] = $"Bienvenue dans l'espace administration, {admin.FullName} !";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET & POST: /Admin/Logout
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Vous avez été déconnecté avec succès de la session d'administration.";
            return RedirectToAction(nameof(Login));
        }

        #endregion

        // GET: /Admin (Dashboard)
        public IActionResult Index()
        {
            var products = _productService.GetAllProducts();
            var categories = _productService.GetCategories();
            var brands = _productService.GetBrands();
            var orders = _productService.GetAllOrders();
            var movements = _productService.GetStockMovements(limit: 6);

            var vm = new AdminDashboardViewModel
            {
                TotalProducts = products.Count,
                InStockProducts = products.Count(p => p.InStock && p.StockQuantity > 5),
                LowStockProductsCount = products.Count(p => p.InStock && p.StockQuantity <= 5 && p.StockQuantity > 0),
                OutOfStockProducts = products.Count(p => p.StockQuantity <= 0),
                TotalUnitsInStock = products.Sum(p => p.StockQuantity),
                TotalOrdersCount = orders.Count,
                RxProducts = products.Count(p => p.RxRequired),
                TotalCategories = categories.Count,
                TotalBrands = brands.Count,
                EstimatedStockValue = products.Sum(p => p.Price * p.StockQuantity),
                RecentProducts = products.Take(6).ToList(),
                RecentOrders = orders.Take(5).ToList(),
                RecentMovements = movements,
                Categories = categories
            };

            return View(vm);
        }

        // GET: /Admin/Products
        public IActionResult Products(string? q, string? category, string? brand, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var allFiltered = _productService.FilterProducts(category, q, "newest", brand);
            var categories = _productService.GetCategories();
            var brands = _productService.GetAllBrandNames();

            var totalCount = allFiltered.Count;
            var pagedProducts = allFiltered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

            ViewBag.SearchQuery = q;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedBrand = brand;
            ViewBag.Categories = categories;
            ViewBag.Brands = brands;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            return View(pagedProducts);
        }

        // GET: /Admin/CreateProduct
        public IActionResult CreateProduct()
        {
            ViewBag.Categories = _productService.GetCategories();
            ViewBag.Brands = _productService.GetAllBrandNames();
            return View(new Product { InStock = true, StockQuantity = 20, Rating = 5.0, ReviewCount = 1 });
        }

        // POST: /Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, IFormFile? imageFile, string? ingredientsText, string? dosageText, string? customBrand)
        {
            if (!string.IsNullOrWhiteSpace(customBrand) && (product.Brand == "__new__" || string.IsNullOrWhiteSpace(product.Brand)))
            {
                product.Brand = customBrand.Trim();
            }

            if (string.IsNullOrWhiteSpace(product.Name) || product.Price <= 0 || string.IsNullOrWhiteSpace(product.Brand) || product.Brand == "__new__")
            {
                ModelState.AddModelError("", "Veuillez renseigner un nom valide, une marque valide et un prix supérieur à 0.");
                ViewBag.Categories = _productService.GetCategories();
                ViewBag.Brands = _productService.GetAllBrandNames();
                return View(product);
            }

            // Handle PC Image Upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                product.ImageUrl = "/uploads/" + uniqueFileName;
            }

            if (string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                product.ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCZ2e8wA5GHLGeqd2-GRkEpnXg3tA-cwf4FKgZqpSmcT3wEIXTLV2lZ2L7GWzkAHpOAd_yxJChfy12O_f3J-tdCkJ1hMrp-hb0viwy-3cPCEc1nH2NNJ9BnpB88WFxl-OFICe7ge5L7kVIP3WTueJK9ZKOyWK8IrRHiDFof39zraR-hgfvzehfm5NhC0bPCQNJFsbiitlvUbTG_8ARaC45Aead4CTSD9sZyvUqIq-ekpxxO5jM9_tRGAw";
            }

            if (!string.IsNullOrWhiteSpace(ingredientsText))
            {
                product.Ingredients = ingredientsText
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(dosageText))
            {
                product.DosageInstructions = dosageText
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            _productService.AddProduct(product);
            TempData["SuccessMessage"] = $"Le produit « {product.Name} » a été ajouté avec succès au catalogue.";

            return RedirectToAction(nameof(Products));
        }

        // GET: /Admin/EditProduct/5
        public IActionResult EditProduct(int id)
        {
            var product = _productService.GetProductById(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Produit introuvable.";
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = _productService.GetCategories();
            ViewBag.Brands = _productService.GetAllBrandNames();
            ViewBag.IngredientsText = string.Join(Environment.NewLine, product.Ingredients);
            ViewBag.DosageText = string.Join(Environment.NewLine, product.DosageInstructions);

            return View(product);
        }

        // POST: /Admin/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product product, IFormFile? imageFile, string? ingredientsText, string? dosageText, string? customBrand)
        {
            if (!string.IsNullOrWhiteSpace(customBrand) && (product.Brand == "__new__" || string.IsNullOrWhiteSpace(product.Brand)))
            {
                product.Brand = customBrand.Trim();
            }

            if (string.IsNullOrWhiteSpace(product.Name) || product.Price <= 0 || string.IsNullOrWhiteSpace(product.Brand) || product.Brand == "__new__")
            {
                ModelState.AddModelError("", "Veuillez renseigner un nom valide, une marque valide et un prix supérieur à 0.");
                ViewBag.Categories = _productService.GetCategories();
                ViewBag.Brands = _productService.GetAllBrandNames();
                return View(product);
            }

            // Handle PC Image Upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                product.ImageUrl = "/uploads/" + uniqueFileName;
            }

            if (!string.IsNullOrWhiteSpace(ingredientsText))
            {
                product.Ingredients = ingredientsText
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            else
            {
                product.Ingredients = new List<string>();
            }

            if (!string.IsNullOrWhiteSpace(dosageText))
            {
                product.DosageInstructions = dosageText
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            else
            {
                product.DosageInstructions = new List<string>();
            }

            var updated = _productService.UpdateProduct(product);
            if (updated)
            {
                TempData["SuccessMessage"] = $"Le produit « {product.Name} » a été mis à jour avec succès.";
            }
            else
            {
                TempData["ErrorMessage"] = "Impossible de mettre à jour le produit.";
            }

            return RedirectToAction(nameof(Products));
        }

        // POST: /Admin/DeleteProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            var deleted = _productService.DeleteProduct(id);
            if (deleted)
            {
                TempData["SuccessMessage"] = "Le produit a été supprimé du catalogue.";
            }
            else
            {
                TempData["ErrorMessage"] = "Impossible de supprimer le produit.";
            }

            return RedirectToAction(nameof(Products));
        }

        // POST: /Admin/ToggleStock/5
        [HttpPost]
        public IActionResult ToggleStock(int id)
        {
            var product = _productService.GetProductById(id);
            if (product != null)
            {
                if (product.StockQuantity > 0)
                {
                    product.StockQuantity = 0;
                }
                else
                {
                    product.StockQuantity = 15;
                }
                _productService.UpdateProduct(product);
                TempData["SuccessMessage"] = $"Statut du stock mis à jour pour « {product.Name} » ({(product.InStock ? $"{product.StockQuantity} unités en stock" : "En Rupture")}).";
            }
            return RedirectToAction(nameof(Products));
        }

        // GET/POST: /Admin/Categories
        public IActionResult Categories()
        {
            var categories = _productService.GetCategories();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(Category category, IFormFile? iconFile)
        {
            if (!string.IsNullOrWhiteSpace(category.Name))
            {
                // Handle PC Image Upload for Category Icon
                if (iconFile != null && iconFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(iconFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await iconFile.CopyToAsync(fileStream);
                    }
                    category.Icon = "/uploads/" + uniqueFileName;
                }

                if (string.IsNullOrWhiteSpace(category.Icon))
                {
                    category.Icon = "spa";
                }

                _productService.AddCategory(category);
                TempData["SuccessMessage"] = $"La catégorie « {category.Name} » a été créée avec succès.";
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int id)
        {
            var deleted = _productService.DeleteCategory(id);
            if (deleted)
            {
                TempData["SuccessMessage"] = "La catégorie a été supprimée.";
            }
            return RedirectToAction(nameof(Categories));
        }

        // GET: /Admin/Brands
        public IActionResult Brands(string? q, string? category, int page = 1, int pageSize = 12)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;

            var allBrands = _productService.GetBrands();
            var query = allBrands.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all")
            {
                query = query.Where(b => b.Category.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                                         b.CategorySlug.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(term) ||
                                         b.Description.ToLower().Contains(term) ||
                                         b.Tagline.ToLower().Contains(term) ||
                                         b.Origin.ToLower().Contains(term) ||
                                         b.SignatureProduct.ToLower().Contains(term));
            }

            var filteredList = query.ToList();
            var totalFiltered = filteredList.Count;
            var pagedBrands = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalFiltered / pageSize));

            var categories = allBrands.Select(b => b.Category).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();

            ViewBag.SearchQuery = q;
            ViewBag.SelectedCategory = category;
            ViewBag.BrandCategories = categories;
            ViewBag.TotalBrands = allBrands.Count;
            ViewBag.FeaturedBrands = allBrands.Count(b => b.IsFeatured);
            ViewBag.TotalProductsInBrands = allBrands.Sum(b => b.ProductCount);
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalFiltered;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;

            return View(pagedBrands);
        }

        // POST: /Admin/AddBrand
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBrand(BrandItem brand, IFormFile? imageFile, IFormFile? logoFile, string? keyFeaturesText)
        {
            if (string.IsNullOrWhiteSpace(brand.Name))
            {
                TempData["ErrorMessage"] = "Le nom de la marque est obligatoire.";
                return RedirectToAction(nameof(Brands));
            }

            // Handle PC Image Upload for Brand Banner Image
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = "brand_" + Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                brand.ImageUrl = "/uploads/" + uniqueFileName;
            }

            // Handle PC Image Upload for Brand Logo
            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = "logo_" + Guid.NewGuid().ToString() + Path.GetExtension(logoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(fileStream);
                }
                brand.LogoUrl = "/uploads/" + uniqueFileName;
            }

            if (!string.IsNullOrWhiteSpace(keyFeaturesText))
            {
                brand.KeyFeatures = keyFeaturesText
                    .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            _productService.AddBrand(brand);
            TempData["SuccessMessage"] = $"La marque « {brand.Name} » a été ajoutée avec succès.";

            return RedirectToAction(nameof(Brands));
        }

        // POST: /Admin/EditBrand
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBrand(string originalSlug, BrandItem brand, IFormFile? imageFile, IFormFile? logoFile, string? keyFeaturesText)
        {
            if (string.IsNullOrWhiteSpace(originalSlug) || string.IsNullOrWhiteSpace(brand.Name))
            {
                TempData["ErrorMessage"] = "Informations de marque invalides.";
                return RedirectToAction(nameof(Brands));
            }

            // Handle PC Image Upload for Brand Banner Image
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = "brand_" + Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                brand.ImageUrl = "/uploads/" + uniqueFileName;
            }

            // Handle PC Image Upload for Brand Logo
            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = "logo_" + Guid.NewGuid().ToString() + Path.GetExtension(logoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(fileStream);
                }
                brand.LogoUrl = "/uploads/" + uniqueFileName;
            }

            if (!string.IsNullOrWhiteSpace(keyFeaturesText))
            {
                brand.KeyFeatures = keyFeaturesText
                    .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            var updated = _productService.UpdateBrand(originalSlug, brand);
            if (updated)
            {
                TempData["SuccessMessage"] = $"La marque « {brand.Name} » a été mise à jour avec succès.";
            }
            else
            {
                TempData["ErrorMessage"] = "Impossible de mettre à jour la marque.";
            }

            return RedirectToAction(nameof(Brands));
        }

        // POST: /Admin/DeleteBrand
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBrand(string slug)
        {
            var brand = _productService.GetBrandBySlug(slug);
            var brandName = brand?.Name ?? slug;
            var deleted = _productService.DeleteBrand(slug);
            if (deleted)
            {
                TempData["SuccessMessage"] = $"La marque « {brandName} » a été supprimée.";
            }
            else
            {
                TempData["ErrorMessage"] = "Impossible de supprimer la marque.";
            }

            return RedirectToAction(nameof(Brands));
        }

        // ==========================================
        // DEDICATED STOCK MANAGEMENT (/Admin/Stock)
        // ==========================================

        // GET: /Admin/Stock
        public IActionResult Stock(string? q, string? category, string? status, int page = 1, int pageSize = 15)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 15;

            var allProducts = _productService.GetAllProducts();
            var categories = _productService.GetCategories();
            var movements = _productService.GetStockMovements(limit: 30);

            var query = allProducts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all" && category.ToLower() != "tous")
            {
                var catTerm = category.Trim().ToLower();
                query = query.Where(p => p.CategorySlug.ToLower() == catTerm || p.CategoryName.ToLower() == catTerm);
            }

            if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
            {
                if (status.Equals("low", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p => p.InStock && p.StockQuantity <= 5 && p.StockQuantity > 0);
                }
                else if (status.Equals("out", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p => p.StockQuantity <= 0);
                }
                else if (status.Equals("instock", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(p => p.InStock && p.StockQuantity > 5);
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                var cleanNum = term.TrimStart('#').Trim();
                int.TryParse(cleanNum, out int parsedId);

                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         p.Brand.ToLower().Contains(term) ||
                                         p.CategoryName.ToLower().Contains(term) ||
                                         (parsedId > 0 && p.Id == parsedId));
            }

            var filteredProducts = query.ToList();
            var totalFiltered = filteredProducts.Count;
            var pagedProducts = filteredProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var vm = new StockManagementViewModel
            {
                TotalProducts = allProducts.Count,
                InStockCount = allProducts.Count(p => p.InStock && p.StockQuantity > 5),
                LowStockCount = allProducts.Count(p => p.InStock && p.StockQuantity <= 5 && p.StockQuantity > 0),
                OutOfStockCount = allProducts.Count(p => p.StockQuantity <= 0),
                TotalUnitsInStock = allProducts.Sum(p => p.StockQuantity),
                TotalInventoryValue = allProducts.Sum(p => p.Price * p.StockQuantity),
                Products = pagedProducts,
                LowStockProducts = allProducts.Where(p => p.InStock && p.StockQuantity <= 5 && p.StockQuantity > 0).ToList(),
                OutOfStockProducts = allProducts.Where(p => p.StockQuantity <= 0).ToList(),
                RecentMovements = movements,
                Categories = categories,
                FilterCategory = category,
                FilterStatus = status,
                SearchQuery = q,
                TotalFilteredCount = totalFiltered,
                CurrentPage = page,
                PageSize = pageSize
            };

            return View(vm);
        }

        // POST: /Admin/QuickRestock
        [HttpPost]
        public IActionResult QuickRestock(int id, int amount = 10, string? returnUrl = null)
        {
            var product = _productService.GetProductById(id);
            if (product != null)
            {
                _productService.RestockProduct(id, amount, $"Réapprovisionnement rapide (+{amount})");
                TempData["SuccessMessage"] = $"Stock réapprovisionné pour « {product.Name} » (+{amount} unités). Nouveau stock : {product.StockQuantity}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Produit introuvable.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Stock));
        }

        // POST: /Admin/AdjustStock
        [HttpPost]
        public IActionResult AdjustStock(int id, int quantity, string? reason, string? returnUrl = null)
        {
            var product = _productService.GetProductById(id);
            if (product != null)
            {
                var r = string.IsNullOrWhiteSpace(reason) ? "Ajustement inventaire manuel" : reason.Trim();
                _productService.AdjustStock(id, quantity, r);
                TempData["SuccessMessage"] = $"Stock ajusté pour « {product.Name} » à {quantity} unités. Motif : {r}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Produit introuvable.";
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Stock));
        }

        // ==========================================
        // DYNAMIC ORDERS MANAGEMENT (/Admin/Orders)
        // ==========================================

        // GET: /Admin/Orders
        public IActionResult Orders(string? q, string? status, int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var allOrders = _productService.GetAllOrders();
            var query = allOrders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status.ToLower() != "all")
            {
                query = query.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                var cleanOrderNum = term.TrimStart('#').Trim();

                query = query.Where(o => o.OrderNumber.ToLower().Contains(term) ||
                                         o.OrderNumber.ToLower().Contains(cleanOrderNum) ||
                                         o.CustomerName.ToLower().Contains(term) ||
                                         o.City.ToLower().Contains(term) ||
                                         o.Phone.ToLower().Contains(term) ||
                                         (o.Address != null && o.Address.ToLower().Contains(term)) ||
                                         (o.DeliveryNotes != null && o.DeliveryNotes.ToLower().Contains(term)) ||
                                         (o.Items != null && o.Items.Any(i => i.ProductName.ToLower().Contains(term))));
            }

            var ordersList = query.ToList();
            var totalCount = ordersList.Count;
            var pagedOrders = ordersList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));

            ViewBag.SearchQuery = q;
            ViewBag.SelectedStatus = status;
            ViewBag.TotalOrders = allOrders.Count;
            ViewBag.FilteredCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.PendingOrders = allOrders.Count(o => o.Status == "En attente" || o.Status == "En préparation");
            ViewBag.DeliveredOrders = allOrders.Count(o => o.Status == "Livrée");
            ViewBag.CancelledOrders = allOrders.Count(o => o.Status == "Annulée");
            ViewBag.TotalRevenue = allOrders.Where(o => o.Status != "Annulée").Sum(o => o.TotalAmount);

            return View(pagedOrders);
        }

        // POST: /Admin/UpdateOrderStatus
        [HttpPost]
        public IActionResult UpdateOrderStatus(string orderNumber, string status)
        {
            var order = _productService.GetOrderByNumber(orderNumber);
            if (order != null)
            {
                bool wasAlreadyCancelled = order.Status.Equals("Annulée", StringComparison.OrdinalIgnoreCase);
                _productService.UpdateOrderStatus(orderNumber, status);

                if (status == "Annulée" && !wasAlreadyCancelled)
                {
                    TempData["SuccessMessage"] = $"Commande {orderNumber} annulée. Les {order.Items.Sum(i => i.Quantity)} articles ont été automatiquement restitués au stock inventaire !";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Statut de la commande {orderNumber} mis à jour : « {status} ».";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Commande introuvable.";
            }

            return RedirectToAction(nameof(Orders));
        }

        #region Special Offers Admin

        // GET: /Admin/SpecialOffers
        public IActionResult SpecialOffers()
        {
            var offers = _productService.GetAllSpecialOffers();
            ViewBag.Products = _productService.GetAllProducts();
            return View(offers);
        }

        // POST: /Admin/CreateSpecialOffer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSpecialOffer(SpecialOffer offer, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(offer.Title))
            {
                TempData["ErrorMessage"] = "Le titre de l'offre est obligatoire.";
                return RedirectToAction(nameof(SpecialOffers));
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = "promo_" + Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                offer.ImageUrl = "/uploads/" + uniqueFileName;
            }
            else if (string.IsNullOrWhiteSpace(offer.ImageUrl))
            {
                offer.ImageUrl = "/images/hero_skincare.jpg";
            }

            _productService.AddSpecialOffer(offer);
            TempData["SuccessMessage"] = $"L'offre spéciale « {offer.Title} » a été ajoutée avec succès !";
            return RedirectToAction(nameof(SpecialOffers));
        }

        // POST: /Admin/EditSpecialOffer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSpecialOffer(SpecialOffer offer, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(offer.Title))
            {
                TempData["ErrorMessage"] = "Le titre de l'offre est obligatoire.";
                return RedirectToAction(nameof(SpecialOffers));
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = "promo_" + Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                offer.ImageUrl = "/uploads/" + uniqueFileName;
            }

            _productService.UpdateSpecialOffer(offer);
            TempData["SuccessMessage"] = $"L'offre spéciale « {offer.Title} » a été mise à jour !";
            return RedirectToAction(nameof(SpecialOffers));
        }

        // POST: /Admin/DeleteSpecialOffer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSpecialOffer(int id)
        {
            var offer = _productService.GetSpecialOfferById(id);
            if (offer != null)
            {
                _productService.DeleteSpecialOffer(id);
                TempData["SuccessMessage"] = $"L'offre « {offer.Title} » a été supprimée.";
            }
            return RedirectToAction(nameof(SpecialOffers));
        }

        // POST: /Admin/ToggleSpecialOffer
        [HttpPost]
        public IActionResult ToggleSpecialOffer(int id)
        {
            _productService.ToggleSpecialOffer(id);
            TempData["SuccessMessage"] = "Statut de l'offre mis à jour.";
            return RedirectToAction(nameof(SpecialOffers));
        }

        #endregion

        #region Stories & Demo Reels Admin

        // GET: /Admin/Stories
        public IActionResult Stories()
        {
            var stories = _productService.GetAllStoryReels();
            ViewBag.Products = _productService.GetAllProducts();
            ViewBag.Brands = _productService.GetAllBrandNames();
            return View(stories);
        }

        // POST: /Admin/CreateStory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStory(StoryReel story, IFormFile? videoFile, IFormFile? posterFile)
        {
            if (string.IsNullOrWhiteSpace(story.Title))
            {
                story.Title = "Démonstration Soin";
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Handle Video Upload
            if (videoFile != null && videoFile.Length > 0)
            {
                var uniqueVideoName = "story_vid_" + Guid.NewGuid().ToString("N") + Path.GetExtension(videoFile.FileName);
                var videoPath = Path.Combine(uploadsFolder, uniqueVideoName);
                using (var fileStream = new FileStream(videoPath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(fileStream);
                }
                story.VideoUrl = "/uploads/" + uniqueVideoName;
            }
            else if (string.IsNullOrWhiteSpace(story.VideoUrl))
            {
                story.VideoUrl = "/videos/v1.mp4";
            }

            // Handle Poster Upload
            if (posterFile != null && posterFile.Length > 0)
            {
                var uniquePosterName = "story_post_" + Guid.NewGuid().ToString("N") + Path.GetExtension(posterFile.FileName);
                var posterPath = Path.Combine(uploadsFolder, uniquePosterName);
                using (var fileStream = new FileStream(posterPath, FileMode.Create))
                {
                    await posterFile.CopyToAsync(fileStream);
                }
                story.PosterUrl = "/uploads/" + uniquePosterName;
            }

            _productService.AddStoryReel(story);
            TempData["SuccessMessage"] = $"La story vidéo « {story.BrandName} » a été ajoutée avec succès !";
            return RedirectToAction(nameof(Stories));
        }

        // POST: /Admin/EditStory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStory(StoryReel story, IFormFile? videoFile, IFormFile? posterFile)
        {
            if (string.IsNullOrWhiteSpace(story.Title))
            {
                story.Title = "Démonstration Soin";
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Handle Video Upload
            if (videoFile != null && videoFile.Length > 0)
            {
                var uniqueVideoName = "story_vid_" + Guid.NewGuid().ToString("N") + Path.GetExtension(videoFile.FileName);
                var videoPath = Path.Combine(uploadsFolder, uniqueVideoName);
                using (var fileStream = new FileStream(videoPath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(fileStream);
                }
                story.VideoUrl = "/uploads/" + uniqueVideoName;
            }

            // Handle Poster Upload
            if (posterFile != null && posterFile.Length > 0)
            {
                var uniquePosterName = "story_post_" + Guid.NewGuid().ToString("N") + Path.GetExtension(posterFile.FileName);
                var posterPath = Path.Combine(uploadsFolder, uniquePosterName);
                using (var fileStream = new FileStream(posterPath, FileMode.Create))
                {
                    await posterFile.CopyToAsync(fileStream);
                }
                story.PosterUrl = "/uploads/" + uniquePosterName;
            }

            _productService.UpdateStoryReel(story);
            TempData["SuccessMessage"] = $"La story « {story.Title} » a été mise à jour !";
            return RedirectToAction(nameof(Stories));
        }

        // POST: /Admin/DeleteStory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteStory(int id)
        {
            var story = _productService.GetStoryReelById(id);
            if (story != null)
            {
                _productService.DeleteStoryReel(id);
                TempData["SuccessMessage"] = $"La story « {story.Title} » a été supprimée.";
            }
            return RedirectToAction(nameof(Stories));
        }

        // POST: /Admin/ToggleStory
        [HttpPost]
        public IActionResult ToggleStory(int id)
        {
            _productService.ToggleStoryReel(id);
            TempData["SuccessMessage"] = "Statut de la story mis à jour.";
            return RedirectToAction(nameof(Stories));
        }

        #endregion
    }

    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProductsCount { get; set; }
        public int OutOfStockProducts { get; set; }
        public int TotalUnitsInStock { get; set; }
        public int TotalOrdersCount { get; set; }
        public int RxProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBrands { get; set; }
        public decimal EstimatedStockValue { get; set; }
        public List<Product> RecentProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<StockMovement> RecentMovements { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
