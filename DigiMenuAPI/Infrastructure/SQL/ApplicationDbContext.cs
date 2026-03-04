using DigiMenuAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigiMenuAPI.Infrastructure.SQL
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // ── Tablas ───────────────────────────────────────────────────
        // Plataforma (gestionadas por SuperAdmin)
        public DbSet<Plan> Plans { get; set; }
        public DbSet<PlatformModule> PlatformModules { get; set; }
        public DbSet<StandardIcon> StandardIcons { get; set; }

        // Tenant raíz
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyModule> CompanyModules { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<AppUser> Users { get; set; }

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
        public DbSet<Setting> Settings { get; set; }
        public DbSet<FooterLink> FooterLinks { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigurePlan(modelBuilder);
            ConfigureCompany(modelBuilder);
            ConfigureBranch(modelBuilder);
            ConfigureAppUser(modelBuilder);
            ConfigurePlatformModule(modelBuilder);
            ConfigureCompanyModule(modelBuilder);
            ConfigureCategory(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureTag(modelBuilder);
            ConfigureCategoryTranslation(modelBuilder);
            ConfigureProductTranslation(modelBuilder);
            ConfigureTagTranslation(modelBuilder);
            ConfigureBranchProduct(modelBuilder);
            ConfigureSetting(modelBuilder);
            ConfigureFooterLink(modelBuilder);
            ConfigureReservation(modelBuilder);

            SeedData(modelBuilder);
        }

        // ── Configuraciones ──────────────────────────────────────────

        private static void ConfigurePlan(ModelBuilder b)
        {
            b.Entity<Plan>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Code).IsRequired().HasMaxLength(50);
                e.Property(p => p.Name).IsRequired().HasMaxLength(100);
                e.Property(p => p.Description).HasMaxLength(300);
                e.Property(p => p.MonthlyPrice).HasPrecision(18, 2);
                e.Property(p => p.AnnualPrice).HasPrecision(18, 2);

                // Código único para referencias en código
                e.HasIndex(p => p.Code).IsUnique();
            });
        }

        private static void ConfigureCompany(ModelBuilder b)
        {
            b.Entity<Company>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);
                e.Property(c => c.Email).IsRequired().HasMaxLength(150);
                e.Property(c => c.Phone).HasMaxLength(20);
                e.Property(c => c.CountryCode).HasMaxLength(3);

                // RESTRICT: el borrado de Plan no debe eliminar Companies.
                // Los planes se desactivan (IsActive = false), no se borran.
                e.HasOne(c => c.Plan)
                 .WithMany(p => p.Companies)
                 .HasForeignKey(c => c.PlanId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBranch(ModelBuilder b)
        {
            b.Entity<Branch>(e =>
            {
                e.HasKey(br => br.Id);
                e.Property(br => br.Name).IsRequired().HasMaxLength(100);
                e.Property(br => br.Slug).IsRequired().HasMaxLength(60);
                e.Property(br => br.Address).HasMaxLength(200);
                e.Property(br => br.Phone).HasMaxLength(20);
                e.Property(br => br.Email).HasMaxLength(150);

                // Slug único global para las URLs del menú público: digimenu.app/{slug}
                e.HasIndex(br => br.Slug).IsUnique();
                e.HasIndex(br => new { br.CompanyId, br.IsDeleted });

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true / IsActive = false).
                e.HasOne(br => br.Company)
                 .WithMany(c => c.Branches)
                 .HasForeignKey(br => br.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Filtro global: excluye branches eliminadas de todas las consultas
                e.HasQueryFilter(br => !br.IsDeleted);
            });
        }

        private static void ConfigureAppUser(ModelBuilder b)
        {
            b.Entity<AppUser>(e =>
            {
                e.HasKey(u => u.Id);
                e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
                e.Property(u => u.Email).IsRequired().HasMaxLength(150);
                e.Property(u => u.PasswordHash).IsRequired();
                e.Property(u => u.Role).HasColumnType("tinyint");

                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => new { u.CompanyId, u.IsDeleted, u.IsActive });
                e.HasIndex(u => new { u.BranchId, u.IsDeleted });

                // RESTRICT: los usuarios se desactivan lógicamente (IsActive = false / IsDeleted = true).
                e.HasOne(u => u.Company)
                 .WithMany(c => c.Users)
                 .HasForeignKey(u => u.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                // BranchId es nullable: CompanyAdmin y SuperAdmin no pertenecen a una Branch específica.
                e.HasOne(u => u.Branch)
                 .WithMany(br => br.Users)
                 .HasForeignKey(u => u.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigurePlatformModule(ModelBuilder b)
        {
            b.Entity<PlatformModule>(e =>
            {
                e.HasKey(pm => pm.Id);
                e.Property(pm => pm.Code).IsRequired().HasMaxLength(50);
                e.Property(pm => pm.Name).IsRequired().HasMaxLength(100);
                e.Property(pm => pm.Description).HasMaxLength(300);

                e.HasIndex(pm => pm.Code).IsUnique();
            });
        }

        private static void ConfigureCompanyModule(ModelBuilder b)
        {
            b.Entity<CompanyModule>(e =>
            {
                e.HasKey(cm => cm.Id);

                // Una empresa no puede tener el mismo módulo dos veces
                e.HasIndex(cm => new { cm.CompanyId, cm.PlatformModuleId }).IsUnique();
                e.HasIndex(cm => new { cm.CompanyId, cm.IsActive });

                // RESTRICT: todos los borrados son lógicos (IsActive = false).
                e.HasOne(cm => cm.Company)
                 .WithMany(c => c.CompanyModules)
                 .HasForeignKey(cm => cm.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(cm => cm.PlatformModule)
                 .WithMany()
                 .HasForeignKey(cm => cm.PlatformModuleId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCategory(ModelBuilder b)
        {
            b.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);

                e.HasIndex(c => new { c.CompanyId, c.IsDeleted, c.DisplayOrder });

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(c => c.Company)
                 .WithMany(co => co.Categories)
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

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(p => p.Company)
                 .WithMany(c => c.Products)
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

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(t => t.Company)
                 .WithMany(c => c.Tags)
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
                 .WithMany(br => br.BranchProducts)
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

        private static void ConfigureSetting(ModelBuilder b)
        {
            b.Entity<Setting>(e =>
            {
                e.HasKey(s => s.Id);

                // Colores con valores por defecto
                e.Property(s => s.PageBackgroundColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(s => s.HeaderBackgroundColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(s => s.HeaderTextColor).HasMaxLength(7).HasDefaultValue("#000000");
                e.Property(s => s.TabBackgroundColor).HasMaxLength(7).HasDefaultValue("#000000");
                e.Property(s => s.TabTextColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(s => s.PrimaryColor).HasMaxLength(7).HasDefaultValue("#E63946");
                e.Property(s => s.PrimaryTextColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(s => s.SecondaryColor).HasMaxLength(7).HasDefaultValue("#457B9D");
                e.Property(s => s.TitlesColor).HasMaxLength(7).HasDefaultValue("#000000");
                e.Property(s => s.TextColor).HasMaxLength(7).HasDefaultValue("#1D3557");
                e.Property(s => s.BrowserThemeColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");

                e.Property(s => s.HeaderStyle).HasColumnType("tinyint").HasDefaultValue((byte)1);
                e.Property(s => s.MenuLayout).HasColumnType("tinyint").HasDefaultValue((byte)1);
                e.Property(s => s.ProductDisplay).HasColumnType("tinyint").HasDefaultValue((byte)1);

                // Relación 1:1 Branch → Setting (índice único en BranchId)
                e.HasIndex(s => s.BranchId).IsUnique();

                // RESTRICT: el borrado de Branch se gestiona lógicamente (IsDeleted = true).
                e.HasOne(s => s.Branch)
                 .WithOne(br => br.Setting)
                 .HasForeignKey<Setting>(s => s.BranchId)
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
                 .WithMany(br => br.FooterLinks)
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

                e.HasIndex(r => new { r.BranchId, r.IsDeleted, r.ReservationDate });

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true).
                e.HasOne(r => r.Branch)
                 .WithMany(br => br.Reservations)
                 .HasForeignKey(r => r.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Filtro global: excluye reservas eliminadas de todas las consultas
                e.HasQueryFilter(r => !r.IsDeleted);
            });
        }

        // ── Data Seeding ─────────────────────────────────────────────

        private static void SeedData(ModelBuilder b)
        {
            SeedPlans(b);
            SeedStandardIcons(b);
            SeedPlatformModules(b);
            SeedMasterCompany(b);
            SeedMasterBranch(b);
            SeedMasterUser(b);
            SeedMasterSetting(b);
            SeedMasterTags(b);
            SeedMasterCategories(b);
            SeedMasterProducts(b);
            SeedMasterFooterLinks(b);
            SeedMasterCompanyModules(b);
        }

        private static void SeedPlans(ModelBuilder b)
        {
            b.Entity<Plan>().HasData(
                new Plan
                {
                    Id           = 1,
                    Code         = "BASIC",
                    Name         = "Básico",
                    Description  = "Ideal para negocios con una sola sucursal.",
                    MonthlyPrice = 0m,
                    AnnualPrice  = null,
                    MaxBranches  = 1,
                    MaxUsers     = 3,
                    IsPublic     = true,
                    IsActive     = true,
                    DisplayOrder = 1
                },
                new Plan
                {
                    Id           = 2,
                    Code         = "PRO",
                    Name         = "Pro",
                    Description  = "Para negocios en crecimiento con hasta 3 sucursales.",
                    MonthlyPrice = 29.99m,
                    AnnualPrice  = 24.99m,
                    MaxBranches  = 3,
                    MaxUsers     = 10,
                    IsPublic     = true,
                    IsActive     = true,
                    DisplayOrder = 2
                },
                new Plan
                {
                    Id           = 3,
                    Code         = "BUSINESS",
                    Name         = "Business",
                    Description  = "Para cadenas con múltiples locales.",
                    MonthlyPrice = 79.99m,
                    AnnualPrice  = 64.99m,
                    MaxBranches  = 10,
                    MaxUsers     = 30,
                    IsPublic     = true,
                    IsActive     = true,
                    DisplayOrder = 3
                },
                new Plan
                {
                    Id           = 4,
                    Code         = "ENTERPRISE",
                    Name         = "Enterprise",
                    Description  = "Sin límites. Ideal para grandes cadenas y franquicias.",
                    MonthlyPrice = 199.99m,
                    AnnualPrice  = 159.99m,
                    MaxBranches  = -1,   // -1 = ilimitado
                    MaxUsers     = -1,   // -1 = ilimitado
                    IsPublic     = true,
                    IsActive     = true,
                    DisplayOrder = 4
                }
            );
        }

        private static void SeedStandardIcons(ModelBuilder b)
        {
            b.Entity<StandardIcon>().HasData(
                new StandardIcon { Id = 1, Name = "Facebook",  SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z'></path></svg>" },
                new StandardIcon { Id = 2, Name = "Instagram", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='2' y='2' width='20' height='20' rx='5' ry='5'></rect><path d='M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z'></path><line x1='17.5' y1='6.5' x2='17.51' y2='6.5'></line></svg>" },
                new StandardIcon { Id = 3, Name = "WhatsApp",  SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 1 1-7.6-14h.1c4.3 0 7.9 3.5 8.4 7.7z'></path><path d='M17 16l-4-4 4-4'></path></svg>" },
                new StandardIcon { Id = 4, Name = "TikTok",    SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M9 12a4 4 0 1 0 4 4V4a5 5 0 0 0 5 5'></path></svg>" },
                new StandardIcon { Id = 5, Name = "YouTube",   SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M22.54 6.42a2.78 2.78 0 0 0-1.95-1.96C18.88 4 12 4 12 4s-6.88 0-8.59.46a2.78 2.78 0 0 0-1.95 1.96A29 29 0 0 0 1 12a29 29 0 0 0 .46 5.58A2.78 2.78 0 0 0 3.41 19.6C5.12 20 12 20 12 20s6.88 0 8.59-.46a2.78 2.78 0 0 0 1.95-1.95A29 29 0 0 0 23 12a29 29 0 0 0-.46-5.58z'></path><polygon points='9.75 15.02 15.5 12 9.75 8.98 9.75 15.02'></polygon></svg>" },
                new StandardIcon { Id = 6, Name = "X",         SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M4 4l16 16M4 20L20 4'/></svg>" },
                new StandardIcon { Id = 7, Name = "Web",       SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'></circle><line x1='2' y1='12' x2='22' y2='12'></line><path d='M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z'></path></svg>" }
            );
        }

        private static void SeedPlatformModules(ModelBuilder b)
        {
            b.Entity<PlatformModule>().HasData(
                new PlatformModule { Id = 1, Code = "RESERVATIONS",     Name = "Reservaciones",      Description = "Gestión de reservas de mesas desde el menú público.",   IsActive = true, DisplayOrder = 1 },
                new PlatformModule { Id = 2, Code = "TABLE_MANAGEMENT", Name = "Gestión de Mesas",   Description = "Control visual del estado de las mesas del local.",     IsActive = true, DisplayOrder = 2 },
                new PlatformModule { Id = 3, Code = "ANALYTICS",        Name = "Analíticas",         Description = "Reportes de visitas, productos más vistos y reservas.", IsActive = true, DisplayOrder = 3 },
                new PlatformModule { Id = 4, Code = "ONLINE_ORDERS",    Name = "Pedidos en Línea",   Description = "Permite recibir pedidos directamente desde el menú.",   IsActive = true, DisplayOrder = 4 }
            );
        }

        private static void SeedMasterCompany(ModelBuilder b)
        {
            // Empresa maestra de la plataforma (para demo y desarrollo).
            // PlanId = 4 (ENTERPRISE) → sin límites de branches ni usuarios.
            b.Entity<Company>().HasData(new Company
            {
                Id          = 1,
                Name        = "DigiMenu Platform",
                Email       = "admin@digimenu.app",
                Phone       = "+50600000000",
                CountryCode = "CR",
                IsActive    = true,
                PlanId      = 4,
                MaxBranches = -1,
                MaxUsers    = -1,
                CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        private static void SeedMasterBranch(ModelBuilder b)
        {
            b.Entity<Branch>().HasData(new Branch
            {
                Id        = 1,
                CompanyId = 1,
                Name      = "Sucursal Principal",
                Slug      = "digimenu-platform",
                Address   = "San José, Costa Rica",
                Phone     = "+50600000000",
                Email     = "admin@digimenu.app",
                IsActive  = true,
                IsDeleted = false,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        private static void SeedMasterUser(ModelBuilder b)
        {
            // ⚠️ IMPORTANTE: cambiar la contraseña en el primer despliegue a producción.
            // Hash generado con BCrypt.Net.BCrypt.HashPassword("Master@2026!", 12)
            b.Entity<AppUser>().HasData(new AppUser
            {
                Id           = 1,
                CompanyId    = 1,
                BranchId     = null,   // SuperAdmin no pertenece a una Branch específica
                FullName     = "Super Admin",
                Email        = "admin@digimenu.app",
                PasswordHash = "$2a$12$REEMPLAZAR_CON_HASH_REAL",
                Role         = 255,
                IsActive     = true,
                IsDeleted    = false,
                CreatedAt    = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        private static void SeedMasterSetting(ModelBuilder b)
        {
            b.Entity<Setting>().HasData(new Setting
            {
                Id                   = 1,
                BranchId             = 1,
                BusinessName         = "DigiMenu Demo",
                Tagline              = "El mejor menú digital para tu restaurante",
                IsDarkMode           = false,
                PageBackgroundColor  = "#F1FAEE",
                HeaderBackgroundColor= "#1D3557",
                HeaderTextColor      = "#FFFFFF",
                TabBackgroundColor   = "#457B9D",
                TabTextColor         = "#FFFFFF",
                PrimaryColor         = "#E63946",
                PrimaryTextColor     = "#FFFFFF",
                SecondaryColor       = "#457B9D",
                TitlesColor          = "#1D3557",
                TextColor            = "#1D3557",
                BrowserThemeColor    = "#1D3557",
                HeaderStyle          = 1,
                MenuLayout           = 1,
                ProductDisplay       = 1,
                ShowProductDetails   = true,
                ShowSearchButton     = true,
                ShowContactButton    = true,
                CountryCode          = "CR",
                PhoneCode            = "+506",
                Currency             = "CRC",
                CurrencyLocale       = "es-CR",
                Language             = "es",
                TimeZone             = "America/Costa_Rica",
                Decimals             = 0,
                MetaTitle            = "DigiMenu Demo",
                MetaDescription      = "El mejor menú digital para tu restaurante",
                FormShowName         = true,
                FormRequireName      = true,
                FormShowPhone        = true,
                FormRequirePhone     = true,
                FormShowTable        = false,
                FormRequireTable     = false,
                FormShowPersons      = true,
                FormRequirePersons   = true,
                FormShowAllergies    = false,
                FormRequireAllergies = false,
                FormShowBirthday     = false,
                FormRequireBirthday  = false,
                FormShowComments     = true,
                FormRequireComments  = false,
                CreatedAt            = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
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

        private static void SeedMasterCompanyModules(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            b.Entity<CompanyModule>().HasData(
                new CompanyModule { Id = 1, CompanyId = 1, PlatformModuleId = 1, IsActive = true, ActivatedAt = seed, ActivatedByUserId = 1 },
                new CompanyModule { Id = 2, CompanyId = 1, PlatformModuleId = 2, IsActive = true, ActivatedAt = seed, ActivatedByUserId = 1 },
                new CompanyModule { Id = 3, CompanyId = 1, PlatformModuleId = 3, IsActive = true, ActivatedAt = seed, ActivatedByUserId = 1 },
                new CompanyModule { Id = 4, CompanyId = 1, PlatformModuleId = 4, IsActive = true, ActivatedAt = seed, ActivatedByUserId = 1 }
            );
        }

        // ── Auditoría Automática ──────────────────────────────────────

        public override int SaveChanges()
        {
            ApplyAuditInfo();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInfo();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInfo()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                    // Evitamos que se modifique la fecha de creación original
                    entry.Property(x => x.CreatedAt).IsModified = false;
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }
    }
}
