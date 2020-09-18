using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VTOLVR_ModLoader.Classes;

namespace VTOLVR_ModLoader.Views
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    public partial class Manager : UserControl
    {
        private List<Item> _mods = new List<Item>();
        private List<Item> _skins = new List<Item>();
        public Manager()
        {
            InitializeComponent();
        }
        public void UpdateUI()
        {
            FindMods(ref _mods);
            FindSkins(ref _skins);

            modsList.ItemsSource = _mods.ToArray();
        }
        private void FindMods(ref List<Item> items)
        {
            if (items == null)
                items = new List<Item>();

            List<BaseItem> downloadedMods = Helper.FindDownloadMods();
            for (int i = 0; i < downloadedMods.Count; i++)
            {
                items.Add(new Item(
                    downloadedMods[i].Name,
                    Visibility.Visible,
                    downloadedMods[i].Json[ProjectManager.jVersion] == null ? "N/A" : downloadedMods[i].Json[ProjectManager.jVersion].ToString(),
                    "a",
                    false,
                    false,
                    downloadedMods[i].Directory.FullName));
            }
        }
        private void FindSkins(ref List<Item> items)
        {
            if (items == null)
                items = new List<Item>();

            List<BaseItem> downloadSkins = Helper.FindDownloadedSkins();
            for (int i = 0; i < downloadSkins.Count; i++)
            {
                items.Add(new Item(
                    downloadSkins[i].Name,
                    Visibility.Hidden,
                    downloadSkins[i].Json[ProjectManager.jVersion] == null ? "N/A" : downloadSkins[i].Json[ProjectManager.jVersion].ToString(),
                    "a",
                    false,
                    false,
                    downloadSkins[i].Directory.FullName));
            }

        }

        public class Item
        {
            public string Name { get; set; }
            public Visibility UpdateVisibility { get; set; }
            public string CurrentVersion { get; set; }
            public string WebsiteVersion { get; set; }
            public bool LoadOnStartCheck { get; set; }
            public bool AutoUpdateCheck { get; set; }
            public string FolderDirectory { get; set; }

            public Item(string name, Visibility updateVisibility, string currentVersion, string websiteVersion, bool loadOnStartCheck, bool autoUpdateCheck, string folderDirectory)
            {
                Name = name;
                UpdateVisibility = updateVisibility;
                CurrentVersion = currentVersion;
                WebsiteVersion = websiteVersion;
                LoadOnStartCheck = loadOnStartCheck;
                AutoUpdateCheck = autoUpdateCheck;
                FolderDirectory = folderDirectory;
            }
        }
    }
}
