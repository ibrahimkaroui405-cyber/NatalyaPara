namespace PharmaTrust.Models
{
    public class StockManagementViewModel
    {
        public int TotalProducts { get; set; }
        public int InStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int TotalUnitsInStock { get; set; }
        public decimal TotalInventoryValue { get; set; }

        public List<Product> Products { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
        public List<Product> OutOfStockProducts { get; set; } = new();
        public List<StockMovement> RecentMovements { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        public string? FilterCategory { get; set; }
        public string? FilterStatus { get; set; } // "all", "low", "out", "instock"
        public string? SearchQuery { get; set; }

        public int TotalFilteredCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalFilteredCount / (PageSize > 0 ? PageSize : 15)));
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
