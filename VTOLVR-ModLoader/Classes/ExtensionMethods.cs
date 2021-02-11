using Valve.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Core.Jsons;

namespace VTOLVR_ModLoader.Classes
{
    public static class ExtensionMethods
    {
        public static string RemoveSpecialCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_' || c == ' ')
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
        public static BitmapImage LoadImage(this BitmapImage image, string path)
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            return image;
        }
        public static void FilloutForm(this BaseItem baseItem, ref HttpHelper form, bool isMod, string currentPath)
        {
            form.SetValue("version", baseItem.Version);
            form.SetValue("name", baseItem.Name);
            form.SetValue("tagline", baseItem.Tagline);
            form.SetValue("description", baseItem.Description);
            form.SetValue("unlisted", baseItem.Unlisted.ToString());
            form.SetValue("is_public", baseItem.IsPublic.ToString());
            if (isMod)
                form.SetValue("repository", baseItem.Source);

            form.AttachFile("header_image", baseItem.WebPreviewImage, Path.Combine(currentPath, baseItem.WebPreviewImage));
            form.AttachFile("thumbnail", baseItem.PreviewImage, Path.Combine(baseItem.Directory.FullName, baseItem.PreviewImage));
            form.SetValue("user_uploaded_file", string.Empty);

        }

        public static DirectoryInfo GetFolder(this DirectoryInfo directoryInfo, string folder)
        {
            DirectoryInfo[] folders = directoryInfo.GetDirectories();
            for (int i = 0; i < folders.Length; i++)
            {
                if (folders[i].Name == folder)
                    return folders[i];
            }
            return null;
        }
    }
}
