using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PharmaTrust.Models;
using System.Text.Json;

namespace PharmaTrust.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<BrandItem> Brands { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<SpecialOffer> SpecialOffers { get; set; }
        public DbSet<StoryReel> StoryReels { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Value converter for List<string> to JSON string
            var stringListConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
            );

            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Price).HasPrecision(18, 2);
                entity.Property(p => p.OriginalPrice).HasPrecision(18, 2);
                
                entity.Property(p => p.Ingredients)
                    .HasConversion(stringListConverter)
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(p => p.DosageInstructions)
                    .HasConversion(stringListConverter)
                    .Metadata.SetValueComparer(stringListComparer);
            });

            // Configure Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.Slug).IsUnique();

                entity.Property(c => c.SubCategories)
                    .HasConversion(stringListConverter)
                    .Metadata.SetValueComparer(stringListComparer);
            });

            // Configure BrandItem
            modelBuilder.Entity<BrandItem>(entity =>
            {
                entity.HasKey(b => b.Slug);
                entity.Property(b => b.Price).HasPrecision(18, 2);
                entity.Property(b => b.OriginalPrice).HasPrecision(18, 2);

                entity.Property(b => b.KeyFeatures)
                    .HasConversion(stringListConverter)
                    .Metadata.SetValueComparer(stringListComparer);
            });

            // Configure Order & OrderItem
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.OrderNumber);
                entity.Property(o => o.Subtotal).HasPrecision(18, 2);
                entity.Property(o => o.ShippingFee).HasPrecision(18, 2);
                entity.Property(o => o.TotalAmount).HasPrecision(18, 2);

                entity.HasMany(o => o.Items)
                    .WithOne()
                    .HasForeignKey(i => i.OrderNumber)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
            });

            // Configure StockMovement
            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.HasKey(m => m.Id);
            });

            // Configure SpecialOffer
            modelBuilder.Entity<SpecialOffer>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.OriginalPrice).HasPrecision(18, 2);
                entity.Property(s => s.PromoPrice).HasPrecision(18, 2);
            });

            // Configure StoryReel
            modelBuilder.Entity<StoryReel>(entity =>
            {
                entity.HasKey(r => r.Id);
            });

            // Configure AdminUser
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });
        }
    }
}
