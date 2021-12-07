using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Enums;
using Core.Jsons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ModLoader
{
    public class SkinManager : VTOLMOD
    {
        //This variables are used on different scenes
        private List<BaseItem> _skins = new List<BaseItem>();
        private List<Skin> _installedSkins = new List<Skin>();
        private int _selectedSkin = -1;

        //Vehicle Config scene only
        private int _currentSkin;
        private Text _scenarioName;
        private Text _scenarioDescription;
        private RawImage _skinPreview;

        private static GameObject _prefab;

        /// <summary>
        /// All the materials in the game
        /// </summary>
        private List<Mat> _materials;
        /// <summary>
        /// The default textures so we can revert back
        /// </summary>
        private Dictionary<string, Texture> _defaultTextures;
        private readonly string[] _matsNotToTouch = new string[] { "Font Material", "Font Material_0", "Font Material_1", "Font Material_2", "Font Material_3", "Font Material_4", "Font Material_5", "Font Material_6" };
        
        /// <summary>
        /// A dictionary of all the already loaded textures. Their full path as the key.
        /// </summary>
        private Dictionary<string, Texture2D> _loadedTextures = new Dictionary<string, Texture2D>();
        
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
            _defaultTextures = new Dictionary<string, Texture>(materials.Length);


            for (int i = 0; i < materials.Length; i++)
            {
                if (!_matsNotToTouch.Contains(materials[i].name) && !_defaultTextures.ContainsKey(materials[i].name))
                    _defaultTextures.Add(materials[i].name, materials[i].GetTexture("_MainTex"));
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
            if (_prefab == null)
                _prefab = ModLoader.assetBundle.LoadAsset<GameObject>("SkinLoaderMenu");

            //Setting Position
            GameObject pannel = Instantiate(_prefab);
            pannel.transform.position = new Vector3(-83.822f, -15.68818f, 5.774f);
            pannel.transform.rotation = Quaternion.Euler(-180, 62.145f, 180);

            Transform scenarioDisplayObject = pannel.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1);

            //Storing Objects for later use
            _scenarioName = scenarioDisplayObject.GetChild(1).GetChild(3).GetComponent<Text>();
            _scenarioDescription = scenarioDisplayObject.GetChild(1).GetChild(2).GetComponent<Text>();
            _skinPreview = scenarioDisplayObject.GetChild(1).GetChild(1).GetComponent<RawImage>();
            
            _scenarioDescription.gameObject.SetActive(true);

            //Linking buttons with methods
            VRInteractable NextENVButton = scenarioDisplayObject.GetChild(1).GetChild(5).GetComponent<VRInteractable>();
            VRInteractable PrevENVButton = scenarioDisplayObject.GetChild(1).GetChild(6).GetComponent<VRInteractable>();
            NextENVButton.OnInteract.AddListener(Next);
            PrevENVButton.OnInteract.AddListener(Previous);

            VRInteractable ResetButton = scenarioDisplayObject.GetChild(2).GetComponent<VRInteractable>();
            ResetButton.OnInteract.AddListener(RevertTextures);

            VRInteractable ApplyButton = scenarioDisplayObject.GetChild(1).GetChild(4).GetComponent<VRInteractable>();
            ApplyButton.OnInteract.AddListener(delegate { SelectSkin(); Apply(); });

            FindSkins();
            UpdateUI();

        }
        
        private void FindSkins()
        {
            _skins = ModReader.Items.Where(
                x => x.ContentType == ContentType.Skins || x.ContentType == ContentType.MySkins).ToList();
        }
        
        public void Next()
        {
            _currentSkin += 1;
            ClampCount();
            UpdateUI();
        }
        
        public void Previous()
        {
            _currentSkin -= 1;
            ClampCount();
            UpdateUI();

        }
        
        public void SelectSkin()
        {
            Debug.Log("Changed selected skin to " + _currentSkin);
            _selectedSkin = _currentSkin;
        }
        
        private void FindMaterials(Material[] mats)
        {
            if (mats == null)
                mats = Resources.FindObjectsOfTypeAll<Material>();
            _materials = new List<Mat>(mats.Length);

            //We now add every texture into the dictionary which gives more things to change for the skin creators
            for (int i = 0; i < mats.Length; i++)
            {
                _materials.Add(new Mat(mats[i].name, mats[i]));
            }
        }
        public void RevertTextures()
        {
            Log("Reverting Textures");
            for (int i = 0; i < _materials.Count; i++)
            {
                if (_defaultTextures.ContainsKey(_materials[i].name))
                    _materials[i].material.SetTexture("_MainTex", _defaultTextures[_materials[i].name]);
                else
                    LogError($"Tried to get material {_materials[i].name} but it wasn't in the default dictonary");
            }
        }
        
        private void Apply()
        {
            Log("Applying Skin Number " + _selectedSkin);
            if (_selectedSkin < 0)
            {
                Debug.Log("Selected Skin was below 0");
                return;
            }

            BaseItem skin = _skins[_currentSkin];
            Log($"Skin = {skin.Name}|Path = {skin.Directory.FullName}");

            foreach (Core.Classes.Material material in skin.SkinMaterials)
            {
                for (int i = 0; i < _materials.Count; i++)
                {
                    if (!material.Name.Equals(_materials[i].material.name))
                        continue;
                    StartCoroutine(SetTextures(material.Textures, _materials[i].material, skin.Directory.FullName));
                    break;
                }
            }

            if (skin.SkinMaterials.Count != 0)
            {
                Log($"{skin.Name} is an up to date skin");
                return;
            }
            
            // This section is to keep old skins still working.
            
            LogWarning($"{skin.Name} is a legacy skin.");

            string lastPath = string.Empty;
            for (int i = 0; i < _materials.Count; i++)
            {
                lastPath = Path.Combine(skin.Directory.FullName, $"{_materials[i].name}.png");
                if (File.Exists(lastPath))
                {
                    StartCoroutine(UpdateTexture(lastPath, _materials[i].material));
                    continue;
                }
                lastPath = Path.Combine(skin.Directory.FullName, "mat_aFighterExt2.png");
                if (_materials[i].name.Equals("mat_afighterExt2_livery") && File.Exists(lastPath))
                {
                    StartCoroutine(UpdateTexture(lastPath, _materials[i].material));
                }
            }

            /*
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
            */

        }

        private IEnumerator SetTextures(Dictionary<string, string> textures, Material material, string folder)
        {
            string lastPath = string.Empty;
            foreach (KeyValuePair<string,string> pair in textures)
            {
                lastPath = Path.Combine(folder, pair.Value);
                
                // Checking if we have already loaded it
                if (_loadedTextures.ContainsKey(lastPath))
                {
                    material.SetTexture(pair.Key, _loadedTextures[lastPath]);
                    Log($"Found {lastPath} cached");
                    yield break;
                }
                
                Log($"Loading {lastPath} as it isn't in the cache");
                using (WWW www = new WWW($"file:///{lastPath}"))
                {
                    while (!www.isDone)
                        yield return null;
                    material.SetTexture(pair.Key, www.texture);
                    
                    // This check is here in case the same texture gets loaded twice
                    // in two different materials. 
                    if (!_loadedTextures.ContainsKey(lastPath))
                        _loadedTextures.Add(lastPath, www.texture);
                }
            }
        }
        
        // Legacy skins loading method
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
            if (_currentSkin < 0)
            {
                Debug.Log("Current Skin was below 0, moving to max amount which is " + (_skins.Count - 1));
                _currentSkin = _skins.Count - 1;
            }
            else if (_currentSkin > _skins.Count - 1)
            {
                Debug.Log("Current Skin was higher than the max amount of skins, reseting to 0");
                _currentSkin = 0;
            }
        }
        
        private void UpdateUI()
        {
            if (_skins.Count == 0)
                return;
            StartCoroutine(UpdateUIEnumerator());
            Log("Current Skin = " + _currentSkin);
        }
        
        private IEnumerator UpdateUIEnumerator()
        {
            BaseItem skin = _skins[_currentSkin];
            string previewImagePath = String.Empty;

            if (!string.IsNullOrEmpty(skin.PreviewImage))
            {
                previewImagePath = skin.PreviewImage;
            }
            else
            {
                // This is for old skins before 5.2.0
                
                string preview = @"";
                switch (VTOLAPI.GetPlayersVehicleEnum())
                {
                    case VTOLVehicles.AV42C:
                        preview = @"0.png";
                        break;
                    case VTOLVehicles.FA26B:
                        preview = @"1.png";
                        break;
                    case VTOLVehicles.F45A:
                        preview = @"2.png";
                        break;
                }
                Log($"{skin.Directory} + {preview} = {Path.Combine(skin.Directory.FullName, preview)}");
                previewImagePath = Path.Combine(skin.Directory.FullName, preview);
                LogWarning($"Using legacy image path of \"{previewImagePath}\" for {skin.Name}");
            }

            Texture2D previewImage;
            if (_loadedTextures.ContainsKey(previewImagePath))
            {
                previewImage = _loadedTextures[previewImagePath];
            }
            else
            {
                using (WWW www = new WWW($"file://{previewImagePath}"))
                {
                    while (!www.isDone)
                        yield return null;
                    previewImage = www.texture;
                }
            }
            
            _scenarioName.text = skin.Name;
            _scenarioDescription.text = skin.Tagline;
            _skinPreview.texture = previewImage;
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
