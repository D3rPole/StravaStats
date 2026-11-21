using MudBlazor;

namespace StravaStats.Helper
{
    public class MudValues
    {
        public static string GetPrimaryColorString()
        {
            var theme = new MudTheme();
            string primaryHex = theme.PaletteLight.Primary.ToString();
            return primaryHex;
        }

        public static string GetTextColorString()
        {
            var theme = new MudTheme();
            string primaryHex = theme.PaletteLight.White.ToString();
            return primaryHex;
        }

        public static string GetBlackColorString()
        {
            var theme = new MudTheme();
            string primaryHex = theme.PaletteLight.Black.ToString();
            return primaryHex;
        }
    }
}
