namespace PharmaTrust.Models
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> DiscountedProducts { get; set; } = new();
        public List<SpecialOffer> PromotionalOffers { get; set; } = new();
        public List<StoryReel> StoryReels { get; set; } = new();
    }
}
