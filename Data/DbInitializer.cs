using Microsoft.EntityFrameworkCore;
using PharmaTrust.Models;

namespace PharmaTrust.Data
{
    public static class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure database and schema are created
            context.Database.EnsureCreated();

            // Ensure newly added tables exist in existing SQLite database file
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""SpecialOffers"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SpecialOffers"" PRIMARY KEY AUTOINCREMENT,
                    ""Badge"" TEXT NOT NULL DEFAULT '',
                    ""Title"" TEXT NOT NULL DEFAULT '',
                    ""Description"" TEXT NOT NULL DEFAULT '',
                    ""OriginalPrice"" TEXT NOT NULL DEFAULT '0',
                    ""PromoPrice"" TEXT NOT NULL DEFAULT '0',
                    ""ImageUrl"" TEXT NOT NULL DEFAULT '',
                    ""ThemeStyle"" TEXT NOT NULL DEFAULT 'dark-luxury',
                    ""AssociatedProductId"" INTEGER NULL,
                    ""DisplayOrder"" INTEGER NOT NULL DEFAULT 0,
                    ""IsActive"" INTEGER NOT NULL DEFAULT 1
                );
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""StoryReels"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_StoryReels"" PRIMARY KEY AUTOINCREMENT,
                    ""BrandName"" TEXT NOT NULL DEFAULT 'Eucerin',
                    ""BadgeText"" TEXT NOT NULL DEFAULT 'Story Dermo',
                    ""BadgeColor"" TEXT NOT NULL DEFAULT 'red',
                    ""Title"" TEXT NOT NULL DEFAULT '',
                    ""Description"" TEXT NOT NULL DEFAULT '',
                    ""VideoUrl"" TEXT NOT NULL DEFAULT '',
                    ""PosterUrl"" TEXT NOT NULL DEFAULT '',
                    ""AssociatedProductId"" INTEGER NULL,
                    ""DurationSeconds"" INTEGER NOT NULL DEFAULT 15,
                    ""DisplayOrder"" INTEGER NOT NULL DEFAULT 0,
                    ""IsActive"" INTEGER NOT NULL DEFAULT 1
                );
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""AdminUsers"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_AdminUsers"" PRIMARY KEY AUTOINCREMENT,
                    ""Username"" TEXT NOT NULL,
                    ""Email"" TEXT NOT NULL,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""FullName"" TEXT NOT NULL DEFAULT '',
                    ""Role"" TEXT NOT NULL DEFAULT 'Administrator',
                    ""CreatedAt"" TEXT NOT NULL,
                    ""LastLoginAt"" TEXT NULL,
                    ""IsActive"" INTEGER NOT NULL DEFAULT 1
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AdminUsers_Username"" ON ""AdminUsers"" (""Username"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AdminUsers_Email"" ON ""AdminUsers"" (""Email"");
            ");

            // Ensure new columns exist in SQLite database
            try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN ""SubCategory"" TEXT NULL;"); } catch { }
            try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN ""SubCategorySlug"" TEXT NULL;"); } catch { }
            try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Categories"" ADD COLUMN ""ParentSlug"" TEXT NULL;"); } catch { }
            try { context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Categories"" ADD COLUMN ""SubCategories"" TEXT NOT NULL DEFAULT '[]';"); } catch { }

            // Initialize / Sync Categories with Subcategories matching Para Fendri
            var defaultCategories = new List<Category>
            {
                new Category 
                { 
                    Name = "Soins du Visage", 
                    Slug = "visage", 
                    Icon = "face_retouching_natural", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Nettoyants & Démaquillants", "Anti-Âge & Rides", "Anti-Imperfections & Acné", "Anti-Taches & Éclat", "Contour des Yeux", "Hydratation & Nutrition", "Peaux Sensibles & Rougeurs", "Peeling & Masques", "Soins des Lèvres" }
                },
                new Category 
                { 
                    Name = "Soins Capillaires", 
                    Slug = "cheveux", 
                    Icon = "brush", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Shampoings", "Après-Shampoings", "Masques & Sérums", "Anti-Chute & Repousse", "Cheveux Secs & Abîmés", "Anti-Pelliculaire" }
                },
                new Category 
                { 
                    Name = "Soins du Corps", 
                    Slug = "corps", 
                    Icon = "spa", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Hydratation & Nutrition", "Hygiène Corporelle", "Gommages & Exfoliants", "Soins Mains & Pieds", "Anti-Grattage & Sécheresse", "Jambes Légères", "Soins Minceur & Fermeté", "Parfums & Brumes" }
                },
                new Category 
                { 
                    Name = "Protection Solaire", 
                    Slug = "solaire", 
                    Icon = "sunny", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Crèmes Solaires Visage", "Fluides Solaires Teintés", "Packs Solaires", "Après-Solaire & Réparateur", "Solaires Bébé & Enfant", "Huiles de Bronzage" }
                },
                new Category 
                { 
                    Name = "Maman & Bébé", 
                    Slug = "maman-bebe", 
                    Icon = "child_care", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Soins & Toilette Bébé", "Change & Érythème", "Laits & Alimentation", "Biberons & Accessoires", "Soins Grossesse & Maternité" }
                },
                new Category 
                { 
                    Name = "Hygiène & Beauté", 
                    Slug = "hygiene", 
                    Icon = "sanitizer", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Douche & Bain", "Hygiène Bucco-Dentaire", "Hygiène Intime", "Déodorants & Anti-Transpirants", "Anti-Moustiques & Parasites" }
                },
                new Category 
                { 
                    Name = "Vitamines & Compléments", 
                    Slug = "vitamines", 
                    Icon = "medication", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Vitamines & Énergie", "Immunité & Défenses", "Minceur & Détox", "Sommeil & Sérénité", "Articulations & Os", "Beauté Peau & Ongles" }
                },
                new Category 
                { 
                    Name = "Soins Homme", 
                    Slug = "homme", 
                    Icon = "face_6", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Rasage & Après-Rasage", "Soins Visage Homme", "Capillaire & Barbe", "Hygiène & Déodorants" }
                },
                new Category 
                { 
                    Name = "Matériel Médical & Orthopédie", 
                    Slug = "materiel-medical", 
                    Icon = "medical_services", 
                    ItemCount = 0,
                    SubCategories = new List<string> { "Tensiomètres & Diagnostic", "Thermomètres", "Pansements & Premiers Secours", "Orthopédie & Maintien", "Chaussures Médicales" }
                }
            };

            foreach (var defCat in defaultCategories)
            {
                var existing = context.Categories.FirstOrDefault(c => c.Slug == defCat.Slug || (defCat.Slug == "visage" && c.Slug == "soins-visage") || (defCat.Slug == "maman-bebe" && c.Slug == "soins-bebe"));
                if (existing != null)
                {
                    existing.Slug = defCat.Slug;
                    existing.Name = defCat.Name;
                    existing.Icon = defCat.Icon;
                    existing.SubCategories = defCat.SubCategories;
                    existing.ItemCount = context.Products.Count(p => p.CategorySlug == defCat.Slug);
                }
                else
                {
                    defCat.ItemCount = context.Products.Count(p => p.CategorySlug == defCat.Slug);
                    context.Categories.Add(defCat);
                }
            }
            context.SaveChanges();

            // Seed Brands
            if (!context.Brands.Any())
            {
                context.Brands.AddRange(
                    new BrandItem
                    {
                        Name = "Eucerin",
                        Slug = "eucerin",
                        Tagline = "La Science Dermatologique Cutanée & Innovation Thiamidol Brevetée",
                        Description = "Pionnier mondial de la dermo-cosmétique depuis plus d'un siècle. Formules haute tolérance développées avec des dermatologues pour réparer, protéger et corriger les taches d'hyperpigmentation.",
                        Origin = "Hambourg • Allemagne",
                        Category = "Dermo-Cosmétique",
                        CategorySlug = "eucerin",
                        LogoInitials = "EU",
                        Icon = "science",
                        ImageUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=800&q=80",
                        ProductCount = 18,
                        SignatureProduct = "Anti-Pigment Sérum Duo & Hyaluron-Filler",
                        BenefitDescription = "Réduit visiblement les taches pigmentaires et comble les rides profondes.",
                        KeyFeatures = new List<string> { "Thiamidol Breveté", "Haute Tolérance", "Recommandé Dermatologues" },
                        IsFeatured = true,
                        Rating = 4.9,
                        ReviewCount = 214,
                        Price = 39.90m,
                        OriginalPrice = 46.00m,
                        TutorialTag = "EXPERT PIGMENTATION",
                        VideoDuration = "1:10 min",
                        RitualCategory = "RITUEL ANTI-TACHES & HYALURONIQUE",
                        BadgeText = "Innovation Clinique",
                        AssociatedProductId = 1
                    },
                    new BrandItem
                    {
                        Name = "Pharmaceris",
                        Slug = "pharmaceris",
                        Tagline = "Solutions Dermatologiques Spécialisées & Programmes Cliniques Ciblés",
                        Description = "Marque dermatologique experte offrant des programmes ciblés et hypoallergéniques pour peaux atopiques (E), acnéiques (T), allergiques (A) et capillaires (H), rigoureusement testés sous contrôle clinique.",
                        Origin = "Pologne • Dr Irena Eris",
                        Category = "Dermatologie Ciblée",
                        CategorySlug = "pharmaceris",
                        LogoInitials = "PH",
                        Icon = "verified",
                        ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=800&q=80",
                        ProductCount = 14,
                        SignatureProduct = "Émulsion Régulatrice Sébo & Soin Peaux Atopiques",
                        BenefitDescription = "Restauration profonde de la barrière épidermique et soulagement des irritations.",
                        KeyFeatures = new List<string> { "Lignes Spécifiques A, E, T, H", "Haute Tolérance Cutanée", "Efficacité Clinique" },
                        IsFeatured = true,
                        Rating = 4.8,
                        ReviewCount = 156,
                        Price = 18.90m,
                        OriginalPrice = 24.00m,
                        TutorialTag = "PROGRAMME CLINIQUE",
                        VideoDuration = "0:55 min",
                        RitualCategory = "SOIN CIBLÉ PEAUX À PROBLÈMES",
                        BadgeText = "Haute Sécurité Dermo"
                    },
                    new BrandItem
                    {
                        Name = "Dermedic",
                        Slug = "dermedic",
                        Tagline = "Laboratoire Dermatologique & Efficacité Pure de l'Eau Thermale",
                        Description = "Soins dermatologiques formulés à base d'eau thermale hyperthermale artésienne pure. Idéal pour l'hydratation intense des peaux ultra-déshydratées (Hydrain3) et le traitement des rougeurs.",
                        Origin = "Pologne • Eau Artésienne",
                        Category = "Thermale & Hydratation",
                        CategorySlug = "dermedic",
                        LogoInitials = "DM",
                        Icon = "water_drop",
                        ImageUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?auto=format&fit=crop&w=800&q=80",
                        ProductCount = 12,
                        SignatureProduct = "Hydrain3 Hialuro Sérum Réhydratant 4D",
                        BenefitDescription = "Quadruple complexe hyaluronique pour un boost d'hydratation immédiat.",
                        KeyFeatures = new List<string> { "Eau Thermale Artésienne", "Hydratation 4D Hyaluronique", "Non Comédogène" },
                        IsFeatured = true,
                        Rating = 4.8,
                        ReviewCount = 138,
                        Price = 17.50m,
                        OriginalPrice = 22.00m,
                        TutorialTag = "BOOST HYDRATATION",
                        VideoDuration = "0:50 min",
                        RitualCategory = "RITUEL THERMAL HYDRAIN3",
                        BadgeText = "Eau Thermale Pure"
                    },
                    new BrandItem
                    {
                        Name = "Dermacare",
                        Slug = "dermacare",
                        Tagline = "Haute Dermo-Cosmétique Quotidienne & Protection Barrière Cutanée",
                        Description = "Formulations avancées associant actifs dermatologiques purs et textures sensorielles pour restaurer le film hydrolipidique et protéger la peau des agressions extérieures quotidiennes.",
                        Origin = "Paris • France",
                        Category = "Barrière & Soins",
                        CategorySlug = "dermacare",
                        LogoInitials = "DC",
                        Icon = "shield",
                        ImageUrl = "https://images.unsplash.com/photo-1608248597359-00977d247f52?auto=format&fit=crop&w=800&q=80",
                        ProductCount = 11,
                        SignatureProduct = "Dermacare Soin Réparateur & Écran Invisible SPF50+",
                        BenefitDescription = "Barrière protectrice renforcée et haute photoprotection à large spectre.",
                        KeyFeatures = new List<string> { "Bouclier Anti-Pollution", "Actifs Apaisants Purs", "Fini Invisible" },
                        IsFeatured = true,
                        Rating = 4.8,
                        ReviewCount = 112,
                        Price = 16.50m,
                        OriginalPrice = 21.00m,
                        TutorialTag = "RITUEL BARRIÈRE",
                        VideoDuration = "0:45 min",
                        RitualCategory = "PROTECTION QUOTIDIENNE ACTIVE",
                        BadgeText = "Soin Protecteur Avancé"
                    },
                    new BrandItem
                    {
                        Name = "Ducray",
                        Slug = "ducray",
                        Tagline = "Laboratoires Dermatologiques & Soins Ciblés Cuir Chevelu et Peau",
                        Description = "Depuis 1930, Ducray propose des réponses dermatologiques précises et éprouvées pour le cuir chevelu (Anaphase+, Kelual DS) et les peaux à tendance acnéique (Keracnyl).",
                        Origin = "Occitanie • France (Pierre Fabre)",
                        Category = "Capillaire & Peau",
                        CategorySlug = "ducray",
                        LogoInitials = "DU",
                        Icon = "content_cut",
                        ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?auto=format&fit=crop&w=800&q=80",
                        ProductCount = 15,
                        SignatureProduct = "Anaphase+ Shampoing Énergisant & Keracnyl Sérum",
                        BenefitDescription = "Fortification capillaire antichute et régulation durable des imperfections.",
                        KeyFeatures = new List<string> { "Expertise Cuir Chevelu", "Brevets Dermatologiques", "Efficacité Cliniquement Prouvée" },
                        IsFeatured = true,
                        Rating = 4.9,
                        ReviewCount = 186,
                        Price = 15.90m,
                        OriginalPrice = 19.90m,
                        TutorialTag = "CAPILLAIRE & DERMO",
                        VideoDuration = "1:00 min",
                        RitualCategory = "RITUEL EXPERT ANAPHASE+",
                        BadgeText = "Référence Capillaire"
                    },
                    new BrandItem
                    {
                        Name = "Avène",
                        Slug = "avene",
                        Tagline = "Eau Thermale Apaisante & Réparatrice pour Toutes Peaux Sensibles",
                        Description = "Puisée directement à sa source dans l'Hérault, l'Eau Thermale d'Avène offre une minéralité apaisante unique et l'actif biotechnologique C+Restore pour calmer et réparer les peaux réactives.",
                        Origin = "Hérault • France (Pierre Fabre)",
                        Category = "Eau Thermale & Tolérance",
                        CategorySlug = "avene",
                        LogoInitials = "AV",
                        Icon = "spa",
                        ImageUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?auto=format&fit=crop&w=800&q=80",
                        ProductCount = 19,
                        SignatureProduct = "Cicalfate+ Soin Réparateur & Cleanance Gel",
                        BenefitDescription = "Réparation épidermique rapide dès 48h et apaisement thermal immédiat.",
                        KeyFeatures = new List<string> { "Minéralité Faible 266mg/L", "Actif Post-biotique C+Restore", "Sans Parfum & Hypoallergénique" },
                        IsFeatured = true,
                        Rating = 4.9,
                        ReviewCount = 280,
                        Price = 12.90m,
                        OriginalPrice = 16.50m,
                        TutorialTag = "APAISANT NATUREL",
                        VideoDuration = "1:05 min",
                        RitualCategory = "RITUEL CICALFATE+ RÉPARATEUR",
                        BadgeText = "N°1 Prescription Thermale"
                    },
                    new BrandItem
                    {
                        Name = "SVR",
                        Slug = "svr",
                        Tagline = "Laboratoire Dermatologique Français & Dosages Records en Actifs Purs",
                        Description = "Laboratoire indépendant français réputé pour ses concentrations record en actifs purs (Niacinamide, Gluconolactone, Oméga 3-6-9) garantissant des résultats ultra-rapides et une tolérance maximale.",
                        Origin = "Paris • France",
                        Category = "Actifs Purs & Efficacité",
                        CategorySlug = "svr",
                        LogoInitials = "SVR",
                        Icon = "biotech",
                        ImageUrl = "https://images.unsplash.com/photo-1518531933037-91b2f5f229cc?auto=format&fit=crop&w=800&q=80",
                        ProductCount = 16,
                        SignatureProduct = "Sebiaclear Sérum Anti-Marques & Topialyse Huile",
                        BenefitDescription = "Correction puissante des pores, boutons, marques résiduelles et sécheresses intenses.",
                        KeyFeatures = new List<string> { "Dosages Actifs Records", "Tolérance 100% Peaux Sensibles", "Formulation Indépendante" },
                        IsFeatured = true,
                        Rating = 4.9,
                        ReviewCount = 195,
                        Price = 21.90m,
                        OriginalPrice = 27.00m,
                        TutorialTag = "HAUTE CONCENTRATION",
                        VideoDuration = "1:15 min",
                        RitualCategory = "RITUEL SEBIACLEAR PURIFIANT",
                        BadgeText = "Dosages Records Actifs"
                    }
                );
                context.SaveChanges();
            }

            // Seed Products
            if (!context.Products.Any())
            {
                context.Products.AddRange(
                    new Product
                    {
                        Id = 1,
                        Name = "Acide Ascorbique (Vitamine C) 1000mg",
                        Brand = "Eucerin",
                        ShortDescription = "Complément alimentaire haute puissance pour le soutien immunitaire. Formulation de qualité clinique conçue pour une absorption maximale.",
                        FullDescription = "Notre Vitamine C 1000mg de qualité clinique est formulée pour offrir un soutien puissant au système immunitaire et une protection antioxydante. Fabriquée selon les normes certifiées, elle garantit une haute pureté et une biodisponibilité optimale.",
                        Price = 24.99m,
                        OriginalPrice = 32.00m,
                        Rating = 4.8,
                        ReviewCount = 128,
                        ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCZ2e8wA5GHLGeqd2-GRkEpnXg3tA-cwf4FKgZqpSmcT3wEIXTLV2lZ2L7GWzkAHpOAd_yxJChfy12O_f3J-tdCkJ1hMrp-hb0viwy-3cPCEc1nH2NNJ9BnpB88WFxl-OFICe7ge5L7kVIP3WTueJK9ZKOyWK8IrRHiDFof39zraR-hgfvzehfm5NhC0bPCQNJFsbiitlvUbTG_8ARaC45Aead4CTSD9sZyvUqIq-ekpxxO5jM9_tRGAw",
                        CategorySlug = "vitamines",
                        CategoryName = "Vitamines & Compléments",
                        StockQuantity = 24,
                        RxRequired = true,
                        BadgeText = "Sur Ordonnance",
                        Ingredients = new List<string>
                        {
                            "Vitamine C (Acide Ascorbique) 1000mg",
                            "Complexe de Bioflavonoïdes d'Agrumes 100mg",
                            "Poudre de Cynorrhodon 25mg",
                            "Gélule Végétale en Cellulose",
                            "Stéarate de Magnésium Végétal"
                        },
                        DosageInstructions = new List<string>
                        {
                            "Prendre 1 gélule par jour au cours d'un repas avec un grand verre d'eau.",
                            "Boire suffisamment d'eau tout au long de la journée.",
                            "Ne pas dépasser la dose journalière recommandée."
                        }
                    },
                    new Product
                    {
                        Id = 2,
                        Name = "Complexe Multi-Vitamines Quotidien, 60 Gélules",
                        Brand = "Pharmaceris",
                        ShortDescription = "Multivitamines quotidiennes complètes avec micronutriments essentiels pour la vitalité et l'énergie.",
                        FullDescription = "Contient 24 vitamines et minéraux essentiels pour soutenir le métabolisme énergétique, la solidité osseuse et l'immunité au quotidien.",
                        Price = 24.99m,
                        OriginalPrice = 29.99m,
                        Rating = 4.7,
                        ReviewCount = 84,
                        ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCGI_86lUcfmh9Ld-JO6Q6mBiez9mM2Yf7x9xqdOyfdnZgtWPXALbUHnnjV-IdEPI-ITEiYA2aGJGQ3N1OnRPFSK2MRAgggG4e0CNuTHEItMwt4H8ccpK4kKmNnmhfNnMdb53cgLTMrWOskdu7epNglIeRx3QgmzFZ8wxYK7FaJ4SalSLLGRhCFy0q5xx4ENsZes3EnKt1ms2tUcRGlL2hA6ysilp0ZyVsGcrlZuhe44vrVb9CsRb2AWg",
                        CategorySlug = "vitamines",
                        CategoryName = "Vitamines & Compléments",
                        StockQuantity = 18,
                        RxRequired = false,
                        BadgeText = "En Stock",
                        Ingredients = new List<string> { "Vitamines A, C, D3, E, B6, B12", "Zinc", "Magnésium", "Calcium" },
                        DosageInstructions = new List<string> { "Prendre 2 gélules par jour lors du petit-déjeuner." }
                    },
                    new Product
                    {
                        Id = 3,
                        Name = "Anti-Allergique 24H Non Somnolent",
                        Brand = "SVR",
                        ShortDescription = "Soulagement rapide 24 heures contre les éternuements, le nez qui coule et les yeux irrités.",
                        FullDescription = "Offre une protection efficace toute la journée sans provoquer de somnolence contre le pollen, les acariens et les poils d'animaux.",
                        Price = 18.50m,
                        OriginalPrice = 22.00m,
                        Rating = 4.9,
                        ReviewCount = 210,
                        ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAKMhzKBFUsI3a4bSJ87QrHgw4Z-pZNj3H9UzJIMfgdNJnxZ-FPk1yKyMmU5f3WrYINIiFYim0IYCrt2ak1xKI19ODwIwdNjllNtqgn8UqvvCWk8ABSWYDJWXzDlDOpoqY9hQz-lY8KirhE9PKFP3TfsoU27DDQUb_rt3K6Cm9-eX6hPagnv27G7MRs3imYxBC8-vHp6cEPsiyi_UWZ05aZg2aOwIkhidg_3-xBlgExYEP0ix4KDHtM1Q",
                        CategorySlug = "vitamines",
                        CategoryName = "Parapharmacie",
                        StockQuantity = 2,
                        RxRequired = true,
                        BadgeText = "Sur Ordonnance",
                        Ingredients = new List<string> { "Cétirizine HCI 10mg" },
                        DosageInstructions = new List<string> { "Prendre 1 comprimé de 10mg une fois par jour." }
                    },
                    new Product
                    {
                        Id = 4,
                        Name = "Thermomètre Digital Médical Instantané",
                        Brand = "Dermedic",
                        ShortDescription = "Précision médicale en 8 secondes avec écran LCD rétroéclairé et alarme de fièvre.",
                        FullDescription = "Thermomètre numérique infrarouge ultra-rapide et précis adapté aux adultes, enfants et nourrissons. Mémorise les 20 dernières mesures.",
                        Price = 12.99m,
                        OriginalPrice = 16.99m,
                        Rating = 4.6,
                        ReviewCount = 67,
                        ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuDQPE8radJxnGRhhkdD7Yw0WNChnLQZeAZ81qkztYNv_yOZh8TLS1sUApb6QP7ds8G-6vb4T4zMEGWEIMaU13PGV41hEAPEeyHsJRsVxb6fzAT29flj6bYrMbNI0QE95wSiD9ZUxWyCglHkPqLqVrQz9NdDIoTQkqg5EUBUKLaY0uy9YKcAZozz2N9tBOWSdemp927O42cznBAlOiMxmt8vijclwFP9z6bj8HZAZROp1Xhp69bf8-SCsQ",
                        CategorySlug = "materiel-medical",
                        CategoryName = "Matériel Médical",
                        StockQuantity = 0,
                        RxRequired = false,
                        BadgeText = "Rupture",
                        Ingredients = new List<string> { "Plastique ABS médical", "Sonde en acier inoxydable" },
                        DosageInstructions = new List<string> { "Placer sous la langue ou sous l'aisselle. Attendre le signal sonore." }
                    },
                    new Product
                    {
                        Id = 5,
                        Name = "Gel Nettoyant Hydratant Doux, 350ml",
                        Brand = "Dermacare",
                        ShortDescription = "Nettoyant visage quotidien testé sous contrôle dermatologique à l'acide hyaluronique et céramides.",
                        FullDescription = "Nettoie, hydrate et aide à restaurer la barrière protectrice de la peau sans altérer son hydratation naturelle.",
                        Price = 15.00m,
                        OriginalPrice = 19.00m,
                        Rating = 4.9,
                        ReviewCount = 142,
                        ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuA_F3uqJwqseHGTZYQSraAFYmuN8HwNos1fyRXTEA_pRojSD4yjYYh8-d-tYxX0CUQq8tdTz_EINdQoO9kDVK5ZmeVKcYynSny_CN_frYMuRtIgt8rCREOkhJiNVjKuTF5uHeeJo5zOhaIsvUW1g05dZPLk_6rioK4WZ53f9m1bXHG1EU77pebJ5BBijVaeEsTmXlls1dgMSg4lqaIspAVBzuUI_oF53H4gYHZ8RxIzcq69uEOmQutjsg",
                        CategorySlug = "soins-visage",
                        CategoryName = "Soins du Visage",
                        StockQuantity = 14,
                        RxRequired = false,
                        BadgeText = "Meilleure Vente",
                        Ingredients = new List<string> { "Acide Hyaluronique", "Céramides 1, 3, 6-II", "Glycérine Végétale" },
                        DosageInstructions = new List<string> { "Appliquer sur peau humide, masser doucement puis rincer à l'eau tiède." }
                    },
                    new Product
                    {
                        Id = 6,
                        Name = "Vitamine D3 5000 UI Capsules Softgels",
                        Brand = "Avène",
                        ShortDescription = "Soutien osseux et immunitaire essentiel formulé avec de l'huile d'olive biologique.",
                        FullDescription = "La vitamine D3 haute puissance fournit la forme biologiquement active de vitamine D produite naturellement par l'organisme.",
                        Price = 18.99m,
                        OriginalPrice = 24.00m,
                        Rating = 4.8,
                        ReviewCount = 175,
                        ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCl1EhdssDxs-N6HxnrZQ6J4fVbu7jCd86KZfC7gjD0T--ovR3y5TvMzIoRwWDzZEZ7EjgewUJWnVw32wpa36s6KZoM_xTVxgiTm-4joVUtW70gJWYGQjsn0VAk59C-UeVvtnJp5jETGiqFzLHAI18mA6Y_-XMsYJ3aocB7Jo2ILHAduyRIj0nD_UIci3B1Ge8CDrEIKMQ6_fzo82EmHMozmTLDuaqiDETfcghjubunyobWZXLyvmmExA",
                        CategorySlug = "vitamines",
                        CategoryName = "Vitamines & Compléments",
                        StockQuantity = 3,
                        RxRequired = false,
                        BadgeText = "Top Avis",
                        Ingredients = new List<string> { "Vitamine D3 (Cholécalciférol) 125mcg (5000 UI)", "Huile d'Olive Vierge Extra Bio" },
                        DosageInstructions = new List<string> { "Prendre 1 capsule par jour au cours d'un repas principal." }
                    },
                    new Product
                    {
                        Id = 7,
                        Name = "Picolinate de Zinc 50mg Gélules",
                        Brand = "Ducray",
                        ShortDescription = "Complément de zinc hautement assimilable pour la santé cellulaire et l'immunité.",
                        FullDescription = "Le picolinate de zinc est une forme chélatée associée à l'acide picolinique pour garantir une assimilation cellulaire maximale.",
                        Price = 14.50m,
                        OriginalPrice = 17.50m,
                        Rating = 4.7,
                        ReviewCount = 92,
                        ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuDztkElr9DG_j_znuL02gfi5OR1fZfY0tUbcQXf-DLAjbfEJh5N6QQFy-T4VsA7HrEujMzpxVTr5ZqhtYdfgH5--KpjoiGBx7faXcPlBAsZVpVabpA5pIWD-Hb9Bjqjg9MEq0YZsZZ03PFDc392xmW1xDV0MYk0GZ4KFarVxzk8cs_yJMIBFPAL00f7hzCsQ-jydvRvNaeh--AeJeNEzOMnkQWvZkO9ob3kI_Z7FYFD1JUMPMlyhiVyqg",
                        CategorySlug = "vitamines",
                        CategoryName = "Vitamines & Compléments",
                        StockQuantity = 35,
                        RxRequired = false,
                        BadgeText = "Meilleure Vente",
                        Ingredients = new List<string> { "Zinc (issu de 250mg de Picolinate de Zinc) 50mg" },
                        DosageInstructions = new List<string> { "Prendre 1 gélule par jour avec un verre d'eau." }
                    },
                    new Product
                    {
                        Id = 8,
                        Name = "Anaphase+ • Shampoing Énergisant Antichute, 200ml",
                        Brand = "Ducray",
                        ShortDescription = "Élimination durable des impuretés et fortification du cuir chevelu.",
                        FullDescription = "Formule haute tolérance enrichie en piroctone olamine et extraits botaniques purifiants. Élimine visiblement les pellicules dès les premières applications tout en calmant les sensations de démangeaisons.",
                        Price = 16.90m,
                        OriginalPrice = 22.00m,
                        Rating = 4.9,
                        ReviewCount = 124,
                        ImageUrl = "https://images.unsplash.com/photo-1535585209827-a15fcdbc4c2d?auto=format&fit=crop&w=800&q=80",
                        CategorySlug = "soins-visage",
                        CategoryName = "Soins Capillaires",
                        StockQuantity = 25,
                        RxRequired = false,
                        BadgeText = "Culte Capillaire",
                        Ingredients = new List<string> { "Piroctone Olamine", "Extrait de Saule Blanc", "Menthol Fraîcheur", "Zinc Pyrithione" },
                        DosageInstructions = new List<string> { "Appliquer 3 fois par semaine sur cheveux mouillés, masser doucement et rincer abondamment." }
                    }
                );
                context.SaveChanges();
            }

            // Seed Stock Movements
            if (!context.StockMovements.Any())
            {
                context.StockMovements.AddRange(
                    new StockMovement
                    {
                        ProductId = 1,
                        ProductName = "Acide Ascorbique (Vitamine C) 1000mg",
                        ProductImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCZ2e8wA5GHLGeqd2-GRkEpnXg3tA-cwf4FKgZqpSmcT3wEIXTLV2lZ2L7GWzkAHpOAd_yxJChfy12O_f3J-tdCkJ1hMrp-hb0viwy-3cPCEc1nH2NNJ9BnpB88WFxl-OFICe7ge5L7kVIP3WTueJK9ZKOyWK8IrRHiDFof39zraR-hgfvzehfm5NhC0bPCQNJFsbiitlvUbTG_8ARaC45Aead4CTSD9sZyvUqIq-ekpxxO5jM9_tRGAw",
                        QuantityChange = 30,
                        PreviousStock = 0,
                        NewStock = 30,
                        Reason = "Stock initial & Réception Fournisseur",
                        ReferenceType = "Restock",
                        Timestamp = DateTime.Now.AddDays(-3)
                    },
                    new StockMovement
                    {
                        ProductId = 1,
                        ProductName = "Acide Ascorbique (Vitamine C) 1000mg",
                        ProductImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCZ2e8wA5GHLGeqd2-GRkEpnXg3tA-cwf4FKgZqpSmcT3wEIXTLV2lZ2L7GWzkAHpOAd_yxJChfy12O_f3J-tdCkJ1hMrp-hb0viwy-3cPCEc1nH2NNJ9BnpB88WFxl-OFICe7ge5L7kVIP3WTueJK9ZKOyWK8IrRHiDFof39zraR-hgfvzehfm5NhC0bPCQNJFsbiitlvUbTG_8ARaC45Aead4CTSD9sZyvUqIq-ekpxxO5jM9_tRGAw",
                        QuantityChange = -6,
                        PreviousStock = 30,
                        NewStock = 24,
                        Reason = "Vente Commande #CMD-94281",
                        ReferenceType = "Order",
                        Timestamp = DateTime.Now.AddHours(-6)
                    },
                    new StockMovement
                    {
                        ProductId = 3,
                        ProductName = "Anti-Allergique 24H Non Somnolent",
                        ProductImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAKMhzKBFUsI3a4bSJ87QrHgw4Z-pZNj3H9UzJIMfgdNJnxZ-FPk1yKyMmU5f3WrYINIiFYim0IYCrt2ak1xKI19ODwIwdNjllNtqgn8UqvvCWk8ABSWYDJWXzDlDOpoqY9hQz-lY8KirhE9PKFP3TfsoU27DDQUb_rt3K6Cm9-eX6hPagnv27G7MRs3imYxBC8-vHp6cEPsiyi_UWZ05aZg2aOwIkhidg_3-xBlgExYEP0ix4KDHtM1Q",
                        QuantityChange = -8,
                        PreviousStock = 10,
                        NewStock = 2,
                        Reason = "Vente Commande #CMD-94280",
                        ReferenceType = "Order",
                        Timestamp = DateTime.Now.AddHours(-4)
                    }
                );
                context.SaveChanges();
            }

            // Seed Orders
            if (!context.Orders.Any())
            {
                var order1 = new Order
                {
                    OrderNumber = "CMD-94281",
                    CustomerName = "Éléonore de Montmirail",
                    Phone = "06 12 34 56 78",
                    Email = "eleonore@montmirail.fr",
                    City = "Sousse",
                    PostalCode = "4000",
                    Address = "12 Avenue Habib Bourguiba",
                    OrderDate = DateTime.Now.AddHours(-6),
                    Subtotal = 148.50m,
                    ShippingFee = 0.00m,
                    TotalAmount = 148.50m,
                    Status = "Expédiée",
                    PaymentMethod = "Paiement à la livraison (Espèces)",
                    StockDeducted = true,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            OrderNumber = "CMD-94281",
                            ProductId = 1,
                            ProductName = "Acide Ascorbique (Vitamine C) 1000mg",
                            Brand = "Eucerin",
                            UnitPrice = 24.99m,
                            Quantity = 2,
                            ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCZ2e8wA5GHLGeqd2-GRkEpnXg3tA-cwf4FKgZqpSmcT3wEIXTLV2lZ2L7GWzkAHpOAd_yxJChfy12O_f3J-tdCkJ1hMrp-hb0viwy-3cPCEc1nH2NNJ9BnpB88WFxl-OFICe7ge5L7kVIP3WTueJK9ZKOyWK8IrRHiDFof39zraR-hgfvzehfm5NhC0bPCQNJFsbiitlvUbTG_8ARaC45Aead4CTSD9sZyvUqIq-ekpxxO5jM9_tRGAw"
                        }
                    }
                };

                var order2 = new Order
                {
                    OrderNumber = "CMD-94280",
                    CustomerName = "Dr. Alexandre Garnier",
                    Phone = "06 98 76 54 32",
                    Email = "dr.garnier@clinique.fr",
                    City = "Enfidha",
                    PostalCode = "4030",
                    Address = "Route de l'Aéroport, Résidence Les Oliviers",
                    OrderDate = DateTime.Now.AddHours(-4),
                    Subtotal = 89.90m,
                    ShippingFee = 0.00m,
                    TotalAmount = 89.90m,
                    Status = "En préparation",
                    PaymentMethod = "Paiement à la livraison (Espèces)",
                    StockDeducted = true,
                    Items = new List<OrderItem>
                    {
                        new OrderItem
                        {
                            OrderNumber = "CMD-94280",
                            ProductId = 3,
                            ProductName = "Anti-Allergique 24H Non Somnolent",
                            Brand = "SVR",
                            UnitPrice = 18.50m,
                            Quantity = 2,
                            ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAKMhzKBFUsI3a4bSJ87QrHgw4Z-pZNj3H9UzJIMfgdNJnxZ-FPk1yKyMmU5f3WrYINIiFYim0IYCrt2ak1xKI19ODwIwdNjllNtqgn8UqvvCWk8ABSWYDJWXzDlDOpoqY9hQz-lY8KirhE9PKFP3TfsoU27DDQUb_rt3K6Cm9-eX6hPagnv27G7MRs3imYxBC8-vHp6cEPsiyi_UWZ05aZg2aOwIkhidg_3-xBlgExYEP0ix4KDHtM1Q"
                        }
                    }
                };

                context.Orders.AddRange(order1, order2);
                context.SaveChanges();
            }

            // Seed Special Offers (Spotlight Promo Cards)
            if (!context.SpecialOffers.Any())
            {
                context.SpecialOffers.AddRange(
                    new SpecialOffer
                    {
                        Badge = "-25% • Édition Limitée",
                        Title = "Coffret Rituel Éclat & Anti-Âge",
                        Description = "Le duo incontournable : Sérum Concentré + Crème Divine aux actifs botaniques français.",
                        OriginalPrice = 54.00m,
                        PromoPrice = 39.99m,
                        ImageUrl = "/images/hero_skincare.jpg",
                        ThemeStyle = "dark-luxury",
                        AssociatedProductId = 5,
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new SpecialOffer
                    {
                        Badge = "-30% • Pack Bien-Être",
                        Title = "Cure Immunité & Vitalité",
                        Description = "Complexe de Vitamine C 1000mg + D3 5000 UI pour un regain d'énergie durable.",
                        OriginalPrice = 49.99m,
                        PromoPrice = 34.90m,
                        ImageUrl = "/images/hero_vitamins.jpg",
                        ThemeStyle = "amber-gold",
                        AssociatedProductId = 1,
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new SpecialOffer
                    {
                        Badge = "-20% • Trousse Maternité",
                        Title = "Trousse Soin Bébé Douceur",
                        Description = "Gel Lavant Ultra-Doux + Crème de Change Protectrice certifiée pédiatrique.",
                        OriginalPrice = 38.00m,
                        PromoPrice = 29.50m,
                        ImageUrl = "/images/hero_babycare.jpg",
                        ThemeStyle = "emerald-mint",
                        AssociatedProductId = 4,
                        DisplayOrder = 3,
                        IsActive = true
                    }
                );
                context.SaveChanges();
            }

            // Seed Story Reels (Live Rituels / Video Demonstrations)
            if (!context.StoryReels.Any())
            {
                context.StoryReels.AddRange(
                    new StoryReel
                    {
                        BrandName = "Eucerin",
                        BadgeText = "Story Dermo",
                        BadgeColor = "red",
                        Title = "Anti-Pigment Sérum Duo",
                        Description = "Réduit visiblement les taches pigmentaires et comble les rides profondes.",
                        VideoUrl = "/videos/v1.mp4",
                        PosterUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=800&q=80",
                        AssociatedProductId = 1,
                        DisplayOrder = 1,
                        IsActive = true
                    },
                    new StoryReel
                    {
                        BrandName = "Avène",
                        BadgeText = "Story Thermal",
                        BadgeColor = "emerald",
                        Title = "Cicalfate+ Soin Réparateur",
                        Description = "Restaure le film protecteur et apaise les irritations cutanées.",
                        VideoUrl = "/videos/v2.mp4",
                        PosterUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=800&q=80",
                        AssociatedProductId = 2,
                        DisplayOrder = 2,
                        IsActive = true
                    },
                    new StoryReel
                    {
                        BrandName = "SVR",
                        BadgeText = "Story Soin",
                        BadgeColor = "teal",
                        Title = "Sebiaclear Sérum Purifiant",
                        Description = "Formulation active concentrée pour lisser le grain de peau et matifier.",
                        VideoUrl = "/videos/v3.mp4",
                        PosterUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?auto=format&fit=crop&w=800&q=80",
                        AssociatedProductId = 3,
                        DisplayOrder = 3,
                        IsActive = true
                    }
                );
                context.SaveChanges();
            }

            // Seed or Update Admin User with Strong Credentials
            var defaultAdmin = context.AdminUsers.FirstOrDefault(u => u.Username == "admin" || u.Username == "admin_natalya" || u.Email == "admin@natalyapara.com" || u.Email == "admin@natalya.tn");
            
            byte[] salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            byte[] hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                "Natalya@Pharma#2026!",
                salt,
                iterations: 100000,
                hashAlgorithm: System.Security.Cryptography.HashAlgorithmName.SHA256,
                outputLength: 32);

            string strongPasswordHash = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

            if (defaultAdmin == null)
            {
                context.AdminUsers.Add(new AdminUser
                {
                    Username = "admin_natalya",
                    Email = "admin@natalyapara.com",
                    PasswordHash = strongPasswordHash,
                    FullName = "Dr. Natalya (Pharmacien Gérant)",
                    Role = "Administrator",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
                context.SaveChanges();
            }
            else
            {
                defaultAdmin.Username = "admin_natalya";
                defaultAdmin.Email = "admin@natalyapara.com";
                defaultAdmin.PasswordHash = strongPasswordHash;
                defaultAdmin.FullName = "Dr. Natalya (Pharmacien Gérant)";
                defaultAdmin.IsActive = true;
                context.SaveChanges();
            }
        }
    }
}
