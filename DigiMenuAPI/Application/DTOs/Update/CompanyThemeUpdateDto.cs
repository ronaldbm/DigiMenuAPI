namespace DigiMenuAPI.Application.DTOs.Update
{
    public record CompanyThemeUpdateDto(
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
        byte FilterMode,
        bool ShowContactButton,
        bool ShowModalProductInfo
    );
}
