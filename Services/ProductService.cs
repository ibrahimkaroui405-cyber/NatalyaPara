using Microsoft.EntityFrameworkCore;
using PharmaTrust.Data;
using PharmaTrust.Models;
using System.Text.RegularExpressions;

namespace PharmaTrust.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        #region Brands Management

        public List<BrandItem> GetBrands()
        {
            var brands = _context.Brands.AsNoTracking().ToList();
            var products = _context.Products.AsNoTracking().Select(p => p.Brand).ToList();
            var brandCounts = products
                .Where(b => !string.IsNullOrEmpty(b))
                .GroupBy(b => b.ToLower())
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var brand in brands)
            {
                var lowerName = brand.Name.ToLower();
                brand.ProductCount = brandCounts.TryGetValue(lowerName, out var count) ? count : 0;
            }

            return brands;
        }

        public BrandItem? GetBrandBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            var trimmed = slug.Trim().ToLower();
            var brand = _context.Brands.FirstOrDefault(b => b.Slug.ToLower() == trimmed);
            if (brand != null && !string.IsNullOrEmpty(brand.Name))
            {
                var bName = brand.Name.ToLower();
                brand.ProductCount = _context.Products.Count(p => p.Brand.ToLower() == bName);
            }
            return brand;
        }

        public BrandItem? GetBrandByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var trimmed = name.Trim().ToLower();
            var brand = _context.Brands.FirstOrDefault(b => b.Name.ToLower() == trimmed);
            if (brand != null && !string.IsNullOrEmpty(brand.Name))
            {
                var bName = brand.Name.ToLower();
                brand.ProductCount = _context.Products.Count(p => p.Brand.ToLower() == bName);
            }
            return brand;
        }

        public List<string> GetAllBrandNames()
        {
            return _context.Brands
                .Select(b => b.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OrderBy(b => b)
                .ToList();
        }

        public void EnsureBrandExists(string brandName)
        {
            if (string.IsNullOrWhiteSpace(brandName)) return;
            var trimmed = brandName.Trim();
            var lower = trimmed.ToLower();

            if (!_context.Brands.Any(b => b.Name.ToLower() == lower))
            {
                var initials = string.Concat(trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpper(w[0]))).Take(3);
                var initialsStr = string.Join("", initials);
                if (string.IsNullOrEmpty(initialsStr)) initialsStr = "MB";

                var slug = trimmed.ToLower()
                    .Replace(" ", "-")
                    .Replace("&", "et")
                    .Replace("é", "e")
                    .Replace("è", "e")
                    .Replace("ê", "e")
                    .Replace("à", "a")
                    .Replace("'", "");

                // Ensure unique slug
                if (_context.Brands.Any(b => b.Slug == slug))
                {
                    slug = $"{slug}-{DateTime.Now.Ticks % 1000}";
                }

                _context.Brands.Add(new BrandItem
                {
                    Name = trimmed,
                    Slug = slug,
                    Tagline = $"Laboratoire Partenaire Certifié - {trimmed}",
                    Description = $"Sélection experte et soins authentiques du laboratoire {trimmed}, formulés selon les plus hauts standards dermo-cosmétiques.",
                    Origin = "Officine Certifiée",
                    Category = "Dermo-Cosmétique & Soins",
                    CategorySlug = slug,
                    LogoInitials = initialsStr,
                    Icon = "verified",
                    ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=800&q=80",
                    ProductCount = _context.Products.Count(p => p.Brand.ToLower() == lower),
                    SignatureProduct = $"Gamme de soins {trimmed}",
                    BenefitDescription = "Haute tolérance cutanée et efficacité pharmaceutique prouvée.",
                    KeyFeatures = new List<string> { "Authenticité 100%", "Contrôle Qualité", "Conseil Officinal" },
                    IsFeatured = false,
                    Rating = 4.9,
                    ReviewCount = 45,
                    Price = 19.90m,
                    BadgeText = "Laboratoire Officinal"
                });

                _context.SaveChanges();
            }
        }

        public void AddBrand(BrandItem brand)
        {
            if (string.IsNullOrWhiteSpace(brand.Name)) return;
            brand.Name = brand.Name.Trim();

            if (string.IsNullOrWhiteSpace(brand.Slug))
            {
                brand.Slug = brand.Name.ToLower()
                    .Replace(" ", "-")
                    .Replace("&", "et")
                    .Replace("é", "e")
                    .Replace("è", "e")
                    .Replace("ê", "e")
                    .Replace("à", "a")
                    .Replace("'", "");
            }
            brand.Slug = brand.Slug.Trim();
            brand.CategorySlug = brand.Slug;

            if (string.IsNullOrWhiteSpace(brand.LogoInitials))
            {
                var initials = string.Concat(brand.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => char.ToUpper(w[0]))).Take(3);
                brand.LogoInitials = string.Join("", initials);
                if (string.IsNullOrEmpty(brand.LogoInitials)) brand.LogoInitials = "MB";
            }

            if (string.IsNullOrWhiteSpace(brand.Tagline))
            {
                brand.Tagline = $"Laboratoire Partenaire Certifié - {brand.Name}";
            }
            if (string.IsNullOrWhiteSpace(brand.Description))
            {
                brand.Description = $"Sélection experte et soins authentiques du laboratoire {brand.Name}, formulés selon les plus hauts standards dermo-cosmétiques.";
            }
            if (string.IsNullOrWhiteSpace(brand.Origin))
            {
                brand.Origin = "Officine Certifiée";
            }
            if (string.IsNullOrWhiteSpace(brand.Category))
            {
                brand.Category = "Dermo-Cosmétique";
            }
            if (string.IsNullOrWhiteSpace(brand.Icon))
            {
                brand.Icon = "verified";
            }
            if (string.IsNullOrWhiteSpace(brand.ImageUrl))
            {
                brand.ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=800&q=80";
            }
            if (string.IsNullOrWhiteSpace(brand.BadgeText))
            {
                brand.BadgeText = "Laboratoire Officinal";
            }
            if (string.IsNullOrWhiteSpace(brand.SignatureProduct))
            {
                brand.SignatureProduct = $"Gamme de soins {brand.Name}";
            }
            if (brand.Rating <= 0)
            {
                brand.Rating = 4.9;
            }
            if (brand.ReviewCount <= 0)
            {
                brand.ReviewCount = 45;
            }

            var existing = _context.Brands.FirstOrDefault(b => b.Slug.ToLower() == brand.Slug.ToLower());
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(brand);
            }
            else
            {
                _context.Brands.Add(brand);
            }

            _context.SaveChanges();
        }

        public bool UpdateBrand(string originalSlug, BrandItem updatedBrand)
        {
            var existing = _context.Brands.FirstOrDefault(b => b.Slug.ToLower() == originalSlug.ToLower());
            if (existing == null) return false;

            var oldName = existing.Name;
            var newName = string.IsNullOrWhiteSpace(updatedBrand.Name) ? existing.Name : updatedBrand.Name.Trim();

            // Update products referencing this brand if brand name changed
            if (!string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            {
                var productsToUpdate = _context.Products.Where(p => p.Brand.ToLower() == oldName.ToLower()).ToList();
                foreach (var prod in productsToUpdate)
                {
                    prod.Brand = newName;
                }
            }

            existing.Name = newName;
            if (!string.IsNullOrWhiteSpace(updatedBrand.Slug))
            {
                existing.Slug = updatedBrand.Slug.Trim();
                existing.CategorySlug = existing.Slug;
            }
            existing.Tagline = updatedBrand.Tagline ?? existing.Tagline;
            existing.Description = updatedBrand.Description ?? existing.Description;
            existing.Origin = updatedBrand.Origin ?? existing.Origin;
            existing.Category = updatedBrand.Category ?? existing.Category;
            
            if (!string.IsNullOrWhiteSpace(updatedBrand.LogoInitials))
            {
                existing.LogoInitials = updatedBrand.LogoInitials;
            }
            if (!string.IsNullOrWhiteSpace(updatedBrand.ImageUrl))
            {
                existing.ImageUrl = updatedBrand.ImageUrl;
            }
            if (!string.IsNullOrWhiteSpace(updatedBrand.LogoUrl))
            {
                existing.LogoUrl = updatedBrand.LogoUrl;
            }
            if (!string.IsNullOrWhiteSpace(updatedBrand.SignatureProduct))
            {
                existing.SignatureProduct = updatedBrand.SignatureProduct;
            }
            if (!string.IsNullOrWhiteSpace(updatedBrand.BenefitDescription))
            {
                existing.BenefitDescription = updatedBrand.BenefitDescription;
            }
            if (updatedBrand.KeyFeatures != null && updatedBrand.KeyFeatures.Any())
            {
                existing.KeyFeatures = updatedBrand.KeyFeatures;
            }
            if (!string.IsNullOrWhiteSpace(updatedBrand.BadgeText))
            {
                existing.BadgeText = updatedBrand.BadgeText;
            }
            existing.IsFeatured = updatedBrand.IsFeatured;
            if (updatedBrand.Rating > 0)
            {
                existing.Rating = updatedBrand.Rating;
            }

            _context.SaveChanges();
            return true;
        }

        public bool DeleteBrand(string slug)
        {
            var brand = _context.Brands.FirstOrDefault(b => b.Slug.ToLower() == slug.ToLower());
            if (brand == null) return false;
            _context.Brands.Remove(brand);
            _context.SaveChanges();
            return true;
        }

        #endregion

        #region Products & Categories Management

        public List<Product> GetAllProducts()
        {
            return _context.Products.AsNoTracking().OrderByDescending(p => p.Id).ToList();
        }

        public Product? GetProductById(int id)
        {
            return _context.Products.FirstOrDefault(p => p.Id == id);
        }

        public List<Product> GetFeaturedProducts()
        {
            return _context.Products.AsNoTracking().Take(4).ToList();
        }

        public List<Product> GetRelatedProducts(int productId, int count = 4)
        {
            var product = _context.Products.AsNoTracking().FirstOrDefault(p => p.Id == productId);
            if (product == null) return _context.Products.AsNoTracking().Take(count).ToList();

            var categorySlug = product.CategorySlug;
            var brand = product.Brand;

            // 1. Get products from same category or brand
            var related = _context.Products.AsNoTracking()
                .Where(p => p.Id != productId && (p.CategorySlug == categorySlug || p.Brand == brand))
                .Take(count)
                .ToList();

            // 2. If fewer than 'count', backfill with other products
            if (related.Count < count)
            {
                var existingIds = related.Select(r => r.Id).Append(productId).ToList();
                var remainingCount = count - related.Count;
                var backfill = _context.Products.AsNoTracking()
                    .Where(p => !existingIds.Contains(p.Id))
                    .Take(remainingCount)
                    .ToList();

                related.AddRange(backfill);
            }

            return related;
        }

        public List<Category> GetCategories()
        {
            return _context.Categories.AsNoTracking().ToList();
        }

        public Category? GetCategoryBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            return _context.Categories.FirstOrDefault(c => c.Slug.ToLower() == slug.Trim().ToLower());
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var lower = text.ToLowerInvariant();
            lower = Regex.Replace(lower, @"[àáâãäå]", "a");
            lower = Regex.Replace(lower, @"[èéêë]", "e");
            lower = Regex.Replace(lower, @"[ìíîï]", "i");
            lower = Regex.Replace(lower, @"[òóôõö]", "o");
            lower = Regex.Replace(lower, @"[ùúûü]", "u");
            lower = Regex.Replace(lower, @"[ç]", "c");
            lower = Regex.Replace(lower, @"[^a-z0-9]+", "-");
            return lower.Trim('-');
        }

        public List<Product> FilterProducts(string? category, string? searchQuery, string? sortOrder, string? brand = null, string? subCategory = null)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category.ToLower() != "all" && category.ToLower() != "tous")
            {
                var catSlug = Slugify(category);
                var catText = category.Trim().ToLower();
                query = query.Where(p => p.CategorySlug.ToLower() == catSlug || p.CategorySlug.ToLower() == catText || p.CategoryName.ToLower().Contains(catText));
            }

            if (!string.IsNullOrWhiteSpace(subCategory) && subCategory.ToLower() != "all" && subCategory.ToLower() != "tous")
            {
                var sub = subCategory.Trim().ToLower();
                var subSlug = Slugify(subCategory);
                query = query.Where(p => (p.SubCategory != null && (p.SubCategory.ToLower().Contains(sub) || p.SubCategory.ToLower().Contains(subSlug))) ||
                                         (p.SubCategorySlug != null && (p.SubCategorySlug.ToLower().Contains(subSlug) || p.SubCategorySlug.ToLower().Contains(sub))));
            }

            if (!string.IsNullOrWhiteSpace(brand) && brand.ToLower() != "all" && brand.ToLower() != "tous")
            {
                var b = brand.Trim().ToLower();
                query = query.Where(p => p.Brand.ToLower().Contains(b));
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim().ToLower();
                var cleanNum = term.TrimStart('#').Trim();
                int.TryParse(cleanNum, out int parsedId);

                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         p.Brand.ToLower().Contains(term) ||
                                         p.CategoryName.ToLower().Contains(term) ||
                                         (p.SubCategory != null && p.SubCategory.ToLower().Contains(term)) ||
                                         p.ShortDescription.ToLower().Contains(term) ||
                                         (parsedId > 0 && p.Id == parsedId));
            }

            query = sortOrder?.ToLower() switch
            {
                "price-low" => query.OrderBy(p => p.Price),
                "price-high" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Rating),
                _ => query.OrderByDescending(p => p.Id)
            };

            return query.ToList();
        }

        public void AddProduct(Product product)
        {
            var cat = _context.Categories.FirstOrDefault(c => c.Slug.ToLower() == product.CategorySlug.ToLower());
            if (cat != null)
            {
                product.CategoryName = cat.Name;
                cat.ItemCount++;
            }

            if (!string.IsNullOrWhiteSpace(product.Brand))
            {
                EnsureBrandExists(product.Brand);
            }

            _context.Products.Add(product);
            _context.SaveChanges();
        }

        public bool UpdateProduct(Product product)
        {
            var existing = _context.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existing == null) return false;

            if (!string.IsNullOrWhiteSpace(product.Brand))
            {
                EnsureBrandExists(product.Brand);
            }

            // Adjust category item count if category changed
            if (!string.Equals(existing.CategorySlug, product.CategorySlug, StringComparison.OrdinalIgnoreCase))
            {
                var oldCat = _context.Categories.FirstOrDefault(c => c.Slug.ToLower() == existing.CategorySlug.ToLower());
                if (oldCat != null && oldCat.ItemCount > 0) oldCat.ItemCount--;

                var newCat = _context.Categories.FirstOrDefault(c => c.Slug.ToLower() == product.CategorySlug.ToLower());
                if (newCat != null)
                {
                    product.CategoryName = newCat.Name;
                    newCat.ItemCount++;
                }
            }
            else
            {
                product.CategoryName = existing.CategoryName;
            }

            existing.Name = product.Name;
            existing.Brand = product.Brand;
            existing.ShortDescription = product.ShortDescription;
            existing.FullDescription = product.FullDescription;
            existing.Price = product.Price;
            existing.OriginalPrice = product.OriginalPrice;
            existing.Rating = product.Rating;
            existing.ReviewCount = product.ReviewCount;
            existing.ImageUrl = product.ImageUrl;
            existing.CategorySlug = product.CategorySlug;
            existing.CategoryName = product.CategoryName;
            existing.StockQuantity = product.StockQuantity;
            existing.RxRequired = product.RxRequired;
            existing.BadgeText = product.BadgeText;
            existing.Ingredients = product.Ingredients;
            existing.DosageInstructions = product.DosageInstructions;

            _context.SaveChanges();
            return true;
        }

        public bool DeleteProduct(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return false;

            var cat = _context.Categories.FirstOrDefault(c => c.Slug.ToLower() == product.CategorySlug.ToLower());
            if (cat != null && cat.ItemCount > 0)
            {
                cat.ItemCount--;
            }

            _context.Products.Remove(product);
            _context.SaveChanges();
            return true;
        }

        public void AddCategory(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                category.Slug = category.Name.ToLower().Replace(" ", "-").Replace("&", "et");
            }
            if (string.IsNullOrWhiteSpace(category.Icon))
            {
                category.Icon = "category";
            }
            _context.Categories.Add(category);
            _context.SaveChanges();
        }

        public bool DeleteCategory(int id)
        {
            var cat = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (cat == null) return false;
            _context.Categories.Remove(cat);
            _context.SaveChanges();
            return true;
        }

        #endregion

        #region Dynamic Stock Management & Audit Log

        public bool DeductStock(int productId, int quantity, string reason, string referenceType = "Order")
        {
            if (quantity <= 0) return true;

            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return false;

            int prevStock = product.StockQuantity;
            int newStock = Math.Max(0, prevStock - quantity);
            product.StockQuantity = newStock;

            _context.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImageUrl = product.ImageUrl,
                QuantityChange = -quantity,
                PreviousStock = prevStock,
                NewStock = newStock,
                Reason = reason,
                ReferenceType = referenceType,
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();
            return true;
        }

        public bool RestockProduct(int productId, int quantityToAdd, string reason = "Réapprovisionnement Manuel")
        {
            if (quantityToAdd <= 0) return false;

            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return false;

            int prevStock = product.StockQuantity;
            int newStock = prevStock + quantityToAdd;
            product.StockQuantity = newStock;

            _context.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImageUrl = product.ImageUrl,
                QuantityChange = quantityToAdd,
                PreviousStock = prevStock,
                NewStock = newStock,
                Reason = reason,
                ReferenceType = "Restock",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();
            return true;
        }

        public bool AdjustStock(int productId, int newQuantity, string reason = "Ajustement Inventaire")
        {
            if (newQuantity < 0) newQuantity = 0;

            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return false;

            int prevStock = product.StockQuantity;
            int change = newQuantity - prevStock;
            product.StockQuantity = newQuantity;

            _context.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImageUrl = product.ImageUrl,
                QuantityChange = change,
                PreviousStock = prevStock,
                NewStock = newQuantity,
                Reason = reason,
                ReferenceType = change >= 0 ? "Restock" : "Adjustment",
                Timestamp = DateTime.Now
            });

            _context.SaveChanges();
            return true;
        }

        public List<StockMovement> GetStockMovements(int? productId = null, int limit = 50)
        {
            var query = _context.StockMovements.AsNoTracking().AsQueryable();
            if (productId.HasValue)
            {
                query = query.Where(m => m.ProductId == productId.Value);
            }
            return query.OrderByDescending(m => m.Timestamp).Take(limit).ToList();
        }

        #endregion

        #region Order Management & Stock Restoration

        public List<Order> GetAllOrders()
        {
            return _context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToList();
        }

        public Order? GetOrderByNumber(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber)) return null;
            var trimmed = orderNumber.Trim().ToLower();
            return _context.Orders
                .Include(o => o.Items)
                .FirstOrDefault(o => o.OrderNumber.ToLower() == trimmed);
        }

        public List<Order> GetOrdersByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return new List<Order>();
            var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(cleanPhone)) return new List<Order>();

            return _context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .Where(o => o.Phone.Contains(cleanPhone) || o.Phone.Contains(phone.Trim()))
                .OrderByDescending(o => o.OrderDate)
                .ToList();
        }

        public Order CreateOrder(Cart cart, string fullName, string phone, string? email, string city, string postalCode, string address, string? notes)
        {
            var orderNum = $"CMD-{new Random().Next(10000, 99999)}";

            var order = new Order
            {
                OrderNumber = orderNum,
                CustomerName = string.IsNullOrWhiteSpace(fullName) ? "Client Natalya" : fullName.Trim(),
                Phone = phone?.Trim() ?? string.Empty,
                Email = email?.Trim(),
                City = string.IsNullOrWhiteSpace(city) ? "Sousse" : city.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(postalCode) ? "4000" : postalCode.Trim(),
                Address = string.IsNullOrWhiteSpace(address) ? "Adresse de livraison" : address.Trim(),
                DeliveryNotes = notes?.Trim(),
                OrderDate = DateTime.Now,
                Subtotal = cart.Subtotal,
                ShippingFee = cart.ShippingFee,
                TotalAmount = cart.GrandTotal,
                Status = "En attente",
                PaymentMethod = "Paiement à la livraison (Espèces)",
                StockDeducted = true,
                Items = cart.Items.Select(item => new OrderItem
                {
                    OrderNumber = orderNum,
                    ProductId = item.Product.Id,
                    ProductName = item.Product.Name,
                    Brand = item.Product.Brand,
                    ImageUrl = item.Product.ImageUrl,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity
                }).ToList()
            };

            // Deduct stock for each product in the order
            foreach (var item in cart.Items)
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == item.Product.Id);
                if (product != null)
                {
                    int prevStock = product.StockQuantity;
                    int newStock = Math.Max(0, prevStock - item.Quantity);
                    product.StockQuantity = newStock;

                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        ProductImageUrl = product.ImageUrl,
                        QuantityChange = -item.Quantity,
                        PreviousStock = prevStock,
                        NewStock = newStock,
                        Reason = $"Commande client #{orderNum}",
                        ReferenceType = "Order",
                        Timestamp = DateTime.Now
                    });
                }
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            return order;
        }

        public bool UpdateOrderStatus(string orderNumber, string newStatus)
        {
            var order = _context.Orders.Include(o => o.Items).FirstOrDefault(o => o.OrderNumber.ToLower() == orderNumber.Trim().ToLower());
            if (order == null) return false;

            // If changing to "Annulée" and stock was deducted, restore stock
            if (newStatus.Equals("Annulée", StringComparison.OrdinalIgnoreCase) && order.StockDeducted && !order.Status.Equals("Annulée", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in order.Items)
                {
                    var product = _context.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        int prevStock = product.StockQuantity;
                        int newStock = prevStock + item.Quantity;
                        product.StockQuantity = newStock;

                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ProductImageUrl = product.ImageUrl,
                            QuantityChange = item.Quantity,
                            PreviousStock = prevStock,
                            NewStock = newStock,
                            Reason = $"Restitution suite annulation commande #{order.OrderNumber}",
                            ReferenceType = "Cancellation",
                            Timestamp = DateTime.Now
                        });
                    }
                }
                order.StockDeducted = false;
            }

            order.Status = newStatus;
            _context.SaveChanges();

            return true;
        }

        public bool CancelOrder(string orderNumber)
        {
            return UpdateOrderStatus(orderNumber, "Annulée");
        }

        #endregion

        #region Special Offers Management

        public List<SpecialOffer> GetActiveSpecialOffers()
        {
            return _context.SpecialOffers
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.Id)
                .ToList();
        }

        public List<SpecialOffer> GetAllSpecialOffers()
        {
            return _context.SpecialOffers
                .AsNoTracking()
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.Id)
                .ToList();
        }

        public SpecialOffer? GetSpecialOfferById(int id)
        {
            return _context.SpecialOffers.FirstOrDefault(s => s.Id == id);
        }

        public void AddSpecialOffer(SpecialOffer offer)
        {
            if (offer.DisplayOrder <= 0)
            {
                var maxOrder = _context.SpecialOffers.Any() ? _context.SpecialOffers.Max(s => s.DisplayOrder) : 0;
                offer.DisplayOrder = maxOrder + 1;
            }
            _context.SpecialOffers.Add(offer);
            _context.SaveChanges();
        }

        public bool UpdateSpecialOffer(SpecialOffer offer)
        {
            var existing = _context.SpecialOffers.FirstOrDefault(s => s.Id == offer.Id);
            if (existing == null) return false;

            existing.Badge = offer.Badge;
            existing.Title = offer.Title;
            existing.Description = offer.Description;
            existing.OriginalPrice = offer.OriginalPrice;
            existing.PromoPrice = offer.PromoPrice;
            if (!string.IsNullOrWhiteSpace(offer.ImageUrl))
            {
                existing.ImageUrl = offer.ImageUrl;
            }
            existing.ThemeStyle = offer.ThemeStyle;
            existing.AssociatedProductId = offer.AssociatedProductId;
            existing.DisplayOrder = offer.DisplayOrder;
            existing.IsActive = offer.IsActive;

            _context.SaveChanges();
            return true;
        }

        public bool DeleteSpecialOffer(int id)
        {
            var existing = _context.SpecialOffers.FirstOrDefault(s => s.Id == id);
            if (existing == null) return false;

            _context.SpecialOffers.Remove(existing);
            _context.SaveChanges();
            return true;
        }

        public bool ToggleSpecialOffer(int id)
        {
            var existing = _context.SpecialOffers.FirstOrDefault(s => s.Id == id);
            if (existing == null) return false;

            existing.IsActive = !existing.IsActive;
            _context.SaveChanges();
            return true;
        }

        #endregion

        #region Story Reels Management

        public List<StoryReel> GetActiveStoryReels()
        {
            return _context.StoryReels
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.DisplayOrder)
                .ThenByDescending(r => r.Id)
                .ToList();
        }

        public List<StoryReel> GetAllStoryReels()
        {
            return _context.StoryReels
                .AsNoTracking()
                .OrderBy(r => r.DisplayOrder)
                .ThenByDescending(r => r.Id)
                .ToList();
        }

        public StoryReel? GetStoryReelById(int id)
        {
            return _context.StoryReels.FirstOrDefault(r => r.Id == id);
        }

        public void AddStoryReel(StoryReel story)
        {
            if (story.DisplayOrder <= 0)
            {
                var maxOrder = _context.StoryReels.Any() ? _context.StoryReels.Max(r => r.DisplayOrder) : 0;
                story.DisplayOrder = maxOrder + 1;
            }
            _context.StoryReels.Add(story);
            _context.SaveChanges();
        }

        public bool UpdateStoryReel(StoryReel story)
        {
            var existing = _context.StoryReels.FirstOrDefault(r => r.Id == story.Id);
            if (existing == null) return false;

            existing.BrandName = story.BrandName;
            existing.BadgeText = story.BadgeText;
            existing.BadgeColor = story.BadgeColor;
            existing.Title = story.Title;
            existing.Description = story.Description;
            if (!string.IsNullOrWhiteSpace(story.VideoUrl))
            {
                existing.VideoUrl = story.VideoUrl;
            }
            if (!string.IsNullOrWhiteSpace(story.PosterUrl))
            {
                existing.PosterUrl = story.PosterUrl;
            }
            existing.AssociatedProductId = story.AssociatedProductId;
            existing.DisplayOrder = story.DisplayOrder;
            existing.IsActive = story.IsActive;

            _context.SaveChanges();
            return true;
        }

        public bool DeleteStoryReel(int id)
        {
            var existing = _context.StoryReels.FirstOrDefault(r => r.Id == id);
            if (existing == null) return false;

            _context.StoryReels.Remove(existing);
            _context.SaveChanges();
            return true;
        }

        public bool ToggleStoryReel(int id)
        {
            var existing = _context.StoryReels.FirstOrDefault(r => r.Id == id);
            if (existing == null) return false;

            existing.IsActive = !existing.IsActive;
            _context.SaveChanges();
            return true;
        }

        #endregion
    }
}
