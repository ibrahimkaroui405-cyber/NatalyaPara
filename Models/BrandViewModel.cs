using System.ComponentModel.DataAnnotations;

namespace PharmaTrust.Models
{
    public class BrandItem
    {
        [Key]
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Origin { get; set; } = "France";
        public string Category { get; set; } = "Dermo-Cosmétique";
        public string CategorySlug { get; set; } = "dermo";
        public string LogoInitials { get; set; } = string.Empty;
        public string Icon { get; set; } = "verified";
        public int ProductCount { get; set; } = 0;
        public string SignatureProduct { get; set; } = string.Empty;
        public List<string> KeyFeatures { get; set; } = new();
        public bool IsFeatured { get; set; } = false;
        public double Rating { get; set; } = 4.9;
        public string BadgeText { get; set; } = "Officine Agréée";
        public string ImageUrl { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string TutorialTag { get; set; } = "TUTO SOIN";
        public string VideoDuration { get; set; } = "0:45 min";
        public string RitualCategory { get; set; } = "RITUEL CAPILLAIRE";
        public int ReviewCount { get; set; } = 124;
        public decimal Price { get; set; } = 16.90m;
        public decimal? OriginalPrice { get; set; } = 22.00m;
        public string BenefitDescription { get; set; } = string.Empty;
        public int? AssociatedProductId { get; set; }
    }

    public class BrandsPageViewModel
    {
        public List<BrandItem> Brands { get; set; } = new();
        public List<BrandItem> AllBrands { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public string? SelectedBrand { get; set; }
        public string? SelectedLetter { get; set; }
        public string? SearchQuery { get; set; }
        public string SortOrder { get; set; } = "featured";
        public List<Product> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int TotalBrands { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 24;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalBrands / (PageSize > 0 ? PageSize : 24)));
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
