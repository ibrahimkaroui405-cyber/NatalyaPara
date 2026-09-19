namespace PharmaTrust.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Icon { get; set; } = "medication";
        public int ItemCount { get; set; }
        public string? ParentSlug { get; set; }
        public List<string> SubCategories { get; set; } = new();
    }
}
