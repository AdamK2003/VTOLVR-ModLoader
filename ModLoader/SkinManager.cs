using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ModLoader
{
    public class SkinManager : VTOLMOD
    {
        //This variables are used on different scenes
        private List<Skin> installedSkins = new List<Skin>();
        private int selectedSkin = -1;

        //Vehicle Config scene only
        private int currentSkin;
        private Text scenarioName, scenarioDescription;
        private RawImage skinPreview;

        private static GameObject prefab;

        /// <summary>
        /// All the materials in the game
        /// </summary>
        private List<Mat> materials;
        /// <summary>
        /// The default textures so we can revert back
        /// </summary>
        private Dictionary<string, Texture> defaultTextures;
        private string[] matsNotToTouch = new string[] { "Font Material", "Font Material_0", "Font Material_1", "Font Material_2", "Font Material_3", "Font Material_4", "Font Material_5", "Font Material_6" };
        private struct Mat
        {
            public string name;
            public Material material;

            public Mat(string name, Material material)
            {
                this.name = name;
                this.material = material;
            }
        }
        private void Start()
        {
            Mod mod = new Mod();
            mod.name = "Skin Manger";
            SetModInfo(mod);
            VTOLAPI.SceneLoaded += SceneLoaded;
            Directory.CreateDirectory(ModLoaderManager.RootPath + @"\skins");
        }

        private IEnumerator GetDefaultTextures()
        {
            yield return new WaitForSeconds(0.5f);
            Log("Getting Default Textures");
            Material[] materials = Resources.FindObjectsOfTypeAll(typeof(Material)) as Material[];
            defaultTextures = new Dictionary<string, Texture>(materials.Length);


            for (int i = 0; i < materials.Length; i++)
            {
                if (!matsNotToTouch.Contains(materials[i].name) && !defaultTextures.ContainsKey(materials[i].name))
                    defaultTextures.Add(materials[i].name, materials[i].GetTexture("_MainTex"));
            }

            Log($"Got {materials.Length} default textures stored");
            FindMaterials(materials);
            DataCollector.CollectData();

            //The reason for apply a skin is that, incase we are in the game scene
            //and a material wasn't loaded into the resources in the vehicle config room
            //This will retry to apply it again after finding the list
            Apply();
        }

        private void SceneLoaded(VTOLScenes scene)
        {
            if (scene == VTOLScenes.VehicleConfiguration)
            {
                //Vehicle Configuration Room
                Log("Started Skins Vehicle Config room");
                StartCoroutine(GetDefaultTextures());
                SpawnMenu();
            }

            switch (scene)
            {
                case VTOLScenes.MeshTerrain:
                case VTOLScenes.OpenWater:
                case VTOLScenes.Akutan:
                case VTOLScenes.CustomMapBase:
                case VTOLScenes.CustomMapBase_OverCloud:
                    StartCoroutine(GetDefaultTextures());
                    break;
            }
        }
        private void SpawnMenu()
        {
            if (prefab == null)
                prefab = ModLoader.assetBundle.LoadAsset<GameObject>("SkinLoaderMenu");

            //Setting Position
            GameObject pannel = Instantiate(prefab);
            pannel.transform.position = new Vector3(-83.822f, -15.68818f, 5.774f);
            pannel.transform.rotation = Quaternion.Euler(-180, 62.145f, 180);

            Transform scenarioDisplayObject = pannel.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1);

            //Storing Objects for later use
            scenarioName = scenarioDisplayObject.GetChild(1).GetChild(3).GetComponent<Text>();
            scenarioDescription = scenarioDisplayObject.GetChild(1).GetChild(2).GetComponent<Text>();
            skinPreview = scenarioDisplayObject.GetChild(1).GetChild(1).GetComponent<RawImage>();

            //Linking buttons with methods
            VRInteractable NextENVButton = scenarioDisplayObject.GetChild(1).GetChild(5).GetComponent<VRInteractable>();
            VRInteractable PrevENVButton = scenarioDisplayObject.GetChild(1).GetChild(6).GetComponent<VRInteractable>();
            NextENVButton.OnInteract.AddListener(Next);
            PrevENVButton.OnInteract.AddListener(Previous);

            VRInteractable ResetButton = scenarioDisplayObject.GetChild(2).GetComponent<VRInteractable>();
            ResetButton.OnInteract.AddListener(RevertTextures);

            VRInteractable ApplyButton = scenarioDisplayObject.GetChild(1).GetChild(4).GetComponent<VRInteractable>();
            ApplyButton.OnInteract.AddListener(delegate { SelectSkin(); Apply(); });

            FindSkins(Path.Combine(ModLoaderManager.RootPath, "skins"));
            if (!string.IsNullOrEmpty(ModLoaderManager.MyProjectsPath))
                FindSkins(Path.Combine(ModLoaderManager.MyProjectsPath, "My Skins"));
            UpdateUI();

        }
        private void FindSkins(string path)
        {
            Log("Searching for Skins in " + path);
            foreach (string folder in Directory.GetDirectories(path))
            {
                Skin currentSkin = new Skin();
                string[] split = folder.Split('\\');
                currentSkin.name = split[split.Length - 1];
                if (File.Exists(folder + @"\0.png")) //AV-42C
                {
                    currentSkin.hasAv42c = true;
                    Log($"[{folder}] has a skin for the AV-42C");
                }

                if (File.Exists(folder + @"\1.png")) //FA26B
                {
                    currentSkin.hasFA26B = true;
                    Log($"[{folder}] has a skin for the FA-26B");
                }

                if (File.Exists(folder + @"\2.png")) //F45A
                {
                    currentSkin.hasF45A = true;
                    Log($"[{folder}] has a skin for the F-45A");
                }

                if (VTOLAPI.GetPlayersVehicleEnum() == VTOLVehicles.AV42C && currentSkin.hasAv42c)
                {
                    currentSkin.folderPath = folder;
                    installedSkins.Add(currentSkin);
                    Log("Added that skin to the list");
                }
                else if (VTOLAPI.GetPlayersVehicleEnum() == VTOLVehicles.FA26B && currentSkin.hasFA26B)
                {
                    currentSkin.folderPath = folder;
                    installedSkins.Add(currentSkin);
                    Log("Added that skin to the list");
                }
                else if (VTOLAPI.GetPlayersVehicleEnum() == VTOLVehicles.F45A && currentSkin.hasF45A)
                {
                    currentSkin.folderPath = folder;
                    installedSkins.Add(currentSkin);
                    Log("Added that skin to the list");
                }
                else if (!currentSkin.hasAv42c && !currentSkin.hasF45A && !currentSkin.hasF45A)
                {
                    LogError($"It seems that a folder doesn't have any skins in it. Folder: {folder}");
                }

            }
        }
        public void Next()
        {
            currentSkin += 1;
            ClampCount();
            UpdateUI();
        }
        public void Previous()
        {
            currentSkin -= 1;
            ClampCount();
            UpdateUI();

        }
        public void SelectSkin()
        {
            Debug.Log("Changed selected skin to " + currentSkin);
            selectedSkin = currentSkin;
        }



        private void FindMaterials(Material[] mats)
        {
            if (mats == null)
                mats = Resources.FindObjectsOfTypeAll<Material>();
            materials = new List<Mat>(mats.Length);

            //We now add every texture into the dictionary which gives more things to change for the skin creators
            for (int i = 0; i < mats.Length; i++)
            {
                materials.Add(new Mat(mats[i].name, mats[i]));
            }
        }
        public void RevertTextures()
        {
            Log("Reverting Textures");
            for (int i = 0; i < materials.Count; i++)
            {
                if (defaultTextures.ContainsKey(materials[i].name))
                    materials[i].material.SetTexture("_MainTex", defaultTextures[materials[i].name]);
                else
                    LogError($"Tried to get material {materials[i].name} but it wasn't in the default dictonary");
            }
        }
        private void Apply()
        {
            Log("Applying Skin Number " + selectedSkin);
            if (selectedSkin < 0)
            {
                Debug.Log("Selected Skin was below 0");
                return;
            }

            Skin selected = installedSkins[selectedSkin];

            Log("\nSkin: " + selected.name + " \nPath: " + selected.folderPath);

            for (int i = 0; i < materials.Count; i++)
            {
                if (File.Exists(selected.folderPath + @"\" + materials[i].name + ".png"))
                {
                    StartCoroutine(UpdateTexture(selected.folderPath + @"\" + materials[i].name + ".png", materials[i].material));
                    continue;
                }

                if (materials[i].name.Equals("mat_afighterExt2_livery") && File.Exists(selected.folderPath + @"\mat_aFighterExt2.png"))
                {
                    StartCoroutine(UpdateTexture(selected.folderPath + @"\mat_aFighterExt2.png", materials[i].material));
                }
            }
        }
        private IEnumerator UpdateTexture(string path, Material material)
        {
            Log("Updating Texture from path: " + path);
            if (material == null)
            {
                LogError("Material was null, not updating texture");
            }
            else
            {

                WWW www = new WWW("file:///" + path);
                while (!www.isDone)
                    yield return null;
                material.SetTexture("_MainTex", www.texture);
                Log($"Set Material for {material.name} to texture located at {path}");
            }
        }



        private void ClampCount()
        {
            if (currentSkin < 0)
            {
                Debug.Log("Current Skin was below 0, moving to max amount which is " + (installedSkins.Count - 1));
                currentSkin = installedSkins.Count - 1;
            }
            else if (currentSkin > installedSkins.Count - 1)
            {
                Debug.Log("Current Skin was higher than the max amount of skins, reseting to 0");
                currentSkin = 0;
            }
        }
        private void UpdateUI()
        {
            if (installedSkins.Count == 0)
                return;
            StartCoroutine(UpdateUIEnumerator());
            Log("Current Skin = " + currentSkin);
        }
        private IEnumerator UpdateUIEnumerator()
        {
            string preview = @"";
            switch (VTOLAPI.GetPlayersVehicleEnum())
            {
                case VTOLVehicles.AV42C:
                    preview = @"\0.png";
                    break;
                case VTOLVehicles.FA26B:
                    preview = @"\1.png";
                    break;
                case VTOLVehicles.F45A:
                    preview = @"\2.png";
                    break;
            }
            WWW www = new WWW("file:///" + installedSkins[currentSkin].folderPath + preview);
            while (!www.isDone)
                yield return null;
            scenarioName.text = installedSkins[currentSkin].name;
            skinPreview.texture = www.texture;
        }
        private void OnDestroy()
        {
            VTOLAPI.SceneLoaded -= SceneLoaded;
        }

        private class Skin
        {
            public string name;
            public bool hasAv42c, hasFA26B, hasF45A;
            public string folderPath;
        }
    }
}
