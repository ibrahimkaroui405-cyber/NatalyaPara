namespace PharmaTrust.Models
{
    public class SpecialOffer
    {
        public int Id { get; set; }
        public string Badge { get; set; } = "-25% • Édition Limitée";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal PromoPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string ThemeStyle { get; set; } = "dark-rose"; // dark-rose, amber-gold, emerald-mint, dark-luxury
        public int? AssociatedProductId { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
