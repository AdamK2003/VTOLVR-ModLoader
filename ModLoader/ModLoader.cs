﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Net;
using System.Reflection;
using TMPro;
using ModLoader.Classes.Json;

namespace ModLoader
{
    class ModLoader : VTOLMOD
    {
        public enum Pages { MainMenu, Mods, Settings }
        public enum KeyboardType { DisableAll, Int, Float, String }
        public static ModLoader instance { get; private set; }
        public static AssetBundle assetBundle;
        private static List<Settings> _modSettings;
        public List<Mod> ModsLoaded { get; private set; } = new List<Mod>();
        private ModLoaderManager manager;
        private VTOLAPI api;

        private GameObject modsPage, settingsPage, CampaignListTemplate, settingsCampaignListTemplate, settingsScrollBox;
        private GameObject s_StringTemplate, s_BoolTemplate, s_FloatTemplate, s_IntTemplate, s_CustomLabel, s_Holder;
        private ScrollRect Scroll_View, settingsScrollView, settingsScrollBoxView;
        private Text SelectButton;
        private RectTransform selectionTF, settingsSelection;
        private BaseItem _selectedMod;
        private float buttonHeight = 100;
        private List<BaseItem> _currentMods = new List<BaseItem>();
        private List<Settings> currentSettings = new List<Settings>();
        private VRPointInteractableCanvas InteractableCanvasScript;
        private VRKeyboard stringKeyboard, floatKeyboard, intKeyboard;
        private string currentSelectedSetting = string.Empty;


        private GameObject MainScreen, modTemplate;
        private CampaignInfoUI modInfoUI;
        private TextMeshProUGUI modName, modDescription, loadButton;
        private RawImage modImage;

