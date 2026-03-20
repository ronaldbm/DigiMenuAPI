using AppCore.Application.Common;
using AppCore.Domain.Entities;
using AppCore.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;

namespace AppCore.Infrastructure.SQL
{
    public abstract class CoreDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        protected CoreDbContext(
            DbContextOptions options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

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

        // Catálogo de idiomas soportados por la plataforma (gestionado por SuperAdmin)
        public DbSet<SupportedLanguage> SupportedLanguages { get; set; }

        // Company config
        public DbSet<CompanyInfo> CompanyInfos { get; set; }
        public DbSet<CompanyTheme> CompanyThemes { get; set; }
        public DbSet<CompanySeo> CompanySeos { get; set; }
        public DbSet<CompanyLanguage> CompanyLanguages { get; set; }

        // Branch config
        public DbSet<BranchLocale> BranchLocales { get; set; }
        public DbSet<BranchSchedule> BranchSchedules { get; set; }
        public DbSet<BranchSpecialDay> BranchSpecialDays { get; set; }
        public DbSet<BranchEvent> BranchEvents { get; set; }

        // Emails
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<OutboxEmail> OutboxEmails { get; set; }
        public DbSet<OutboxEmailBody> OutboxEmailBodies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigurePlan(modelBuilder);
            ConfigureCompany(modelBuilder);
            ConfigureBranch(modelBuilder);
            ConfigureAppUser(modelBuilder);
            ConfigurePlatformModule(modelBuilder);
            ConfigureCompanyModule(modelBuilder);
            ConfigureOutboxEmail(modelBuilder);
            ConfigurePasswordResetRequest(modelBuilder);
            ConfigureCompanyInfo(modelBuilder);
            ConfigureCompanyTheme(modelBuilder);
            ConfigureCompanySeo(modelBuilder);
            ConfigureBranchLocale(modelBuilder);
            ConfigureBranchSchedule(modelBuilder);
            ConfigureBranchSpecialDay(modelBuilder);
            ConfigureBranchEvent(modelBuilder);
            ConfigureSupportedLanguage(modelBuilder);
            ConfigureCompanyLanguage(modelBuilder);

            SeedCoreData(modelBuilder);
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

                // Slug único global para identificar la empresa en el panel admin
                e.Property(c => c.Slug).IsRequired().HasMaxLength(60);
                e.HasIndex(c => c.Slug).IsUnique();

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
                e.Property(br => br.Location).HasColumnType("geography");

                // Slug único DENTRO de la Company — dos empresas distintas pueden
                // tener branches con el mismo slug sin conflicto.
                // La URL pública usa {company.Slug}.digimenu.cr/{branch.Slug}
                e.HasIndex(br => new { br.CompanyId, br.Slug }).IsUnique();
                e.HasIndex(br => new { br.CompanyId, br.IsDeleted });

                // RESTRICT: todos los borrados son lógicos (IsDeleted = true / IsActive = false).
                e.HasOne(br => br.Company)
                 .WithMany(c => c.Branches)
                 .HasForeignKey(br => br.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Relación 1:1 con BranchLocale (configuración regional por sucursal).
                e.HasOne(br => br.Locale)
                 .WithOne(l => l.Branch)
                 .HasForeignKey<BranchLocale>(l => l.BranchId)
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

        private static void ConfigurePasswordResetRequest(ModelBuilder b)
        {
            b.Entity<PasswordResetRequest>(e =>
            {
                e.HasKey(x => x.Id);

                // Búsqueda rápida por token al validar
                e.HasIndex(x => x.Token).IsUnique();

                // Tokens activos por usuario
                e.HasIndex(x => new { x.UserId, x.IsUsed, x.ExpiresAt });

                e.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Company)
                 .WithMany()
                 .HasForeignKey(x => x.CompanyId)
                 .OnDelete(DeleteBehavior.NoAction);

            });
        }

        private static void ConfigureOutboxEmail(ModelBuilder b)
        {
            b.Entity<OutboxEmail>(e =>
            {
                e.HasKey(x => x.Id);

                // Status y EmailType como tinyint en BD
                e.Property(x => x.Status)
                 .HasConversion<byte>();

                e.Property(x => x.EmailType)
                 .HasConversion<byte>();

                // Índice principal del processor — solo Pending(0) y Failed(2)
                e.HasIndex(x => new { x.Status, x.NextRetryAt })
                 .HasFilter("[Status] IN (0, 2)");

                // Índice para panel admin — por empresa, tipo y estado
                e.HasIndex(x => new { x.CompanyId, x.EmailType, x.Status });

                // Sin QueryFilter global — el processor procesa todos los tenants
                e.HasOne(x => x.Company)
                 .WithMany()
                 .HasForeignKey(x => x.CompanyId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Branch)
                 .WithMany()
                 .HasForeignKey(x => x.BranchId)
                 .OnDelete(DeleteBehavior.NoAction);
            });

            b.Entity<OutboxEmailBody>(e =>
            {
                // Shared primary key
                e.HasKey(x => x.OutboxEmailId);

                e.HasOne(x => x.OutboxEmail)
                 .WithOne(x => x.Body)
                 .HasForeignKey<OutboxEmailBody>(x => x.OutboxEmailId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Sin límite — el HTML puede ser extenso
                e.Property(x => x.HtmlBody)
                 .HasColumnType("nvarchar(max)");
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

        private static void ConfigureCompanyInfo(ModelBuilder b)
        {
            b.Entity<CompanyInfo>(e =>
            {
                e.HasKey(i => i.Id);
                e.Property(i => i.BusinessName).IsRequired().HasMaxLength(100);
                e.Property(i => i.Tagline).HasMaxLength(200);

                // 1:1 con Company
                e.HasIndex(i => i.CompanyId).IsUnique();
                e.HasOne(i => i.Company)
                 .WithOne(c => c.Info)
                 .HasForeignKey<CompanyInfo>(i => i.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCompanyTheme(ModelBuilder b)
        {
            b.Entity<CompanyTheme>(e =>
            {
                e.HasKey(t => t.Id);

                // Valores por defecto de colores
                e.Property(t => t.PageBackgroundColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(t => t.HeaderBackgroundColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(t => t.HeaderTextColor).HasMaxLength(7).HasDefaultValue("#000000");
                e.Property(t => t.TabBackgroundColor).HasMaxLength(7).HasDefaultValue("#000000");
                e.Property(t => t.TabTextColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(t => t.PrimaryColor).HasMaxLength(7).HasDefaultValue("#E63946");
                e.Property(t => t.PrimaryTextColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(t => t.SecondaryColor).HasMaxLength(7).HasDefaultValue("#457B9D");
                e.Property(t => t.TitlesColor).HasMaxLength(7).HasDefaultValue("#000000");
                e.Property(t => t.TextColor).HasMaxLength(7).HasDefaultValue("#1D3557");
                e.Property(t => t.BrowserThemeColor).HasMaxLength(7).HasDefaultValue("#FFFFFF");
                e.Property(t => t.HeaderStyle).HasColumnType("tinyint").HasDefaultValue((byte)1);
                e.Property(t => t.MenuLayout).HasColumnType("tinyint").HasDefaultValue((byte)1);
                e.Property(t => t.ProductDisplay).HasColumnType("tinyint").HasDefaultValue((byte)1);
                e.Property(t => t.FilterMode).HasColumnType("tinyint").HasDefaultValue((byte)0);
                e.Property(t => t.ShowMapInMenu).HasDefaultValue(true);

                // 1:1 con Company
                e.HasIndex(t => t.CompanyId).IsUnique();
                e.HasOne(t => t.Company)
                 .WithOne(c => c.Theme)
                 .HasForeignKey<CompanyTheme>(t => t.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBranchLocale(ModelBuilder b)
        {
            b.Entity<BranchLocale>(e =>
            {
                e.HasKey(l => l.Id);
                e.Property(l => l.CountryCode).IsRequired().HasMaxLength(3);
                e.Property(l => l.PhoneCode).IsRequired().HasMaxLength(6);
                e.Property(l => l.Currency).IsRequired().HasMaxLength(5);
                e.Property(l => l.CurrencyLocale).IsRequired().HasMaxLength(10);
                e.Property(l => l.Language).IsRequired().HasMaxLength(5);
                e.Property(l => l.TimeZone).IsRequired().HasMaxLength(50);

                // 1:1 con Branch
                e.HasIndex(l => l.BranchId).IsUnique();
                e.HasOne(l => l.Branch)
                 .WithOne(br => br.Locale)
                 .HasForeignKey<BranchLocale>(l => l.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCompanySeo(ModelBuilder b)
        {
            b.Entity<CompanySeo>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.MetaTitle).HasMaxLength(100);
                e.Property(s => s.MetaDescription).HasMaxLength(300);
                e.Property(s => s.GoogleAnalyticsId).HasMaxLength(50);
                e.Property(s => s.FacebookPixelId).HasMaxLength(50);

                // 1:1 con Company
                e.HasIndex(s => s.CompanyId).IsUnique();
                e.HasOne(s => s.Company)
                 .WithOne(c => c.Seo)
                 .HasForeignKey<CompanySeo>(s => s.CompanyId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBranchSchedule(ModelBuilder b)
        {
            b.Entity<BranchSchedule>(e =>
            {
                e.HasKey(x => x.Id);

                // Un solo registro por día por Branch — garantía de integridad
                e.HasIndex(x => new { x.BranchId, x.DayOfWeek }).IsUnique();

                e.Property(x => x.DayOfWeek).HasColumnType("tinyint");

                // Cascade: los horarios se eliminan físicamente si se elimina la Branch
                // BranchSchedule no tiene soft delete propio
                e.HasOne(x => x.Branch)
                 .WithMany(br => br.Schedules)
                 .HasForeignKey(x => x.BranchId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureBranchSpecialDay(ModelBuilder b)
        {
            b.Entity<BranchSpecialDay>(e =>
            {
                e.HasKey(x => x.Id);

                // Una Branch no puede tener dos registros para la misma fecha
                e.HasIndex(x => new { x.BranchId, x.Date }).IsUnique();

                // Índice para la consulta de reservas y menú público por fecha
                e.HasIndex(x => new { x.BranchId, x.Date });

                e.Property(x => x.Date).HasColumnType("date");
                e.Property(x => x.Reason).IsRequired().HasMaxLength(200);

                // Cascade: los días especiales se eliminan físicamente con la Branch
                e.HasOne(x => x.Branch)
                 .WithMany(br => br.SpecialDays)
                 .HasForeignKey(x => x.BranchId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureSupportedLanguage(ModelBuilder b)
        {
            b.Entity<SupportedLanguage>(e =>
            {
                e.HasKey(l => l.Code);
                e.Property(l => l.Code).HasMaxLength(5);
                e.Property(l => l.Name).IsRequired().HasMaxLength(50);
                e.Property(l => l.Flag).IsRequired().HasMaxLength(10);
            });
        }

        private static void ConfigureCompanyLanguage(ModelBuilder b)
        {
            b.Entity<CompanyLanguage>(e =>
            {
                e.HasKey(cl => cl.Id);

                // Una empresa no puede tener el mismo idioma dos veces
                e.HasIndex(cl => new { cl.CompanyId, cl.LanguageCode }).IsUnique();

                // Solo un idioma puede ser default por empresa
                // (se valida en servicio, no en BD para evitar complicaciones de constraint)

                e.HasOne(cl => cl.Company)
                 .WithMany(c => c.Languages)
                 .HasForeignKey(cl => cl.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(cl => cl.Language)
                 .WithMany(l => l.CompanyLanguages)
                 .HasForeignKey(cl => cl.LanguageCode)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureBranchEvent(ModelBuilder b)
        {
            b.Entity<BranchEvent>(e =>
            {
                e.HasKey(x => x.Id);

                // Índice para listar eventos por fecha en una Branch
                e.HasIndex(x => new { x.BranchId, x.EventDate });

                // Índice para el modal promocional: solo eventos activos con modal
                e.HasIndex(x => new { x.BranchId, x.ShowPromotionalModal, x.IsActive });

                e.Property(x => x.EventDate).HasColumnType("date");
                e.Property(x => x.Title).IsRequired().HasMaxLength(200);
                e.Property(x => x.Description).HasMaxLength(1000);
                e.Property(x => x.FlyerImageUrl).HasMaxLength(500);

                // Cascade: los eventos se eliminan físicamente si se elimina la Branch
                e.HasOne(x => x.Branch)
                 .WithMany(br => br.Events)
                 .HasForeignKey(x => x.BranchId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ── Data Seeding ─────────────────────────────────────────────

        private static void SeedCoreData(ModelBuilder b)
        {
            SeedPlans(b);
            SeedStandardIcons(b);
            SeedPlatformModules(b);
            SeedSupportedLanguages(b);
            SeedMasterCompany(b);
            SeedMasterBranch(b);
            SeedMasterUser(b);
            SeedMasterCompanyConfig(b);
            SeedMasterCompanyModules(b);
            SeedMasterBranchSchedule(b);
            SeedMasterCompanyLanguages(b);
        }

        private static void SeedSupportedLanguages(ModelBuilder b)
        {
            // Idiomas soportados por la plataforma.
            // Para agregar un nuevo idioma en el futuro: insertar un registro aquí
            // y crear una nueva migración. No requiere cambios en código.
            b.Entity<SupportedLanguage>().HasData(
                new SupportedLanguage { Code = "es", Name = "Español",   Flag = "🇪🇸", DisplayOrder = 1, IsActive = true },
                new SupportedLanguage { Code = "en", Name = "English",   Flag = "🇺🇸", DisplayOrder = 2, IsActive = true },
                new SupportedLanguage { Code = "pt", Name = "Português", Flag = "🇧🇷", DisplayOrder = 3, IsActive = true },
                new SupportedLanguage { Code = "fr", Name = "Français",  Flag = "🇫🇷", DisplayOrder = 4, IsActive = true }
            );
        }

        private static void SeedMasterCompanyLanguages(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // La empresa demo trabaja con Español por defecto
            b.Entity<CompanyLanguage>().HasData(new CompanyLanguage
            {
                Id           = 1,
                CompanyId    = 1,
                LanguageCode = "es",
                IsDefault    = true,
                CreatedAt    = seed
            });
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
                new StandardIcon { Id = 3, Name = "WhatsApp",  SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.2 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.2-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z'/></svg>" },
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
                Slug        = "digimenu-platform",
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
                Slug      = "Principal",
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
                PasswordHash = "$2y$12$tJGKbEhmd00CrIaQU8yf0eKU.doWOliVml/J48.NCwhXlF./.ZZgS",
                Role         = UserRoles.SuperAdmin,
                IsActive     = true,
                IsDeleted    = false,
                CreatedAt    = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }

        private static void SeedMasterCompanyConfig(ModelBuilder b)
        {
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            b.Entity<CompanyInfo>().HasData(new CompanyInfo
            {
                Id = 1,
                CompanyId = 1,
                BusinessName = "DigiMenu Demo",
                Tagline = "El mejor menú digital para tu restaurante",
                LogoUrl = null,
                FaviconUrl = null,
                BackgroundImageUrl = null,
                CreatedAt = seed
            });

            b.Entity<CompanyTheme>().HasData(new CompanyTheme
            {
                Id = 1,
                CompanyId = 1,
                IsDarkMode = false,
                PageBackgroundColor   = DefaultTheme.PageBackground,
                HeaderBackgroundColor = DefaultTheme.HeaderBackground,
                HeaderTextColor       = DefaultTheme.HeaderText,
                TabBackgroundColor    = DefaultTheme.TabBackground,
                TabTextColor          = DefaultTheme.TabText,
                PrimaryColor          = DefaultTheme.Primary,
                PrimaryTextColor      = DefaultTheme.PrimaryText,
                SecondaryColor        = DefaultTheme.Secondary,
                TitlesColor           = DefaultTheme.Titles,
                TextColor             = DefaultTheme.Text,
                BrowserThemeColor     = DefaultTheme.BrowserTheme,
                HeaderStyle = 1,
                MenuLayout = 1,
                ProductDisplay = 1,
                ShowProductDetails = true,
                FilterMode = 0,
                ShowContactButton = true,
                ShowModalProductInfo = false,
                ShowMapInMenu = true,
                CreatedAt = seed
            });

            b.Entity<BranchLocale>().HasData(new BranchLocale
            {
                Id = 1,
                BranchId = 1,
                CountryCode = "CR",
                PhoneCode = "+506",
                Currency = "CRC",
                CurrencyLocale = "es-CR",
                Language = "es",
                TimeZone = "America/Costa_Rica",
                Decimals = 0,
                CreatedAt = seed
            });

            b.Entity<CompanySeo>().HasData(new CompanySeo
            {
                Id = 1,
                CompanyId = 1,
                MetaTitle = "DigiMenu Demo",
                MetaDescription = "El mejor menú digital para tu restaurante",
                GoogleAnalyticsId = null,
                FacebookPixelId = null,
                CreatedAt = seed
            });
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

        private static void SeedMasterBranchSchedule(ModelBuilder b)
        {
            // Horario semanal de la Branch demo (BranchId = 1).
            // Convención .NET DayOfWeek: 0=Domingo, 1=Lunes … 6=Sábado
            // Lun-Sáb → abierto 09:00–22:00 | Dom → cerrado
            var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var open  = new TimeSpan(9,  0, 0);
            var close = new TimeSpan(22, 0, 0);

            b.Entity<BranchSchedule>().HasData(
                new BranchSchedule { Id = 1, BranchId = 1, DayOfWeek = 0, IsOpen = false, OpenTime = null,  CloseTime = null,  CreatedAt = seed }, // Domingo
                new BranchSchedule { Id = 2, BranchId = 1, DayOfWeek = 1, IsOpen = true,  OpenTime = open,  CloseTime = close, CreatedAt = seed }, // Lunes
                new BranchSchedule { Id = 3, BranchId = 1, DayOfWeek = 2, IsOpen = true,  OpenTime = open,  CloseTime = close, CreatedAt = seed }, // Martes
                new BranchSchedule { Id = 4, BranchId = 1, DayOfWeek = 3, IsOpen = true,  OpenTime = open,  CloseTime = close, CreatedAt = seed }, // Miércoles
                new BranchSchedule { Id = 5, BranchId = 1, DayOfWeek = 4, IsOpen = true,  OpenTime = open,  CloseTime = close, CreatedAt = seed }, // Jueves
                new BranchSchedule { Id = 6, BranchId = 1, DayOfWeek = 5, IsOpen = true,  OpenTime = open,  CloseTime = close, CreatedAt = seed }, // Viernes
                new BranchSchedule { Id = 7, BranchId = 1, DayOfWeek = 6, IsOpen = true,  OpenTime = open,  CloseTime = close, CreatedAt = seed }  // Sábado
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
            // Leer userId del JWT una sola vez para todas las entidades del batch
            var userId = ResolveCurrentUserId();
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedUserId = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedUserId = userId;

                    // Proteger campos de creación — nunca se sobreescriben
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedUserId).IsModified = false;
                }
            }
        }

        /// <summary>
        /// Extrae el UserId del JWT del HttpContext actual.
        /// Devuelve null en:
        ///   - Contextos sin request HTTP (migraciones, seed, background jobs)
        ///   - Endpoints públicos sin JWT (menú público, reservas anónimas)
        ///   - JWT sin claim "userId"
        /// </summary>
        private int? ResolveCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user is null || user.Identity?.IsAuthenticated != true)
                return null;

            var claim = user.FindFirstValue("userId");

            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                return null;

            return userId;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }
    }
}
