namespace PharmaTrust.Models
{
    public class ProductCatalogViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public string? SelectedSubCategory { get; set; }
        public string? SelectedBrand { get; set; }
        public string? SearchQuery { get; set; }
        public string SortOrder { get; set; } = "featured";
        public int TotalProducts { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalProducts / (PageSize > 0 ? PageSize : 12)));
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
