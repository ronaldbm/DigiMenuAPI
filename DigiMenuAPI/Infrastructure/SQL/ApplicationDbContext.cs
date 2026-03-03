using DigiMenuAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigiMenuAPI.Infrastructure.SQL
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // ── Tablas ───────────────────────────────────────────────────────────────
        public DbSet<Company> Companies { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<PlatformModule> PlatformModules { get; set; }
        public DbSet<CompanyModule> CompanyModules { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<StandardIcon> StandardIcons { get; set; }
        public DbSet<FooterLink> FooterLinks { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureCompany(modelBuilder);
            ConfigureAppUser(modelBuilder);
            ConfigurePlatformModule(modelBuilder);
            ConfigureCompanyModule(modelBuilder);
            ConfigureSetting(modelBuilder);
            ConfigureCategory(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureTag(modelBuilder);
            ConfigureFooterLink(modelBuilder);
            ConfigureReservation(modelBuilder);

            SeedData(modelBuilder);
        }

        // ── Configuraciones ──────────────────────────────────────────────────────

        private static void ConfigureCompany(ModelBuilder b)
        {
            b.Entity<Company>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);
                e.Property(c => c.Slug).IsRequired().HasMaxLength(60);
                e.Property(c => c.Email).IsRequired().HasMaxLength(150);
                e.Property(c => c.Phone).HasMaxLength(20);
                e.Property(c => c.CountryCode).HasMaxLength(3);

                // Slug único para URLs del menú público: digimenu.app/{slug}
                e.HasIndex(c => c.Slug).IsUnique();
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
                e.HasIndex(u => u.CompanyId);

                // ⚠️ RESTRICT: evita múltiples rutas de cascada en SQL Server.
                // Los usuarios se desactivan lógicamente (IsActive = false), no se borran físicamente.
                e.HasOne(u => u.Company)
                 .WithMany(c => c.Users)
                 .HasForeignKey(u => u.CompanyId)
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

                // Código único del módulo
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

                // Todos los borrados son lógicos (IsActive = false / IsDeleted = true).
                // RESTRICT en todas las FK para evitar borrados físicos accidentales
                // desde scripts directos en BD.
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
                e.Property(s => s.Decimals).HasColumnType("tinyint").HasDefaultValue((byte)2);

                e.Property(s => s.CountryCode).IsRequired().HasMaxLength(3);
                e.Property(s => s.PhoneCode).IsRequired().HasMaxLength(6);
                e.Property(s => s.Currency).IsRequired().HasMaxLength(5);
                e.Property(s => s.CurrencyLocale).IsRequired().HasMaxLength(10);
                e.Property(s => s.Language).IsRequired().HasMaxLength(5);
                e.Property(s => s.TimeZone).IsRequired().HasMaxLength(50);

                e.Property(s => s.MetaTitle).HasMaxLength(100);
                e.Property(s => s.MetaDescription).HasMaxLength(300);
                e.Property(s => s.GoogleAnalyticsId).HasMaxLength(50);
                e.Property(s => s.FacebookPixelId).HasMaxLength(50);

                // Relación 1:1 Company → Setting (índice único en CompanyId)
                e.HasIndex(s => s.CompanyId).IsUnique();

                // RESTRICT: el borrado de Company se gestiona lógicamente (IsActive = false).
                e.HasOne(s => s.Company)
                 .WithOne(c => c.Setting)
                 .HasForeignKey<Setting>(s => s.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCategory(ModelBuilder b)
        {
            b.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);

                // Filtro Global: Ignora eliminados
                e.HasQueryFilter(c => !c.IsDeleted);

                // Índice compuesto para consultas de menú público por empresa
                e.HasIndex(c => new { c.CompanyId, c.IsDeleted, c.DisplayOrder });

                // RESTRICT: el borrado de Company se gestiona lógicamente (IsActive = false).
                // Nunca se ejecuta DELETE físico sobre Companies desde la aplicación.
                e.HasOne(c => c.Company)
                 .WithMany(co => co.Categories)
                 .HasForeignKey(c => c.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureProduct(ModelBuilder b)
        {
            b.Entity<Product>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).IsRequired().HasMaxLength(150);
                e.Property(p => p.BasePrice).HasPrecision(18, 2);
                e.Property(p => p.OfferPrice).HasPrecision(18, 2);

                // Filtro Global: Ignora eliminados
                e.HasQueryFilter(p => !p.IsDeleted);

                e.HasIndex(p => new { p.CompanyId, p.IsDeleted, p.DisplayOrder });
                e.HasIndex(p => p.CategoryId);

                // RESTRICT en ambas FK: todos los borrados son lógicos (IsDeleted = true).
                // No se ejecutan DELETE físicos desde la aplicación, por lo que no se
                // necesita cascada. RESTRICT protege contra scripts accidentales en BD.
                e.HasOne(p => p.Company)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Relación N:N con Tags
                e.HasMany(p => p.Tags)
                 .WithMany(t => t.Products)
                 .UsingEntity(j => j.ToTable("ProductTags"));
            });
        }

        private static void ConfigureTag(ModelBuilder b)
        {
            b.Entity<Tag>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).IsRequired().HasMaxLength(50);
                e.Property(t => t.Color).HasMaxLength(7).HasDefaultValue("#ffffff").IsRequired();

                // Filtro Global: Ignora eliminados
                e.HasQueryFilter(t => !t.IsDeleted);

                e.HasIndex(t => new { t.CompanyId, t.IsDeleted });

                // RESTRICT: el borrado de Company se gestiona lógicamente (IsActive = false).
                e.HasOne(t => t.Company)
                 .WithMany(c => c.Tags)
                 .HasForeignKey(t => t.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureFooterLink(ModelBuilder b)
        {
            b.Entity<FooterLink>(e =>
            {
                e.HasKey(f => f.Id);

                // Filtro Global: Ignora eliminados
                e.HasQueryFilter(f => !f.IsDeleted);

                e.HasIndex(f => new { f.CompanyId, f.IsDeleted, f.DisplayOrder });
                e.HasIndex(f => f.StandardIconId);

                // RESTRICT: el borrado de Company se gestiona lógicamente (IsActive = false).
                e.HasOne(f => f.Company)
                 .WithMany(c => c.FooterLinks)
                 .HasForeignKey(f => f.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                // RESTRICT: si se elimina un StandardIcon, el FooterLink no debe borrarse.
                // El servicio debe limpiar el StandardIconId manualmente antes de eliminar el icono.
                e.HasOne(f => f.StandardIcon)
                 .WithMany()
                 .HasForeignKey(f => f.StandardIconId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureReservation(ModelBuilder b)
        {
            b.Entity<Reservation>(e =>
            {
                e.HasKey(r => r.Id);

                // Filtro Global: Ignora eliminados
                e.HasQueryFilter(r => !r.IsDeleted);

                e.HasIndex(r => new { r.CompanyId, r.IsDeleted, r.ReservationDate });

                // RESTRICT: el borrado de Company se gestiona lógicamente (IsActive = false).
                e.HasOne(r => r.Company)
                 .WithMany(c => c.Reservations)
                 .HasForeignKey(r => r.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        // ── Data Seeding ─────────────────────────────────────────────────────────
        // El seeding usa fechas fijas para evitar que EF genere migraciones
        // innecesarias al comparar con DateTime.UtcNow.

        private static void SeedData(ModelBuilder modelBuilder)
        {
            SeedStandardIcons(modelBuilder);
            SeedPlatformModules(modelBuilder);
            SeedMasterCompany(modelBuilder);
            SeedMasterUser(modelBuilder);
            SeedMasterSetting(modelBuilder);
            SeedMasterTags(modelBuilder);
            SeedMasterCategories(modelBuilder);
            SeedMasterProducts(modelBuilder);
            SeedMasterFooterLinks(modelBuilder);
            SeedMasterCompanyModules(modelBuilder);
        }

        private static void SeedStandardIcons(ModelBuilder b)
        {
            b.Entity<StandardIcon>().HasData(
                new StandardIcon { Id = 1, Name = "Facebook", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z'></path></svg>" },
                new StandardIcon { Id = 2, Name = "Instagram", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='2' y='2' width='20' height='20' rx='5' ry='5'></rect><path d='M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z'></path><line x1='17.5' y1='6.5' x2='17.51' y2='6.5'></line></svg>" },
                new StandardIcon { Id = 3, Name = "WhatsApp", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 1 1-7.6-14h.1c4.3 0 7.9 3.5 8.4 7.7z'></path><path d='M17 16l-4-4 4-4'></path></svg>" },
                new StandardIcon { Id = 4, Name = "TikTok", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M9 12a4 4 0 1 0 4 4V4a5 5 0 0 0 5 5'></path></svg>" },
                new StandardIcon { Id = 5, Name = "YouTube", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M22.54 6.42a2.78 2.78 0 0 0-1.95-1.96C18.88 4 12 4 12 4s-6.88 0-8.59.46A2.78 2.78 0 0 0 1.46 6.42 29 29 0 0 0 1 12a29 29 0 0 0 .46 5.58 2.78 2.78 0 0 0 1.95 1.96C5.12 20 12 20 12 20s6.88 0 8.59-.46a2.78 2.78 0 0 0 1.95-1.96A29 29 0 0 0 23 12a29 29 0 0 0-.46-5.58z'></path><polygon points='9.75 15.02 15.5 12 9.75 8.98 9.75 15.02'></polygon></svg>" },
                new StandardIcon { Id = 6, Name = "X (Twitter)", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M4 4l16 16M4 20L20 4'/></svg>" },
                new StandardIcon { Id = 7, Name = "Web", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><circle cx='12' cy='12' r='10'></circle><line x1='2' y1='12' x2='22' y2='12'></line><path d='M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z'></path></svg>" }
            );
        }

        private static void SeedPlatformModules(ModelBuilder b)
        {
            b.Entity<PlatformModule>().HasData(
                new PlatformModule { Id = 1, Code = "RESERVATIONS", Name = "Reservas", Description = "Gestión de reservas de mesas y eventos.", IsActive = true, DisplayOrder = 1 },
                new PlatformModule { Id = 2, Code = "TABLE_MANAGEMENT", Name = "Gestión de Mesas", Description = "Vista de plano de mesas en tiempo real.", IsActive = true, DisplayOrder = 2 },
                new PlatformModule { Id = 3, Code = "ANALYTICS", Name = "Analytics Avanzados", Description = "Reportes de visitas, productos más vistos y conversiones.", IsActive = true, DisplayOrder = 3 },
                new PlatformModule { Id = 4, Code = "ONLINE_ORDERS", Name = "Pedidos en Línea", Description = "Delivery y take away con integración de pagos.", IsActive = true, DisplayOrder = 4 }
            );
        }

        /// <summary>
        /// Empresa maestra de la plataforma (CompanyId = 1).
        /// Es el "tenant" del SuperAdmin. No representa un restaurante real;
        /// sirve como empresa demo y contenedor del usuario maestro.
        /// </summary>
        private static void SeedMasterCompany(ModelBuilder b)
        {
            b.Entity<Company>().HasData(new Company
            {
                Id = 1,
                Name = "DigiMenu Platform",
                Slug = "digimenu-platform",
                Email = "admin@digimenu.com",
                Phone = null,
                CountryCode = "CR",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        /// <summary>
        /// Usuario maestro de la plataforma (SuperAdmin).
        /// Rol 255 = acceso total a todas las empresas del sistema.
        ///
        /// Credenciales iniciales:
        ///   Email:      admin@digimenu.com
        ///   Contraseña: Master@2026!
        ///
        /// ⚠️ IMPORTANTE: Cambiar la contraseña en el primer inicio de sesión.
        ///
        /// Para regenerar el hash en el futuro:
        ///   BCrypt.Net.BCrypt.HashPassword("NuevaContraseña", workFactor: 12)
        /// </summary>
        private static void SeedMasterUser(ModelBuilder b)
        {
            b.Entity<AppUser>().HasData(new AppUser
            {
                Id = 1,
                FullName = "Super Admin",
                Email = "admin@digimenu.app",
                // BCrypt hash (cost=12) de: Master@2026!
                PasswordHash = "$2y$12$5JPWX19o/4yLCIfshv2Nq.9EGh/UOfw.wiCaiyI2rYxvu19/LU.tW",
                Role = 255,
                IsActive = true,
                CompanyId = 1,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        /// <summary>
        /// Configuración de branding de la empresa maestra / demo.
        /// Sirve como plantilla visual de referencia para nuevas empresas.
        /// </summary>
        private static void SeedMasterSetting(ModelBuilder b)
        {
            b.Entity<Setting>().HasData(new Setting
            {
                Id = 1,
                CompanyId = 1,
                BusinessName = "DigiMenu Demo",
                Tagline = "Tu menú digital, siempre disponible",
                LogoUrl = null,
                FaviconUrl = null,
                BackgroundImageUrl = null,
                IsDarkMode = false,
                PageBackgroundColor = "#F1FAEE",
                HeaderBackgroundColor = "#1D3557",
                HeaderTextColor = "#FFFFFF",
                TabBackgroundColor = "#457B9D",
                TabTextColor = "#FFFFFF",
                PrimaryColor = "#E63946",
                PrimaryTextColor = "#FFFFFF",
                SecondaryColor = "#457B9D",
                TitlesColor = "#1D3557",
                TextColor = "#1D3557",
                BrowserThemeColor = "#1D3557",
                HeaderStyle = 1,
                MenuLayout = 1,
                ProductDisplay = 1,
                ShowProductDetails = true,
                ShowSearchButton = true,
                ShowContactButton = true,
                CountryCode = "CR",
                PhoneCode = "+506",
                Currency = "CRC",
                CurrencyLocale = "es-CR",
                Language = "es",
                TimeZone = "America/Costa_Rica",
                Decimals = 0,
                MetaTitle = "DigiMenu Demo",
                MetaDescription = "El mejor menú digital para tu restaurante",
                GoogleAnalyticsId = null,
                FacebookPixelId = null,
                // Formulario de reservas: configuración mínima por defecto
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
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        private static void SeedMasterTags(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            b.Entity<Tag>().HasData(
                new Tag { Id = 1, CompanyId = 1, Name = "Vegano", Color = "#4CAF50", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 2, CompanyId = 1, Name = "Picante", Color = "#F44336", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 3, CompanyId = 1, Name = "Alcohólico", Color = "#9C27B0", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 4, CompanyId = 1, Name = "Sin Gluten", Color = "#FF9800", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 5, CompanyId = 1, Name = "Popular", Color = "#F50057", IsDeleted = false, CreatedAt = seed },
                new Tag { Id = 6, CompanyId = 1, Name = "Nuevo", Color = "#2196F3", IsDeleted = false, CreatedAt = seed }
            );
        }

        private static void SeedMasterCategories(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            b.Entity<Category>().HasData(
                new Category { Id = 1, CompanyId = 1, Name = "Entradas", DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Category { Id = 2, CompanyId = 1, Name = "Platos Fuertes", DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Category { Id = 3, CompanyId = 1, Name = "Postres", DisplayOrder = 3, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Category { Id = 4, CompanyId = 1, Name = "Bebidas", DisplayOrder = 4, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Category { Id = 5, CompanyId = 1, Name = "Bebidas Alcohólicas", DisplayOrder = 5, IsVisible = true, IsDeleted = false, CreatedAt = seed }
            );
        }

        private static void SeedMasterProducts(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            b.Entity<Product>().HasData(
                // Entradas (CategoryId = 1)
                new Product { Id = 1, CompanyId = 1, CategoryId = 1, Name = "Ceviche Clásico", ShortDescription = "Fresco ceviche de corvina con limón y culantro.", BasePrice = 5500m, OfferPrice = null, DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Product { Id = 2, CompanyId = 1, CategoryId = 1, Name = "Patacones con Guacamole", ShortDescription = "Patacones crocantes con guacamole casero.", BasePrice = 3500m, OfferPrice = 3000m, DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                // Platos Fuertes (CategoryId = 2)
                new Product { Id = 3, CompanyId = 1, CategoryId = 2, Name = "Casado Tradicional", ShortDescription = "Arroz, frijoles, ensalada, maduro y carne a elegir.", BasePrice = 7500m, OfferPrice = null, DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Product { Id = 4, CompanyId = 1, CategoryId = 2, Name = "Lomo al Chimichurri", ShortDescription = "Lomo de res al punto con salsa chimichurri.", BasePrice = 12500m, OfferPrice = 10900m, DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Product { Id = 5, CompanyId = 1, CategoryId = 2, Name = "Bowl Vegano", ShortDescription = "Quinoa, vegetales asados, hummus y tahini.", BasePrice = 8500m, OfferPrice = null, DisplayOrder = 3, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                // Postres (CategoryId = 3)
                new Product { Id = 6, CompanyId = 1, CategoryId = 3, Name = "Tres Leches", ShortDescription = "Bizcocho esponjoso bañado en tres tipos de leche.", BasePrice = 3200m, OfferPrice = null, DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Product { Id = 7, CompanyId = 1, CategoryId = 3, Name = "Brownies con Helado", ShortDescription = "Brownie de chocolate caliente con helado de vainilla.", BasePrice = 3800m, OfferPrice = null, DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                // Bebidas (CategoryId = 4)
                new Product { Id = 8, CompanyId = 1, CategoryId = 4, Name = "Café Americano", ShortDescription = "Café negro de tueste medio.", BasePrice = 1500m, OfferPrice = null, DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Product { Id = 9, CompanyId = 1, CategoryId = 4, Name = "Refresco Natural", ShortDescription = "Cas, tamarindo o guanábana. A elegir.", BasePrice = 1800m, OfferPrice = null, DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                // Bebidas Alcohólicas (CategoryId = 5)
                new Product { Id = 10, CompanyId = 1, CategoryId = 5, Name = "Imperial", ShortDescription = "Cerveza nacional 355ml bien fría.", BasePrice = 2200m, OfferPrice = null, DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new Product { Id = 11, CompanyId = 1, CategoryId = 5, Name = "Guaro Sour", ShortDescription = "Guaro Cacique, limón, azúcar y hielo.", BasePrice = 3500m, OfferPrice = null, DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed }
            );
        }

        private static void SeedMasterFooterLinks(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            b.Entity<FooterLink>().HasData(
                new FooterLink { Id = 1, CompanyId = 1, Label = "Instagram", Url = "https://instagram.com/digimenu", StandardIconId = 2, DisplayOrder = 1, IsVisible = true, IsDeleted = false, CreatedAt = seed },
                new FooterLink { Id = 2, CompanyId = 1, Label = "WhatsApp", Url = "https://wa.me/50612345678", StandardIconId = 3, DisplayOrder = 2, IsVisible = true, IsDeleted = false, CreatedAt = seed }
            );
        }

        /// <summary>
        /// La empresa maestra tiene todos los módulos activos para servir como demo completa.
        /// </summary>
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

        // ── Auditoría Automática ──────────────────────────────────────────────────

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