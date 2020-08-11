using System;
using UnityEngine;
using ModLoader;
/// <summary>
/// Base Class for any mod so that the mod loader knows what class to load.
/// </summary>
public class VTOLMOD : MonoBehaviour
{
    /// <summary>
    /// Storing the Mods data in a class
    /// </summary>
    public Mod ThisMod { private set; get; } = null;
    /// <summary>
    /// Gets the folder which this current dll is located.
    /// Returns string.Empty if "ThisMod" is null
    /// </summary>
    public string ModFolder 
    { 
        get 
        {
            if (ThisMod != null)
                return ThisMod.ModFolder;
            else
                return string.Empty;
        } 
    }

    /// <summary>
    /// The method which gets called when the mod is loaded.
    /// </summary>
    public virtual void ModLoaded()
    {
        Log("Loaded!");
    }
    /// <summary>
    /// Logs a message to the console with your mod name.
    /// </summary>
    /// <param name="message"></param>
    public void Log(object message)
    {
        if (ThisMod == null)
            Debug.Log(gameObject.name + ": " + message);
        else
            Debug.Log(ThisMod.name + ": " + message);
    }
    /// <summary>
    /// Logs a warning message to the console with your mod name.
    /// </summary>
    /// <param name="message"></param>
    public void LogWarning(object message)
    {
        if (ThisMod == null)
            Debug.LogWarning(gameObject.name + ": " + message);
        else
            Debug.LogWarning(ThisMod.name + ": " + message);
    }
    /// <summary>
    /// Logs a error message to the console with your mod name.
    /// </summary>
    /// <param name="message"></param>
    public void LogError(object message)
    {
        if (ThisMod == null)
            Debug.LogError(gameObject.name + ": " + message);
        else
            Debug.LogError(ThisMod.name + ": " + message);
    }
    /// <summary>
    /// Used by the mod loader to set ThisMod variable.
    /// </summary>
    /// <param name="thisMod"></param>
    public void SetModInfo(Mod thisMod)
    {
        if (ThisMod == null)
            ThisMod = thisMod;
    }
}
