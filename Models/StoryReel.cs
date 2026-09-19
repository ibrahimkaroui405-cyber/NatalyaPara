namespace PharmaTrust.Models
{
    public class StoryReel
    {
        public int Id { get; set; }
        public string BrandName { get; set; } = "Eucerin";
        public string BadgeText { get; set; } = "Story Dermo";
        public string BadgeColor { get; set; } = "red"; // red, emerald, teal, indigo, amber, rose
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = "/videos/v1.mp4";
        public string PosterUrl { get; set; } = string.Empty;
        public int? AssociatedProductId { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
