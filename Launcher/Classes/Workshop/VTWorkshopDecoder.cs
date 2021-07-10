using System.Collections.Generic;
using System.IO;
using System.Text;
using Launcher.Views;

namespace Launcher.Classes.Workshop
{
    static class VTWorkshopDecoder
    {
        public static List<WorkshopItem> WorkshopItems = new List<WorkshopItem>();
        public static List<WorkshopItem> GetWorkshopScenarios()
        {
            Console.Log("Getting workshop scenarios");
            DirectoryInfo workshop = GetWorkshopFolder();
            if (workshop == null)
                return null;

            DirectoryInfo[] workshopItems = workshop.GetDirectories();

            for (int i = 0; i < workshopItems.Length; i++)
            {
                DecodeItem(workshopItems[i]);
            }
            return WorkshopItems;
        }

        private static DirectoryInfo GetWorkshopFolder()
        {
            DirectoryInfo root = new DirectoryInfo(Program.Root);
            DirectoryInfo steamapps = root.Parent.Parent.Parent;

            if (steamapps.Name != "steamapps")
            {
                Console.Log("Folder miss match on steamapps. " + steamapps.Name);
                return null;
            }

            DirectoryInfo workshop = steamapps.GetFolder("workshop");
            if (workshop == null)
            {
                Console.Log("Folder miss match on workshop");
                return null;
            }

            DirectoryInfo content = workshop.GetFolder("content");
            if (content == null)
            {
                Console.Log("Folder miss match on content");
                return null;
            }

            DirectoryInfo vtol = content.GetFolder("667970");
            if (vtol == null)
            {
                Console.Log("Folder miss match on 667970");
                return null;
            }
            return vtol;
        }

        private static void DecodeItem(DirectoryInfo folder)
        {
            FileInfo[] files = folder.GetFiles("*.vtsb");

            foreach (FileInfo file in files)
            {
                using (Stream stream = File.Open(file.FullName, FileMode.Open))
                {
                    int num = (int)stream.Length;
                    byte[] array = new byte[num];
                    int num2 = 0;
                    int data;
                    while ((data = stream.ReadByte()) != -1)
                    {
                        array[num2] = WorkshopDecode(data);
                        num2++;
                    }
                    string[] lines = Encoding.UTF8.GetString(array, 0, num).Split('\n');
                    if (IsCustomScenario(lines))
                    {
                        WorkshopItems.Add(new WorkshopItem(lines));
                    }

                }
            }
        }

        private static byte WorkshopDecode(int data)
        {
            return (byte)((data - 88) % 256);
        }

        private static bool IsCustomScenario(string[] lines)
        {
            return lines[0] == "CustomScenario";
        }
    }
}
