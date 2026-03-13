using AppCore.Infrastructure.SQL;
using DigiMenuAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigiMenuAPI.Infrastructure.SQL
{
    public class ApplicationDbContext : CoreDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options, httpContextAccessor)
        {
        }

        // ── Tablas DigiMenu-específicas ───────────────────────────────
        // Catálogo global (por Company)
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Tag> Tags { get; set; }

        // Traducciones del catálogo global
        public DbSet<CategoryTranslation> CategoryTranslations { get; set; }
        public DbSet<ProductTranslation> ProductTranslations { get; set; }
        public DbSet<TagTranslation> TagTranslations { get; set; }

        // Por Branch
        public DbSet<BranchProduct> BranchProducts { get; set; }
        public DbSet<BranchReservationForm> BranchReservationForms { get; set; }
        public DbSet<FooterLink> FooterLinks { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureCategory(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureTag(modelBuilder);
            ConfigureCategoryTranslation(modelBuilder);
            ConfigureProductTranslation(modelBuilder);
            ConfigureTagTranslation(modelBuilder);
            ConfigureBranchProduct(modelBuilder);
            ConfigureFooterLink(modelBuilder);
            ConfigureBranchReservationForm(modelBuilder);
            ConfigureReservation(modelBuilder);

            SeedMenuData(modelBuilder);
        }

        // ── Configuraciones DigiMenu-específicas ──────────────────────

        private static void ConfigureCategory(ModelBuilder b)
        {
            b.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);

                e.HasIndex(c => new { c.CompanyId, c.IsDeleted, c.DisplayOrder });
                e.HasIndex(c => new { c.CompanyId, c.Name }).IsUnique();

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(c => c.Company)
                 .WithMany()
                 .HasForeignKey(c => c.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Filtro global: excluye categorías eliminadas de todas las consultas
                e.HasQueryFilter(c => !c.IsDeleted);
            });
        }

        private static void ConfigureProduct(ModelBuilder b)
        {
            b.Entity<Product>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).IsRequired().HasMaxLength(150);

                e.HasIndex(p => new { p.CompanyId, p.IsDeleted });
                e.HasIndex(p => p.CategoryId);
                e.HasIndex(p => new { p.CompanyId, p.Name }).IsUnique();

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(p => p.Company)
                 .WithMany()
                 .HasForeignKey(p => p.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                // N:N con Tags via tabla ProductTags
                e.HasMany(p => p.Tags)
                 .WithMany(t => t.Products)
                 .UsingEntity(j => j.ToTable("ProductTags"));

                // Filtro global: excluye productos eliminados de todas las consultas
                e.HasQueryFilter(p => !p.IsDeleted);
            });
        }

        private static void ConfigureTag(ModelBuilder b)
        {
            b.Entity<Tag>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).IsRequired().HasMaxLength(50);
                e.Property(t => t.Color).HasMaxLength(7).HasDefaultValue("#ffffff").IsRequired();

                e.HasIndex(t => new { t.CompanyId, t.IsDeleted });
                e.HasIndex(t => new { t.CompanyId, t.Name }).IsUnique();

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(t => t.Company)
                 .WithMany()
                 .HasForeignKey(t => t.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Filtro global: excluye tags eliminadas de todas las consultas
                e.HasQueryFilter(t => !t.IsDeleted);
            });
        }

        private static void ConfigureCategoryTranslation(ModelBuilder b)
        {
            b.Entity<CategoryTranslation>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.LanguageCode).IsRequired().HasMaxLength(5);
                e.Property(t => t.Name).IsRequired().HasMaxLength(100);

                // Una categoría no puede tener dos traducciones al mismo idioma
                e.HasIndex(t => new { t.CategoryId, t.LanguageCode }).IsUnique();

                // RESTRICT: si se elimina la categoría (lógicamente), las traducciones quedan
                // intactas. Se limpian manualmente en el servicio si fuera necesario.
                e.HasOne(t => t.Category)
                 .WithMany(c => c.Translations)
                 .HasForeignKey(t => t.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureProductTranslation(ModelBuilder b)
        {
            b.Entity<ProductTranslation>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.LanguageCode).IsRequired().HasMaxLength(5);
                e.Property(t => t.Name).IsRequired().HasMaxLength(150);

                // Un producto no puede tener dos traducciones al mismo idioma
                e.HasIndex(t => new { t.ProductId, t.LanguageCode }).IsUnique();

                // RESTRICT: las traducciones son datos históricos valiosos.
                e.HasOne(t => t.Product)
                 .WithMany(p => p.Translations)
                 .HasForeignKey(t => t.ProductId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureTagTranslation(ModelBuilder b)
        {
            b.Entity<TagTranslation>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.LanguageCode).IsRequired().HasMaxLength(5);
                e.Property(t => t.Name).IsRequired().HasMaxLength(50);

                // Un tag no puede tener dos traducciones al mismo idioma
                e.HasIndex(t => new { t.TagId, t.LanguageCode }).IsUnique();

                // RESTRICT: las traducciones son datos históricos valiosos.
                e.HasOne(t => t.Tag)
                 .WithMany(t => t.Translations)
                 .HasForeignKey(t => t.TagId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBranchProduct(ModelBuilder b)
        {
            b.Entity<BranchProduct>(e =>
            {
                e.HasKey(bp => bp.Id);
                e.Property(bp => bp.Price).HasPrecision(18, 2);
                e.Property(bp => bp.OfferPrice).HasPrecision(18, 2);

                // Índice único: un producto no puede estar activado dos veces en la misma Branch
                e.HasIndex(bp => new { bp.BranchId, bp.ProductId }).IsUnique();
                e.HasIndex(bp => new { bp.BranchId, bp.IsDeleted, bp.DisplayOrder });

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(bp => bp.Branch)
                 .WithMany()
                 .HasForeignKey(bp => bp.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(bp => bp.Product)
                 .WithMany(p => p.BranchProducts)
                 .HasForeignKey(bp => bp.ProductId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(bp => bp.Category)
                 .WithMany(c => c.BranchProducts)
                 .HasForeignKey(bp => bp.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Filtro global: excluye BranchProducts eliminados de todas las consultas
                e.HasQueryFilter(bp => !bp.IsDeleted);
            });
        }

        private static void ConfigureBranchReservationForm(ModelBuilder b)
        {
            b.Entity<BranchReservationForm>(e =>
            {
                e.HasKey(f => f.Id);

                // 1:1 con Branch — opcional, solo existe si el módulo está activo
                e.HasIndex(f => f.BranchId).IsUnique();
                e.HasOne(f => f.Branch)
                 .WithOne()
                 .HasForeignKey<BranchReservationForm>(f => f.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureFooterLink(ModelBuilder b)
        {
            b.Entity<FooterLink>(e =>
            {
                e.HasKey(f => f.Id);
                e.Property(f => f.Label).IsRequired().HasMaxLength(50);
                e.Property(f => f.Url).IsRequired().HasMaxLength(500);

                e.HasIndex(f => new { f.BranchId, f.IsDeleted, f.DisplayOrder });
                e.HasIndex(f => f.StandardIconId);

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(f => f.Branch)
                 .WithMany()
                 .HasForeignKey(f => f.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);

                // RESTRICT: si se elimina un StandardIcon, el servicio limpia el FK antes.
                e.HasOne(f => f.StandardIcon)
                 .WithMany()
                 .HasForeignKey(f => f.StandardIconId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Filtro global: excluye footer links eliminados de todas las consultas
                e.HasQueryFilter(f => !f.IsDeleted);
            });
        }

        private static void ConfigureReservation(ModelBuilder b)
        {
            b.Entity<Reservation>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.CustomerName).IsRequired().HasMaxLength(100);
                e.Property(r => r.Phone).IsRequired().HasMaxLength(20);
                e.Property(r => r.TableNumber).HasMaxLength(20);
                e.Property(r => r.Allergies).HasMaxLength(500);
                e.Property(r => r.Comments).HasMaxLength(500);

                e.Property(r => r.Status).HasConversion<byte>();
                e.HasIndex(r => new { r.BranchId, r.IsDeleted, r.ReservationDate });

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(r => r.Branch)
                 .WithMany()
                 .HasForeignKey(r => r.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Filtro global: excluye reservas eliminadas de todas las consultas
                e.HasQueryFilter(r => !r.IsDeleted);
            });
        }

        // ── Data Seeding DigiMenu-específico ──────────────────────────

        private static void SeedMenuData(ModelBuilder b)
        {
            SeedMasterBranchReservationForm(b);
            SeedMasterTags(b);
            SeedMasterCategories(b);
            SeedMasterProducts(b);
            SeedMasterFooterLinks(b);
        }

        private static void SeedMasterBranchReservationForm(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // BranchReservationForm del seed — la empresa demo tiene el módulo activo
            b.Entity<BranchReservationForm>().HasData(new BranchReservationForm
            {
                Id = 1,
                BranchId = 1,
                FormShowName = true,
                FormRequireName = true,
                FormShowPhone = true,
                FormRequirePhone = true,
                FormShowTable = false,
                FormRequireTable = false,
                FormShowPersons = true,
                FormRequirePersons = true,
                FormShowAllergies = false,
                FormRequireAllergies = false,
                FormShowBirthday = false,
                FormRequireBirthday = false,
                FormShowComments = true,
                FormRequireComments = false,
                CreatedAt = seed
            });
        }

        private static void SeedMasterTags(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            b.Entity<Tag>().HasData(
                new Tag { Id = 1, CompanyId = 1, Name = "Vegano",     Color = "#4CAF50", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 2, CompanyId = 1, Name = "Picante",    Color = "#F44336", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 3, CompanyId = 1, Name = "Alcohólico", Color = "#9C27B0", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 4, CompanyId = 1, Name = "Sin Gluten", Color = "#FF9800", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 5, CompanyId = 1, Name = "Popular",    Color = "#F50057", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 6, CompanyId = 1, Name = "Nuevo",      Color = "#2196F3", IsDeleted = false, CreatedAt = seed }
            );
        }

        private static void SeedMasterCategories(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            b.Entity<Category>().HasData(
                new Category { Id = 1, CompanyId = 1, Name = "Entradas",            DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Category { Id = 2, CompanyId = 1, Name = "Platos Fuertes",      DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Category { Id = 3, CompanyId = 1, Name = "Postres",             DisplayOrder = 3, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Category { Id = 4, CompanyId = 1, Name = "Bebidas",             DisplayOrder = 4, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Category { Id = 5, CompanyId = 1, Name = "Bebidas Alcohólicas", DisplayOrder = 5, IsVisible = true, IsDeleted = false, CreatedAt = seed }
            );
        }

        private static void SeedMasterProducts(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Productos del catálogo global (sin precio, el precio va en BranchProduct)
            b.Entity<Product>().HasData(
                new Product { Id = 1,  CompanyId = 1, CategoryId = 1, Name = "Ceviche Clásico",         ShortDescription = "Fresco ceviche de corvina con limón y culantro.",      IsDeleted = false, CreatedAt = seed },
                new Product { Id = 2,  CompanyId = 1, CategoryId = 1, Name = "Patacones con Guacamole", ShortDescription = "Patacones crocantes con guacamole casero.",             IsDeleted = false, CreatedAt = seed },
                new Product { Id = 3,  CompanyId = 1, CategoryId = 2, Name = "Casado Tradicional",      ShortDescription = "Arroz, frijoles, ensalada, maduro y carne a elegir.",   IsDeleted = false, CreatedAt = seed },
                new Product { Id = 4,  CompanyId = 1, CategoryId = 2, Name = "Lomo al Chimichurri",     ShortDescription = "Lomo de res al punto con salsa chimichurri.",           IsDeleted = false, CreatedAt = seed },
                new Product { Id = 5,  CompanyId = 1, CategoryId = 2, Name = "Bowl Vegano",             ShortDescription = "Quinoa, vegetales asados, hummus y tahini.",            IsDeleted = false, CreatedAt = seed },
                new Product { Id = 6,  CompanyId = 1, CategoryId = 3, Name = "Tres Leches",             ShortDescription = "Bizcocho esponjoso bañado en tres tipos de leche.",     IsDeleted = false, CreatedAt = seed },
                new Product { Id = 7,  CompanyId = 1, CategoryId = 3, Name = "Brownies con Helado",     ShortDescription = "Brownie de chocolate caliente con helado de vainilla.", IsDeleted = false, CreatedAt = seed },
                new Product { Id = 8,  CompanyId = 1, CategoryId = 4, Name = "Café Americano",          ShortDescription = "Café negro de tueste medio.",                           IsDeleted = false, CreatedAt = seed },
                new Product { Id = 9,  CompanyId = 1, CategoryId = 4, Name = "Refresco Natural",        ShortDescription = "Cas, tamarindo o guanábana. A elegir.",                 IsDeleted = false, CreatedAt = seed },
                new Product { Id = 10, CompanyId = 1, CategoryId = 5, Name = "Imperial",                ShortDescription = "Cerveza nacional 355ml bien fría.",                     IsDeleted = false, CreatedAt = seed },
                new Product { Id = 11, CompanyId = 1, CategoryId = 5, Name = "Guaro Sour",              ShortDescription = "Guaro Cacique, limón, azúcar y hielo.",                 IsDeleted = false, CreatedAt = seed }
            );

            // Activación de productos en la Branch demo con precios
            b.Entity<BranchProduct>().HasData(
                new BranchProduct { Id = 1,  BranchId = 1, ProductId = 1,  CategoryId = 1, Price = 5500m,  OfferPrice = null,   DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 2,  BranchId = 1, ProductId = 2,  CategoryId = 1, Price = 3500m,  OfferPrice = 3000m,  DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 3,  BranchId = 1, ProductId = 3,  CategoryId = 2, Price = 7500m,  OfferPrice = null,   DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 4,  BranchId = 1, ProductId = 4,  CategoryId = 2, Price = 12500m, OfferPrice = 10900m, DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 5,  BranchId = 1, ProductId = 5,  CategoryId = 2, Price = 8500m,  OfferPrice = null,   DisplayOrder = 3, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 6,  BranchId = 1, ProductId = 6,  CategoryId = 3, Price = 3200m,  OfferPrice = null,   DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 7,  BranchId = 1, ProductId = 7,  CategoryId = 3, Price = 3800m,  OfferPrice = null,   DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 8,  BranchId = 1, ProductId = 8,  CategoryId = 4, Price = 1500m,  OfferPrice = null,   DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 9,  BranchId = 1, ProductId = 9,  CategoryId = 4, Price = 1800m,  OfferPrice = null,   DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 10, BranchId = 1, ProductId = 10, CategoryId = 5, Price = 2200m,  OfferPrice = null,   DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new BranchProduct { Id = 11, BranchId = 1, ProductId = 11, CategoryId = 5, Price = 3500m,  OfferPrice = null,   DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed }
            );
        }

        private static void SeedMasterFooterLinks(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            b.Entity<FooterLink>().HasData(
                new FooterLink { Id = 1, BranchId = 1, Label = "Instagram", Url = "https://instagram.com/digimenu", StandardIconId = 2, DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new FooterLink { Id = 2, BranchId = 1, Label = "WhatsApp",  Url = "https://wa.me/50612345678",      StandardIconId = 3, DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed }
            );
        }
    }
}
