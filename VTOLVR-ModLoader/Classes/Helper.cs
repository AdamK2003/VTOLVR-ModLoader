using System.Text.RegularExpressions;

namespace VTOLVR_ModLoader.Classes
{
    public static class Helper
    {
        public static string ClearSpaces(string input)
        {
            return Regex.Replace(input, @"\s+", "");
        }
    }
}