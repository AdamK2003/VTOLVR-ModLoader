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
    /// The method which gets called when the mod is loaded.
    /// private void Awake does the same thing
    /// </summary>
    [Obsolete]
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

    public void SetModInfo(Mod thisMod)
    {
        if (ThisMod == null)
            ThisMod = thisMod;
    }
}
