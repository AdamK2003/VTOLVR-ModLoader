#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ModdingUtilitys : MonoBehaviour
{
    [MenuItem("VTOLVR Modding/Build Asset Bundles")]
    public static void BuildAllAssetBundles()
    {
        string dir = Directory.GetCurrentDirectory();
        string assetBundleDirectory = @"\AssetBundles";
        if (!Directory.Exists(dir + assetBundleDirectory))
        {
            Directory.CreateDirectory(dir + assetBundleDirectory);
        }
        else
        {
            string[] files = Directory.GetFiles(dir + assetBundleDirectory);
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    Debug.LogWarning("Deleting: " + files[i]);
                    File.Delete(files[i]);
                }
                catch {}
            }
            Debug.Log($"Deleted {files.Length} files in asset bundle folder");
        }
        try
        {
            BuildPipeline.BuildAssetBundles(dir + assetBundleDirectory, BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows);
            Debug.Log("Built Asset Bundles!");
        }
        catch (Exception e)
        {
            Debug.LogError("Error when building asset bundles.\n" + e.ToString());
            return;
        }

        
        if (Directory.Exists(@"A:\Program Files\Steam\steamapps\common\VTOL VR\VTOLVR_ModLoader"))
        {
            if (File.Exists(@"A:\Program Files\Steam\steamapps\common\VTOL VR\VTOLVR_ModLoader\modloader.assets"))
            {
                File.Delete(@"A:\Program Files\Steam\steamapps\common\VTOL VR\VTOLVR_ModLoader\modloader.assets");
            }
            File.Move(dir + assetBundleDirectory + @"\modloader.assets", @"A:\Program Files\Steam\steamapps\common\VTOL VR\VTOLVR_ModLoader\modloader.assets");
            Debug.Log("Move file to VTOL directory");
        }
        else
        {
            Process.Start(dir + assetBundleDirectory);
        }

    }
}
#endif