using PharmaTrust.Models;

namespace PharmaTrust.Services
{
    public interface IProductService
    {
        List<Product> GetAllProducts();
        Product? GetProductById(int id);
        List<Product> GetFeaturedProducts();
        List<Product> GetRelatedProducts(int productId, int count = 4);
        List<Category> GetCategories();
        Category? GetCategoryBySlug(string slug);
        List<BrandItem> GetBrands();
        BrandItem? GetBrandBySlug(string slug);
        BrandItem? GetBrandByName(string name);
        List<string> GetAllBrandNames();
        void EnsureBrandExists(string brandName);
        List<Product> FilterProducts(string? category, string? searchQuery, string? sortOrder, string? brand = null, string? subCategory = null);
        
        // Stock Management Operations
        bool DeductStock(int productId, int quantity, string reason, string referenceType = "Order");
        bool RestockProduct(int productId, int quantityToAdd, string reason = "Réapprovisionnement Manuel");
        bool AdjustStock(int productId, int newQuantity, string reason = "Ajustement Inventaire");
        List<StockMovement> GetStockMovements(int? productId = null, int limit = 50);
        
        // Order Management Operations
        List<Order> GetAllOrders();
        Order? GetOrderByNumber(string orderNumber);
        List<Order> GetOrdersByPhone(string phone);
        Order CreateOrder(Cart cart, string fullName, string phone, string? email, string city, string postalCode, string address, string? notes);
        bool UpdateOrderStatus(string orderNumber, string newStatus);
        bool CancelOrder(string orderNumber);

        // CRUD operations for Admin
        void AddProduct(Product product);
        bool UpdateProduct(Product product);
        bool DeleteProduct(int id);
        void AddCategory(Category category);
        bool DeleteCategory(int id);
        void AddBrand(BrandItem brand);
        bool UpdateBrand(string originalSlug, BrandItem updatedBrand);
        bool DeleteBrand(string slug);

        // Special Offers Operations
        List<SpecialOffer> GetActiveSpecialOffers();
        List<SpecialOffer> GetAllSpecialOffers();
        SpecialOffer? GetSpecialOfferById(int id);
        void AddSpecialOffer(SpecialOffer offer);
        bool UpdateSpecialOffer(SpecialOffer offer);
        bool DeleteSpecialOffer(int id);
        bool ToggleSpecialOffer(int id);

        // Story Reels Operations
        List<StoryReel> GetActiveStoryReels();
        List<StoryReel> GetAllStoryReels();
        StoryReel? GetStoryReelById(int id);
        void AddStoryReel(StoryReel story);
        bool UpdateStoryReel(StoryReel story);
        bool DeleteStoryReel(int id);
        bool ToggleStoryReel(int id);
    }
}
