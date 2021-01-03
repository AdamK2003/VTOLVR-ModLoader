using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
/// <summary>
/// The information stored about a mod which is used by the mod loader
/// and can be used by mods with the API command GetUsersMods
/// </summary>
public class Mod
{
    /// <summary>
    /// The name of the mod which displays on the mods page.
    /// </summary>
    public string name;
    /// <summary>
    /// The description of the mod which displays when the mod is selected. 
    /// </summary>
    public string description;
    /// <summary>
    /// The location of the .dll file of this mod.
    /// </summary>
    [XmlIgnore]
    public string dllPath;
    /// <summary>
    /// If the mod is currently loaded.
    /// </summary>
    [XmlIgnore]
    public bool isLoaded;
    /// <summary>
    /// The folder which the mods dll and other files are stored.
    /// </summary>
    [XmlIgnore]
    public string ModFolder;

    public Mod() { }

    public Mod(string name, string description, string dllPath, string modFolder)
    {
        this.name = name;
        this.description = description;
        this.dllPath = dllPath;
        ModFolder = modFolder;
    }
}

