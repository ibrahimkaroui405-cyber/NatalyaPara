namespace PharmaTrust.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public double Rating { get; set; } = 4.5;
        public int ReviewCount { get; set; } = 95;
        public string ImageUrl { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? SubCategory { get; set; }
        public string? SubCategorySlug { get; set; }
        
        // Stock management
        public int StockQuantity { get; set; } = 15;
        public bool InStock
        {
            get => StockQuantity > 0;
            set
            {
                if (!value && StockQuantity > 0) StockQuantity = 0;
                else if (value && StockQuantity <= 0) StockQuantity = 10;
            }
        }

        public bool RxRequired { get; set; } = false;
        public string? BadgeText { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public List<string> DosageInstructions { get; set; } = new();
    }
}
