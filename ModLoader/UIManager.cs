using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ModLoader
{
    internal class UIManager : MonoBehaviour
    {
        private const string _assetBundleName = "modloader.assets";
        private const string _logoTextKey = "Logo with text.prefab";

        private static AssetBundle _assetBundle;

        private Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

        public void LoadAssetBundle(Action onCompleted) => StartCoroutine(LoadAssetBundleRoutine(onCompleted));

        private IEnumerator LoadAssetBundleRoutine(Action onCompleted)
        {
            Log("Loading Asset Bundle");
            string path = Path.Combine(ModLoaderManager.RootPath, _assetBundleName);
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
            yield return request;
            _assetBundle = request.assetBundle;
            Log("AssetBundle Loaded");
            onCompleted?.Invoke();
        }

        public void SplashScene()
        {
            bool loaded = _prefabs.TryGetValue(_logoTextKey, out GameObject Prefab);
            if (!loaded)
            {
                StartCoroutine(SplashSceneLoadPrefabs(SplashScene));
                return;
            }
            
            Log("Spawning Splash Scene UI");
            GameObject last = Instantiate(
                Prefab, 
                new Vector3(10, 1.0575f, 0), 
                Quaternion.Euler(0, 90, 0));
            last.transform.localScale = new Vector3(0.2340389f, 0.2340387f, 0.2340389f);
            
            last = Instantiate(
                Prefab, 
                new Vector3(0, 1.0575f, 10), 
                Quaternion.Euler(0, 0, 0));
            last.transform.localScale = new Vector3(0.2340387f, 0.2340387f, 0.2340387f);
            
            last = Instantiate(
                Prefab, 
                new Vector3(0, 1.0575f, -10), 
                Quaternion.Euler(0, 180, 0));
            last.transform.localScale = new Vector3(0.2340387f, 0.2340387f, 0.2340387f);
            
            last = Instantiate(
                Prefab, 
                new Vector3(-10, 1.0575f, 0), 
                Quaternion.Euler(0, -90, 0));
            last.transform.localScale = new Vector3(0.2340389f, 0.2340387f, 0.2340389f);
        }

        private IEnumerator SplashSceneLoadPrefabs(Action onCompleted)
        {
            Log($"Loading \"{_logoTextKey}\"");
            AssetBundleRequest request = _assetBundle.LoadAssetAsync<GameObject>(_logoTextKey);
            yield return request;
            _prefabs.Add(_logoTextKey, request.asset as GameObject);
            onCompleted?.Invoke();
        }

        private static void Log(object message) => Debug.Log($"[UI Manager]{message}");
        private static void Warning(object message) => Debug.LogWarning($"[UI Manager]{message}");
        private static void Error(object message) => Debug.LogError($"[UI Manager]{message}");
    }
}