namespace DigiMenuAPI.Application.DTOs.Read
{
    public record CompanyThemeReadDto(
        int Id,
        int CompanyId,
        bool IsDarkMode,
        string PageBackgroundColor,
        string HeaderBackgroundColor,
        string HeaderTextColor,
        string TabBackgroundColor,
        string TabTextColor,
        string PrimaryColor,
        string PrimaryTextColor,
        string SecondaryColor,
        string TitlesColor,
        string TextColor,
        string BrowserThemeColor,
        byte HeaderStyle,
        byte MenuLayout,
        byte ProductDisplay,
        bool ShowProductDetails,
        bool ShowSearchButton,
        bool ShowContactButton
    );
}
