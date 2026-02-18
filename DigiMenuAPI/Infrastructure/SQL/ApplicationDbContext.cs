using DigiMenuAPI.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DigiMenuAPI.Infrastructure.SQL
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // --- TABLAS ---
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

            // 1. Configuración de Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
                entity.Property(p => p.BasePrice).HasPrecision(18, 2);
                entity.Property(p => p.OfferPrice).HasPrecision(18, 2);

                // Filtro Global: Ignora eliminados
                entity.HasQueryFilter(p => !p.IsDeleted);

                // Relación N:N con Tags
                entity.HasMany(p => p.Tags)
                      .WithMany(t => t.Products)
                      .UsingEntity(j => j.ToTable("ProductTags"));
            });

            // 2. Configuración de Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // 3. Configuración de FooterLinks
            modelBuilder.Entity<FooterLink>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.HasQueryFilter(f => !f.IsDeleted);

                entity.HasOne(f => f.StandardIcon)
                      .WithMany()
                      .HasForeignKey(f => f.StandardIconId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 4. Configuración de Settings
            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.PrimaryColor).HasMaxLength(7);
                entity.Property(s => s.ProductDisplay).HasDefaultValue(1);
            });

            // 5. Configuración de Tags
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.HasQueryFilter(t => !t.IsDeleted);
            });

            // --- DATA SEEDING ---
            modelBuilder.Entity<StandardIcon>().HasData(
                new StandardIcon
                {
                    Id = 1,
                    Name = "Facebook",
                    SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z'></path></svg>"
                },
                new StandardIcon
                {
                    Id = 2,
                    Name = "Instagram",
                    SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><rect x='2' y='2' width='20' height='20' rx='5' ry='5'></rect><path d='M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z'></path><line x1='17.5' y1='6.5' x2='17.51' y2='6.5'></line></svg>"
                },
                new StandardIcon
                {
                    Id = 3,
                    Name = "WhatsApp",
                    SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 1 1-7.6-14h.1c4.3 0 7.9 3.5 8.4 7.7z'></path><path d='M17 16l-4-4 4-4'></path></svg>"
                },
                new StandardIcon
                {
                    Id = 4,
                    Name = "TikTok",
                    SvgContent = "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'><path d='M9 12a4 4 0 1 0 4 4V4a5 5 0 0 0 5 5'></path></svg>"
                }
            );

            modelBuilder.Entity<Setting>().HasData(
                new Setting
                {
                    Id = 1,
                    BusinessName = "DigiMenu Store",
                    PrimaryColor = "#E63946",
                    SecondaryColor = "#457B9D",
                    BackgroundColor = "#F1FAEE",
                    TextColor = "#1D3557",
                    ShowProductDetails = true,
                    ProductDisplay = 1
                }
            );

            modelBuilder.Entity<Tag>().HasData(
                new Tag { Id = 1, Name = "Vegan", IsDeleted = false },
                new Tag { Id = 2, Name = "Spicy", IsDeleted = false },
                new Tag { Id = 3, Name = "Alcoholic", IsDeleted = false }
            );
        }

        // --- AUDITORÍA AUTOMÁTICA ---

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