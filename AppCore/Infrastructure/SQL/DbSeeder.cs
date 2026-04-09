using AppCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppCore.Infrastructure.SQL
{
    /// <summary>
    /// Ejecuta seeds que no pueden hacerse via HasData (ej: entidades con owned types ToJson).
    /// Se llama una vez al iniciar la app, después de aplicar migraciones.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(CoreDbContext context)
        {
            await SeedDemoCompanyThemeAsync(context);
        }

        // CompanyTheme (Id=1, CompanyId=1) no puede seedearse con HasData porque tiene
        // propiedades owned mapeadas con ToJson() — limitación de EF Core 10.
        private static async Task SeedDemoCompanyThemeAsync(CoreDbContext context)
        {
            if (await context.CompanyThemes.AnyAsync(t => t.CompanyId == 1))
                return;

            context.CompanyThemes.Add(new CompanyTheme
            {
                CompanyId = 1,
                ColorPalette = new ColorPaletteData
                {
                    HeaderBackgroundColor = "#FFFFFF",
                    HeaderTextColor       = "#1D3557",
                    PageBackgroundColor   = "#F1FAEE",
                    TextColor             = "#1D3557",
                    CardBackgroundColor   = "#FFFFFF",
                    CardBorderColor       = "#0F0F0F0F",
                    TabBackgroundColor    = "#1D3557",
                    TabTextColor          = "#FFFFFF",
                    PrimaryColor          = "#E63946",
                    PrimaryTextColor      = "#FFFFFF",
                    SecondaryColor        = "#457B9D",
                    FooterBackgroundColor = "#FFFFFF",
                    FooterTextColor       = "#1D3557",
                    CategoryTitleColor    = "#000000",
                    CardTitleColor        = "#000000",
                    PriceColor            = "#000000",
                    PromotionColor        = "#E63946",
                    BrowserThemeColor     = "#FFFFFF"
                },
                BackgroundSettings   = new BackgroundSettingsData(),
                FrameSettings        = new FrameSettingsData(),
                IsDarkMode           = false,
                DarkModeAutoGenerate = true,
                HeaderStyle          = 1,
                MenuLayout           = 1,
                ProductDisplay       = 1,
                CategoryHeaderStyle  = 1,
                ShowProductDetails   = true,
                ShowCategoryImages   = true,
                FilterMode           = 0,
                ShowContactButton    = true,
                ShowModalProductInfo = false,
                ShowMapInMenu        = true,
                CreatedAt            = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            await context.SaveChangesAsync();
        }
    }
}
