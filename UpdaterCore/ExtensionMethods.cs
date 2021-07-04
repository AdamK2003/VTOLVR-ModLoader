using System.Text;

namespace UpdaterCore
{
    public static class ExtensionMethods
    {
        public static string RemoveSpecialCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' ||
                    c == '_' || c == ' ')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static string RemoveSpaces(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c != ' ')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}