        private void Awake()
        {
            if (instance)
                Destroy(this.gameObject);

            instance = this;
            Mod mod = new Mod();
            mod.name = "Mod Loader";
            SetModInfo(mod);
            StartCoroutine(LoadAssetBundle());
        }
        private void Start()
        {
            manager = ModLoaderManager.Instance;
            api = VTOLAPI.instance;

            SceneManager.sceneLoaded += SceneLoaded;
        }
        private void SceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            switch (scene.name)
            {
                case "ReadyRoom":
                    CreateUI();
                    CheckForLOSMods();
                    break;
                case "Akutan":
                    break;
                default:
                    break;
            }
        }
        private IEnumerator LoadAssetBundle()
        {
            Log("Loading Asset Bundle");
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(Directory.GetCurrentDirectory() + @"\VTOLVR_ModLoader\modloader.assets");
            yield return request;
            assetBundle = request.assetBundle;
            Log("AssetBundle Loaded");
        }
        private void SetModInfo(string modName = "", string modDescription = "", bool hideImage = false, string imagePath = "")
        {
            if (this.modName)
                this.modName.text = modName;
            if (this.modDescription)
                this.modDescription.text = modDescription;
            if (modImage && hideImage)
                modImage.color = new Color(0, 0, 0, 0);
            if (hideImage == false)
            {
                modImage.color = Color.white;
                StartCoroutine(SetModPreviewImage(modImage, imagePath));
            }
            if (modName == "" && modDescription == "")
                selectionTF.GetComponent<RawImage>().color = new Color(0, 0, 0, 0);
        }
        private void CreateUI()
        {
            if (!assetBundle)
                LogError("Asset Bundle is null");

            Log("Creating UI for Ready Room");
            GameObject InteractableCanvas = GameObject.Find("InteractableCanvas");
            if (InteractableCanvas == null)
                LogError("InteractableCanvas was null");
            InteractableCanvasScript = InteractableCanvas.GetComponent<VRPointInteractableCanvas>();
            GameObject CampaignDisplay = GameObject.Find("CampaignSelector").transform.GetChild(0).GetChild(0).gameObject;
            if (CampaignDisplay == null)
                LogError("CampaignDisplay was null");
            CampaignDisplay.SetActive(true);
            MainScreen = GameObject.Find("MainScreen");
            if (MainScreen == null)
                LogError("Main Screen was null");

            Log("Spawning Keyboards");
            stringKeyboard = Instantiate(assetBundle.LoadAsset<GameObject>("StringKeyboard")).GetComponent<VRKeyboard>();
            floatKeyboard = Instantiate(assetBundle.LoadAsset<GameObject>("FloatKeyboard")).GetComponent<VRKeyboard>();
            intKeyboard = Instantiate(assetBundle.LoadAsset<GameObject>("IntKeyboard")).GetComponent<VRKeyboard>();
            stringKeyboard.gameObject.SetActive(false);
            floatKeyboard.gameObject.SetActive(false);
            intKeyboard.gameObject.SetActive(false);

            Log("Creating Mods Button");//Mods Button
            GameObject SettingsButton = MainScreen.transform.GetChild(0).GetChild(0).GetChild(8).gameObject;
            GameObject ModsButton = Instantiate(assetBundle.LoadAsset<GameObject>("ModsButton"), SettingsButton.transform.parent);
            ModsButton.transform.localPosition = new Vector3(-811, -412, 0);
            VRInteractable modsInteractable = ModsButton.GetComponent<VRInteractable>();
            modsInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Mods); SetDefaultText(); });

            Log("Creating Mods Page");//Mods Page
            modsPage = Instantiate(assetBundle.LoadAsset<GameObject>("ModLoaderDisplay"), CampaignDisplay.transform.parent);

            CampaignListTemplate = modsPage.transform.GetChild(3).GetChild(0).GetChild(0).GetChild(1).gameObject;
            Scroll_View = modsPage.transform.GetChild(3).GetComponent<ScrollRect>();
            buttonHeight = ((RectTransform)CampaignListTemplate.transform).rect.height;
            selectionTF = (RectTransform)modsPage.transform.GetChild(3).GetChild(0).GetChild(0).GetChild(0).transform;
            modInfoUI = modsPage.transform.GetChild(5).GetComponentInChildren<CampaignInfoUI>();
            SelectButton = modsPage.transform.GetChild(1).GetComponentInChildren<Text>();
            VRInteractable selectVRI = modsPage.transform.GetChild(1).GetComponent<VRInteractable>();
            if (selectVRI == null)
                LogError("selectVRI is null");
            selectVRI.OnInteract.AddListener(LoadMod);
            VRInteractable backInteractable = modsPage.transform.GetChild(2).GetComponent<VRInteractable>();
            if (backInteractable == null)
                LogError("backInteractable is null");
            backInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.MainMenu); });
            VRInteractable settingsInteractable = modsPage.transform.GetChild(4).GetComponent<VRInteractable>();
            settingsInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Settings); });


            if (_currentMods.Count == 0)
            {
                Log("Finding mods");
                _currentMods = ModReader.GetMods();
            }

            for (int i = 0; i < _currentMods.Count; i++)
            {
                _currentMods[i].ListGO = Instantiate(CampaignListTemplate, Scroll_View.content);
                _currentMods[i].ListGO.transform.localPosition = new Vector3(0f, -i * buttonHeight, 0f);
                _currentMods[i].ListGO.GetComponent<VRUIListItemTemplate>().Setup(_currentMods[i].Name, i, OpenMod);
                //Button currentButton = currentMods[i].listGO.transform.GetChild(2).GetComponent<Button>();
                //currentButton.onClick.RemoveAllListeners(); //Trying to remove the existing button click
                Log("Added Mod:\n" + _currentMods[i].Name + "\n" + _currentMods[i].Description);
            }

            Log("Loaded " + _currentMods.Count + " mods");

            Log("Mod Settings");//Mod Setttings
            settingsPage = Instantiate(assetBundle.LoadAsset<GameObject>("ModSettings"), CampaignDisplay.transform.parent);
            settingsSelection = (RectTransform)settingsPage.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).transform;


            s_BoolTemplate = assetBundle.LoadAsset<GameObject>("BoolTemplate");
            s_StringTemplate = assetBundle.LoadAsset<GameObject>("StringTemplate");
            s_IntTemplate = assetBundle.LoadAsset<GameObject>("NumberTemplate");
            s_CustomLabel = assetBundle.LoadAsset<GameObject>("CustomLabel");
            s_FloatTemplate = s_IntTemplate;
            s_Holder = modsPage.transform.GetChild(5).gameObject;

            Log("Setting up settings buttons");
            settingsCampaignListTemplate = settingsPage.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject;
            settingsCampaignListTemplate.SetActive(false);
            VRInteractable settingsBackInteractable = settingsPage.transform.GetChild(2).GetComponent<VRInteractable>();
            settingsBackInteractable.OnInteract.AddListener(delegate { OpenPage(Pages.Mods); });
            settingsScrollBox = settingsPage.transform.GetChild(4).gameObject;
            settingsScrollBoxView = settingsScrollBox.GetComponent<ScrollRect>();
            settingsScrollView = settingsPage.transform.GetChild(1).GetComponent<ScrollRect>();

            Log("Finished clearning up");//Finished and clearning up
            OpenPage(Pages.MainMenu);
            Scroll_View.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + _currentMods.Count) * buttonHeight);
            Scroll_View.ClampVertical();
            InteractableCanvasScript.RefreshInteractables();
            CampaignDisplay.SetActive(false);
            CampaignListTemplate.SetActive(false);
            SetDefaultText();
            RecreateSettings();
        }
        public void LoadMod()
        {
            if (_selectedMod == null)
            {
                LogError("There was no selected mod");
                return;
            }
            if (_selectedMod.Mod == null)
            {
                LogError("It seems the Mod was null in " + _selectedMod.Name);
                return;
            }
            if (_selectedMod.Mod.isLoaded)
            {
                Log(_selectedMod.Name + " is already loaded");
                return;
            }

            CheckForDependencies();

            string path = _selectedMod.GetFullDllPath();
            Log($"Loading from {_selectedMod.GetFullDllPath()}");
            IEnumerable<Type> source =
                from t in Assembly.Load(File.ReadAllBytes(path)).GetTypes()
                where t.IsSubclassOf(typeof(VTOLMOD))
                select t;
            if (source != null && source.Count() == 1)
            {
                GameObject newModGo = new GameObject(_selectedMod.Name, source.First());
                VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
                mod.SetModInfo(_selectedMod.Mod);
                newModGo.name = _selectedMod.Name;
                DontDestroyOnLoad(newModGo);
                _selectedMod.Mod.isLoaded = true;
                SelectButton.text = "Loaded!";
                mod.ModLoaded();
                ModsLoaded.Add(_selectedMod.Mod);
                ModLoaderManager.LoadedModsCount++;
                ModLoaderManager.Instance.UpdateDiscord();
            }
            else
            {
                LogError("Source is null");
            }
            Log("End of LoadMod");
        }
        private void CheckForDependencies()
        {
            string path = string.Empty;
            if (_selectedMod.IsDevFolder)
                path = Path.Combine(_selectedMod.Directory.Parent.FullName, "dependencies");
            else
                path = Path.Combine(_selectedMod.Directory.FullName, "dependencies");

            if (!Directory.Exists(path))
            {
                Log($"{_selectedMod.Name} doesn't have a dependencies folder. Checked {path} DevFolder = {_selectedMod.IsDevFolder}");
                return;
            }

            if (_selectedMod.Dependencies == null)
            {
                Log($"{_selectedMod.Name} Dependencies is null.");
                return;
            }

            FileInfo[] dlls = new DirectoryInfo(path).GetFiles("*.dll");
            for (int i = 0; i < dlls.Length; i++)
            {
                for (int j = 0; j < _selectedMod.Dependencies.Count; j++)
                {
                    if (_selectedMod.Dependencies[j] == dlls[i].Name)
                        LoadDependency(dlls[i]);
                }
            }
        }
        private void LoadDependency(FileInfo fileInfo)
        {
            Assembly assembly = Assembly.Load(File.ReadAllBytes(fileInfo.FullName));
            Log("Loaded Dependency " + fileInfo.FullName);

            IEnumerable<Type> source =
                from t in assembly.GetTypes()
                where t.IsSubclassOf(typeof(VTOLMOD))
                select t;
            // This Dependency is a VTOL Mod
            if (source != null && source.Count() == 1)
            {
                BaseItem item = CreateBaseItem(fileInfo);
                _currentMods.Add(item);

                GameObject newModGo = new GameObject(fileInfo.Name, source.First());
                VTOLMOD mod = newModGo.GetComponent<VTOLMOD>();
                mod.SetModInfo(item.Mod);
                newModGo.name = fileInfo.Name;
                DontDestroyOnLoad(newModGo);
                mod.ModLoaded();
                ModLoaderManager.LoadedModsCount++;
                Log("The Dependency was also a vtol mod and has been spawned");
                return;
            }

        }
        private BaseItem CreateBaseItem(FileInfo info)
        {
            BaseItem item = new BaseItem();
            item.Name = $"{_selectedMod.Name}/{info.Name}";
            item.DllPath = info.Name;
            item.Description = $"This mod is a dependency for {_selectedMod.Name}";
            item.Directory = info.Directory;
            item.CreateMod();
            return item;
        }
        public void OpenMod(int id)
        {
            if (id > _currentMods.Count - 1)
            {
                LogError("Open Mods tried to open a number too high.");
                return;
            }
            Log("Opening Mod " + id);
            _selectedMod = _currentMods[id];
            SelectButton.text = _selectedMod.Mod.isLoaded ? "Loaded!" : "Load";
            Scroll_View.ViewContent((RectTransform)_selectedMod.ListGO.transform);
            selectionTF.position = _selectedMod.ListGO.transform.position;
            selectionTF.GetComponent<Image>().color = new Color(0.3529411764705882f, 0.196078431372549f, 0);
            modInfoUI.campaignName.text = _selectedMod.Name;
            modInfoUI.campaignDescription.text = _selectedMod.Description;
            if (!string.IsNullOrWhiteSpace(_selectedMod.ImagePath))
            {
                modInfoUI.campaignImage.color = Color.white;
                StartCoroutine(SetModPreviewImage(modInfoUI.campaignImage, _selectedMod.ImagePath));
            }
            else
            {
                modInfoUI.campaignImage.color = new Color(0, 0, 0, 0);
            }

        }
        private void SetDefaultText()
        {
            Log("Setting Default Text for mod");
            modInfoUI.campaignName.text = "";
            modInfoUI.campaignDescription.text = "";
            modInfoUI.campaignImage.color = new Color(0, 0, 0, 0);
            selectionTF.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            settingsSelection.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        }
        public void OpenPage(Pages page)
        {
            Log("Opening Page " + page.ToString());
            modsPage.SetActive(false);
            MainScreen.SetActive(false);
            settingsPage.SetActive(false);

            switch (page)
            {
                case Pages.MainMenu:
                    MainScreen.SetActive(true);
                    break;
                case Pages.Mods:
                    modsPage.SetActive(true);
                    break;
                case Pages.Settings:
                    settingsPage.SetActive(true);
                    break;
                default:
                    break;
            }
        }
        private IEnumerator SetModPreviewImage(RawImage raw, string path)
        {
            if (raw == null)
                LogError("Image is null");
            WWW www = new WWW("file:///" + path);
            while (!www.isDone)
                yield return null;
            raw.texture = www.texture;
        }
        private void RecreateSettings()
        {
            if (_modSettings == null)
                return;
            for (int i = 0; i < _modSettings.Count; i++)
            {
                CreateSettingsMenu(_modSettings[i], true);
            }
        }
        public void CreateSettingsMenu(Settings settings, bool recreating = false)
        {
            int currentModIndex = FindModIndex(settings.Mod.ThisMod.name);

            _currentMods[currentModIndex].SettingsGO = Instantiate(settingsCampaignListTemplate, settingsScrollView.content);
            _currentMods[currentModIndex].SettingsGO.SetActive(true);
            _currentMods[currentModIndex].SettingsGO.transform.localPosition = new Vector3(0f, -(settingsScrollView.content.childCount - 5) * buttonHeight, 0f);
            _currentMods[currentModIndex].SettingsGO.GetComponent<VRUIListItemTemplate>().Setup(_currentMods[currentModIndex].Name, currentModIndex, OpenSetting);
            currentSettings.Add(settings);

            _currentMods[currentModIndex].SettingsHolerGO = new GameObject(_currentMods[currentModIndex].Name, typeof(RectTransform));
            _currentMods[currentModIndex].SettingsHolerGO.transform.SetParent(s_Holder.transform, false);

            for (int i = 0; i < settings.subSettings.Count; i++)
            {
                if (settings.subSettings[i] is Settings.BoolSetting)
                {
                    Log("Found Bool Setting");
                    Settings.BoolSetting currentBool = (Settings.BoolSetting)settings.subSettings[i];
                    GameObject boolGO = Instantiate(s_BoolTemplate, _currentMods[currentModIndex].SettingsHolerGO.transform, false);
                    boolGO.transform.GetChild(1).GetComponent<Text>().text = currentBool.settingName;
                    currentBool.text = boolGO.transform.GetChild(2).GetComponentInChildren<Text>();
                    currentBool.text.text = currentBool.defaultValue.ToString();
                    boolGO.transform.GetChild(2).GetComponent<VRInteractable>().OnInteract.AddListener(delegate { currentBool.Invoke(); });
                    boolGO.SetActive(true);

                    Log($"Spawned Bool Setting. Name:{currentBool.settingName} at {boolGO.transform.position}");
                }
                else if (settings.subSettings[i] is Settings.FloatSetting)
                {
                    Log("Found Float Setting");
                    Settings.FloatSetting currentFloat = (Settings.FloatSetting)settings.subSettings[i];
                    GameObject floatGO = Instantiate(s_FloatTemplate, _currentMods[currentModIndex].SettingsHolerGO.transform, false);
                    floatGO.transform.GetChild(1).GetComponent<Text>().text = currentFloat.settingName;
                    currentFloat.text = floatGO.transform.GetChild(2).GetComponent<Text>();
                    currentFloat.text.text = currentFloat.value.ToString();
                    floatGO.transform.GetChild(3).GetComponent<VRInteractable>().OnInteract.AddListener(delegate
                    {
                        OpenKeyboard(KeyboardType.Float, currentFloat.value.ToString(), 32, currentFloat.SetValue);
                    });
                    floatGO.SetActive(true);
                    Log($"Spawned Float setting called {currentFloat.settingName} at {floatGO.transform.position}");
                }
                else if (settings.subSettings[i] is Settings.IntSetting)
                {
                    Log("Found Int Setting");
                    Settings.IntSetting currentInt = (Settings.IntSetting)settings.subSettings[i];
                    GameObject intGO = Instantiate(s_IntTemplate, _currentMods[currentModIndex].SettingsHolerGO.transform, false);
                    intGO.transform.GetChild(1).GetComponent<Text>().text = currentInt.settingName;
                    currentInt.text = intGO.transform.GetChild(2).GetComponent<Text>();
                    currentInt.text.text = currentInt.value.ToString();
                    intGO.transform.GetChild(3).GetComponent<VRInteractable>().OnInteract.AddListener(delegate
                    {
                        OpenKeyboard(KeyboardType.Int, currentInt.value.ToString(), 32, currentInt.SetValue);
                    });
                    intGO.SetActive(true);
                    Log($"Spawned Int setting called {currentInt.settingName} at {intGO.transform.position}");

                }
                else if (settings.subSettings[i] is Settings.StringSetting)
                {
                    Log("Found String Setting");
                    Settings.StringSetting currentString = (Settings.StringSetting)settings.subSettings[i];
                    GameObject stringGO = Instantiate(s_StringTemplate, _currentMods[currentModIndex].SettingsHolerGO.transform, false);
                    stringGO.transform.GetChild(1).GetComponent<Text>().text = currentString.settingName;
                    currentString.text = stringGO.transform.GetChild(2).GetComponentInChildren<Text>();
                    currentString.text.text = currentString.value;
                    stringGO.transform.GetChild(3).GetComponent<VRInteractable>().OnInteract.AddListener(delegate
                    {
                        OpenKeyboard(KeyboardType.String, currentString.value, 32, currentString.SetValue);
                    });
                    stringGO.SetActive(true);
                    Log($"Spawned String setting called {currentString.settingName} at {stringGO.transform.position}");
                }
                else if (settings.subSettings[i] is Settings.CustomLabel)
                {
                    Log("Found a custom label");
                    Settings.CustomLabel currentLabel = (Settings.CustomLabel)settings.subSettings[i];
                    GameObject label = Instantiate(s_CustomLabel, _currentMods[currentModIndex].SettingsHolerGO.transform, false);
                    label.GetComponentInChildren<Text>().text = currentLabel.settingName;
                    label.SetActive(true);
                    Log($"Spawned a custom label with the text:\n{currentLabel.settingName}");
                }
            }
            _currentMods[currentModIndex].SettingsHolerGO.SetActive(false);
            if (!recreating)
            {
                if (_modSettings == null)
                    _modSettings = new List<Settings>();
                _modSettings.Add(settings);
            }
            Debug.Log("Done spawning " + settings.subSettings.Count + " settings");
            RefreshSettings();
        }
        private void RefreshSettings()
        {
            settingsScrollView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + settingsScrollView.content.childCount) * buttonHeight);
            settingsScrollView.ClampVertical();
            settingsScrollBoxView.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (2f + 1) * 100);
            settingsScrollBoxView.ClampVertical();
            InteractableCanvasScript.RefreshInteractables();
        }
        public void OpenSetting(int id)
        {
            if (id > _currentMods.Count - 1)
            {
                LogError("Open Mods tried to open a number too high.");
                return;
            }
            Log("Opening Settings for mod " + id);
            BaseItem selectedMod = _currentMods[id];
            settingsScrollView.ViewContent((RectTransform)selectedMod.SettingsGO.transform);
            settingsSelection.position = selectedMod.SettingsGO.transform.position;
            settingsSelection.GetComponent<Image>().color = new Color(0.3529411764705882f, 0.196078431372549f, 0);

            if (currentSelectedSetting != string.Empty)
            {
                //There already is something on the content of the settings scroll box.
                MoveBackToPool(currentSelectedSetting);
            }
            MoveToSettingsView(selectedMod.SettingsHolerGO.transform);
            RefreshSettings();
        }
        private void MoveToSettingsView(Transform parent)
        {
            currentSelectedSetting = parent.name;
            //They need to be stored in a temp array so that we can move them all.
            Transform[] children = new Transform[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
            {
                children[i] = parent.GetChild(i);
            }
            for (int i = 0; i < children.Length; i++)
            {
                children[i].SetParent(settingsScrollBoxView.content, false);
            }
        }
        private void MoveBackToPool(string name)
        {
            int modIndex = FindModIndex(name);
            Transform holder = s_Holder.transform.Find(_currentMods[modIndex].SettingsHolerGO.name);
            if (holder == null)
            {
                //Couldn't find the holder for some reason in the pool.
                LogWarning("Couldn't find holder for settings, recreating it");
                holder = new GameObject(_currentMods[modIndex].Name, typeof(RectTransform)).transform;
                holder.SetParent(s_Holder.transform, false);
                _currentMods[modIndex].SettingsHolerGO = holder.gameObject;
            }

            Transform[] itemsToMove = new Transform[settingsScrollBoxView.content.childCount];
            for (int i = 0; i < settingsScrollBoxView.content.childCount; i++)
            {
                itemsToMove[i] = settingsScrollBoxView.content.GetChild(i);
            }

            for (int i = 0; i < itemsToMove.Length; i++)
            {
                itemsToMove[i].SetParent(holder, false);
            }
        }
        /// <summary>
        /// Finds the index of the current mod by its name.
        /// </summary>
        /// <param name="name">Mod.name value of what you want to find</param>
        /// <returns>The index of where this mod is in the currentMods list</returns>
        private int FindModIndex(string name)
        {
            int returnValue = -1;
            for (int i = 0; i < _currentMods.Count; i++)
            {
                if (_currentMods[i].Name.Equals(name))
                {
                    returnValue = i;
                    break;
                }
            }
            return returnValue;
        }
        public void OpenKeyboard(KeyboardType keyboardType, string startingText, int maxChars, Action<string> onEntered, UnityAction onCancelled = null)
        {
            OpenKeyboard(KeyboardType.DisableAll); //Closing them all first incase one is opened
            if (keyboardType == KeyboardType.DisableAll)
                return;
            switch (keyboardType)
            {
                case KeyboardType.Int:
                    intKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
                case KeyboardType.Float:
                    floatKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
                case KeyboardType.String:
                    stringKeyboard.Display(startingText, maxChars, onEntered, onCancelled);
                    break;
            }
        }
        public void OpenKeyboard(KeyboardType keyboardType)
        {
            if (keyboardType != KeyboardType.DisableAll)
                return;
            stringKeyboard.gameObject.SetActive(false);
            intKeyboard.gameObject.SetActive(false);
            floatKeyboard.gameObject.SetActive(false);
        }
        private void CheckForLOSMods()
        {
            Log($"Checking for Load On Start Mods. Count = {_currentMods.Count} Settings Count = {LauncherSettings.Settings.Items.Count}");
            for (int i = 0; i < _currentMods.Count; i++)
            {
                for (int j = 0; j < LauncherSettings.Settings.Items.Count; j++)
                {
                    if (LauncherSettings.Settings.Items[j].LoadOnStartCheck &&
                        _currentMods[i].Directory.FullName.ToLower() == LauncherSettings.Settings.Items[j].FolderDirectory.ToLower())
                    {
                        Log($"Found {_currentMods[i].Name} with LOS");
                        _selectedMod = _currentMods[i];
                        LoadMod();
                        break;
                    }
                }
            }
            _selectedMod = null;
        }
    }
}
