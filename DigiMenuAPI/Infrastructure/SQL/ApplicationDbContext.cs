// DigiMenuAPI/Infrastructure/SQL/ApplicationDbContext.cs
// Versión corregida — incluye AppUser correctamente configurado

using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace DigiMenuAPI.Infrastructure.SQL
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantService? _tenantService;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantService? tenantService = null)
            : base(options)
        {
            _tenantService = tenantService;
        }

        // ── DbSets ──────────────────────────────────────────────────
        public DbSet<Company> Companies { get; set; }
        public DbSet<AppUser> Users { get; set; }   // ← Estaba ausente
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<StandardIcon> StandardIcons { get; set; }
        public DbSet<FooterLink> FooterLinks { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<PlatformModule> PlatformModules { get; set; }
        public DbSet<CompanyModule> CompanyModules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── COMPANY ─────────────────────────────────────────────
            modelBuilder.Entity<Company>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);
                e.Property(c => c.Slug).IsRequired().HasMaxLength(60);
                e.HasIndex(c => c.Slug).IsUnique();
                e.Property(c => c.Email).IsRequired().HasMaxLength(150);
                e.Property(c => c.Phone).HasMaxLength(20);
                e.Property(c => c.CountryCode).HasMaxLength(3);
            });

            // ── APPUSER ─────────────────────────────────────────────
            modelBuilder.Entity<AppUser>(e =>
            {
                e.HasKey(u => u.Id);
                e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
                e.Property(u => u.Email).IsRequired().HasMaxLength(150);
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.PasswordHash).IsRequired();

                e.HasOne(u => u.Company)
                 .WithMany(c => c.Users)
                 .HasForeignKey(u => u.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── SETTING ─────────────────────────────────────────────
            modelBuilder.Entity<Setting>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.BusinessName).IsRequired().HasMaxLength(100);
                e.Property(s => s.Tagline).HasMaxLength(200);
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
                e.Property(s => s.ProductDisplay).HasDefaultValue((byte)1);
                e.Property(s => s.HeaderStyle).HasDefaultValue((byte)1);
                e.Property(s => s.MenuLayout).HasDefaultValue((byte)1);
                e.Property(s => s.Decimals).HasDefaultValue((byte)2);
                e.Property(s => s.CountryCode).HasMaxLength(3);
                e.Property(s => s.PhoneCode).HasMaxLength(6);
                e.Property(s => s.Currency).HasMaxLength(5);
                e.Property(s => s.CurrencyLocale).HasMaxLength(10);
                e.Property(s => s.Language).HasMaxLength(5);
                e.Property(s => s.TimeZone).HasMaxLength(50);
                e.Property(s => s.MetaTitle).HasMaxLength(100);
                e.Property(s => s.MetaDescription).HasMaxLength(300);
                e.Property(s => s.GoogleAnalyticsId).HasMaxLength(50);
                e.Property(s => s.FacebookPixelId).HasMaxLength(50);

                // 1:1 con Company
                e.HasOne(s => s.Company)
                 .WithOne(c => c.Setting)
                 .HasForeignKey<Setting>(s => s.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(s => s.CompanyId).IsUnique();
            });

            // ── CATEGORY ────────────────────────────────────────────
            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);
                e.HasQueryFilter(c => !c.IsDeleted && c.CompanyId == GetCurrentCompanyId());
                e.HasOne(c => c.Company)
                 .WithMany(co => co.Categories)
                 .HasForeignKey(c => c.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(c => new { c.CompanyId, c.IsDeleted, c.DisplayOrder });
            });

            // ── PRODUCT ─────────────────────────────────────────────
            modelBuilder.Entity<Product>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).IsRequired().HasMaxLength(150);
                e.Property(p => p.BasePrice).HasPrecision(18, 2);
                e.Property(p => p.OfferPrice).HasPrecision(18, 2);
                e.HasQueryFilter(p => !p.IsDeleted && p.CompanyId == GetCurrentCompanyId());
                e.HasMany(p => p.Tags)
                 .WithMany(t => t.Products)
                 .UsingEntity(j => j.ToTable("ProductTags"));
                e.HasOne(p => p.Company)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(p => new { p.CompanyId, p.IsDeleted, p.DisplayOrder });
            });

            // ── TAG ─────────────────────────────────────────────────
            modelBuilder.Entity<Tag>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Name).IsRequired().HasMaxLength(50);
                e.Property(t => t.Color).HasMaxLength(7).HasDefaultValue("#ffffff");
                e.HasQueryFilter(t => !t.IsDeleted && t.CompanyId == GetCurrentCompanyId());
                e.HasOne(t => t.Company)
                 .WithMany(c => c.Tags)
                 .HasForeignKey(t => t.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(t => new { t.CompanyId, t.IsDeleted });
            });

            // ── FOOTERLINK ──────────────────────────────────────────
            modelBuilder.Entity<FooterLink>(e =>
            {
                e.HasKey(f => f.Id);
                e.HasQueryFilter(f => !f.IsDeleted && f.CompanyId == GetCurrentCompanyId());
                e.HasOne(f => f.StandardIcon)
                 .WithMany()
                 .HasForeignKey(f => f.StandardIconId)
                 .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(f => f.Company)
                 .WithMany(c => c.FooterLinks)
                 .HasForeignKey(f => f.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(f => new { f.CompanyId, f.IsDeleted, f.DisplayOrder });
            });

            // ── RESERVATION ─────────────────────────────────────────
            modelBuilder.Entity<Reservation>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.CustomerName).IsRequired().HasMaxLength(100);
                e.Property(r => r.Phone).IsRequired().HasMaxLength(20);
                e.Property(r => r.TableNumber).HasMaxLength(20);
                e.Property(r => r.Allergies).HasMaxLength(500);
                e.Property(r => r.Comments).HasMaxLength(500);
                e.HasQueryFilter(r => !r.IsDeleted && r.CompanyId == GetCurrentCompanyId());
                e.HasOne(r => r.Company)
                 .WithMany(c => c.Reservations)
                 .HasForeignKey(r => r.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(r => new { r.CompanyId, r.IsDeleted, r.ReservationDate });
            });

            // ── STANDARDICON (global, sin tenant) ───────────────────
            modelBuilder.Entity<StandardIcon>(e =>
            {
                e.HasKey(s => s.Id);
                e.Property(s => s.Name).IsRequired().HasMaxLength(50);
                e.Property(s => s.SvgContent).IsRequired();
            });

            // ── PLATFORMMODULE ──────────────────────────────────────
            modelBuilder.Entity<PlatformModule>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Code).IsRequired().HasMaxLength(50);
                e.HasIndex(m => m.Code).IsUnique();
                e.Property(m => m.Name).IsRequired().HasMaxLength(100);
                e.Property(m => m.Description).HasMaxLength(300);
            });

            // ── COMPANYMODULE ───────────────────────────────────────
            modelBuilder.Entity<CompanyModule>(e =>
            {
                e.HasKey(cm => cm.Id);
                e.HasIndex(cm => new { cm.CompanyId, cm.PlatformModuleId }).IsUnique();
                e.HasOne(cm => cm.Company)
                 .WithMany(c => c.Modules)
                 .HasForeignKey(cm => cm.CompanyId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(cm => cm.PlatformModule)
                 .WithMany(m => m.CompanyModules)
                 .HasForeignKey(cm => cm.PlatformModuleId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(cm => new { cm.CompanyId, cm.IsActive });
            });

            SeedData(modelBuilder);
        }

        private int GetCurrentCompanyId()
            => _tenantService?.TryGetCompanyId() ?? 0;

        // ── AUDITORÍA ────────────────────────────────────────────────
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
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                    entry.Property(x => x.CreatedAt).IsModified = false;
                }
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StandardIcon>().HasData(
                new StandardIcon { Id = 1, Name = "Facebook", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z'></path></svg>" },
                new StandardIcon { Id = 2, Name = "Instagram", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='2' y='2' width='20' height='20' rx='5' ry='5'></rect><path d='M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z'></path><line x1='17.5' y1='6.5' x2='17.51' y2='6.5'></line></svg>" },
                new StandardIcon { Id = 3, Name = "WhatsApp", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 1 1-7.6-14h.1c4.3 0 7.9 3.5 8.4 7.7z'></path><path d='M17 16l-4-4 4-4'></path></svg>" },
                new StandardIcon { Id = 4, Name = "TikTok", SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M9 12a4 4 0 1 0 4 4V4a5 5 0 0 0 5 5'></path></svg>" }
            );

            modelBuilder.Entity<PlatformModule>().HasData(
                new PlatformModule { Id = 1, Code = "RESERVATIONS", Name = "Reservas", Description = "Gestión de reservas de mesas y eventos.", IsActive = true, DisplayOrder = 1 },
                new PlatformModule { Id = 2, Code = "TABLE_MANAGEMENT", Name = "Gestión de Mesas", Description = "Vista de plano de mesas en tiempo real.", IsActive = true, DisplayOrder = 2 },
                new PlatformModule { Id = 3, Code = "ANALYTICS", Name = "Analytics Avanzados", Description = "Reportes de visitas, productos más vistos y conversiones.", IsActive = true, DisplayOrder = 3 },
                new PlatformModule { Id = 4, Code = "ONLINE_ORDERS", Name = "Pedidos en Línea", Description = "Delivery y take away con integración de pagos.", IsActive = true, DisplayOrder = 4 }
            );
        }
    }
}