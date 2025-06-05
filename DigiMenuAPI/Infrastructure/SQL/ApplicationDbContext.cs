using DigiMenuAPI.Infrastructure.Entities;
using DigiMenuAPI.Infrastructure.Entities.Views;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Infrastructure.SQL
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
            
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //TABLAS

            // Configuración para la entidad Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id); 
                entity.Property(p => p.Label).IsRequired();
                entity.Property(p => p.Description);
                entity.Property(p => p.ImagePath); 
                entity.Property(p => p.Price).HasColumnType("float");
                entity.Property(p => p.Position).IsRequired();
                entity.Property(p => p.Alive).IsRequired();
                entity.Property(p => p.IsVisible).IsRequired();

                // Relación con Subcategory
                entity.HasOne(p => p.Subcategory)
                      .WithMany(s => s.Products)
                      .HasForeignKey(p => p.SubcategoryId)
                      .OnDelete(DeleteBehavior.Restrict); 
            });

            // Configuración para la entidad Subcategory
            modelBuilder.Entity<Subcategory>(entity =>
            {
                entity.HasKey(s => s.Id); 
                entity.Property(s => s.Label).IsRequired(); 
                entity.Property(s => s.Position).IsRequired();
                entity.Property(s => s.Alive).IsRequired();
                entity.Property(s => s.IsVisible).IsRequired();

                // Relación con Category
                entity.HasOne(s => s.Category) 
                      .WithMany(c => c.Subcategories)
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración para la entidad Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Label).IsRequired(); 
                entity.Property(c => c.Position).IsRequired();
                entity.Property(c => c.Alive).IsRequired(); 
                entity.Property(c => c.IsVisible).IsRequired();
            });

            // Configuración para la entidad SocialLink
            modelBuilder.Entity<SocialLink>(entity =>
            {
                entity.HasKey(sl => sl.Id); 
                entity.Property(sl => sl.Icon).IsRequired();
                entity.Property(sl => sl.Label).IsRequired();
                entity.Property(sl => sl.URL);
                entity.Property(sl => sl.IsVisible).IsRequired();
            });


            // VISTAS

            // Configuración de ProductVisibleList
            modelBuilder.Entity<vwProductVisibleList>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwProductVisibleList"); 
            });

            // Configuración de SubcategoryVisibleList
            modelBuilder.Entity<vwSubcategoryVisibleList>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwSubcategoryVisibleList"); 
            });

            // Configuración de CategoryVisibleList
            modelBuilder.Entity<vwCategoryVisibleList>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwCategoryVisibleList"); 
            });

            // Configuración de GetAllCategories
            modelBuilder.Entity<vwGetAllCategories>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwGetAllCategories");
            });

            // Configuración de GetAllSubcategories
            modelBuilder.Entity<vwGetAllSubcategories>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwGetAllSubcategories");
            });

            // Configuración de GetAllProducts
            modelBuilder.Entity<vwGetAllProducts>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vwGetAllProducts");
            });


        }

        //Tablas
        public DbSet<Product> Product { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Subcategory> Subcategory { get; set; }
        public DbSet<SocialLink> SocialLink { get; set; }

        //Vistas
        public DbSet<vwGetAllProducts> vwGetAllProducts { get; set; }
        public DbSet<vwProductVisibleList> vwProductVisibleLists { get; set; }
        public DbSet<vwGetAllSubcategories> vwGetAllSubcategories { get; set; }
        public DbSet<vwSubcategoryVisibleList> vwSubcategoryVisibleLists { get; set; }
        public DbSet<vwCategoryVisibleList> vwCategoryVisibleLists { get; set; }
        public DbSet<vwGetAllCategories> vwGetAllCategories { get; set; }
    }
}
