﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace VTOLVR_ModLoader.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("VTOLVR_ModLoader.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [
        ///  &quot;0Harmony.dll&quot;,
        ///  &quot;Accessibility.dll&quot;,
        ///  &quot;Assembly-CSharp-firstpass.dll&quot;,
        ///  &quot;Assembly-CSharp.dll&quot;,
        ///  &quot;CsvHelper.dll&quot;,
        ///  &quot;HttpAuth.dll&quot;,
        ///  &quot;Microsoft.CSharp.dll&quot;,
        ///  &quot;Mono.Security.dll&quot;,
        ///  &quot;MP3Sharp.dll&quot;,
        ///  &quot;mscorlib.dll&quot;,
        ///  &quot;NAudio.dll&quot;,
        ///  &quot;netstandard.dll&quot;,
        ///  &quot;Valve.Newtonsoft.Json.dll&quot;,
        ///  &quot;Oculus.VR.dll&quot;,
        ///  &quot;Rewired_Core.dll&quot;,
        ///  &quot;Rewired_Windows_Lib.dll&quot;,
        ///  &quot;SharpDX.DirectInput.dll&quot;,
        ///  &quot;SharpDX.dll&quot;,
        ///  &quot;SteamVR.dll&quot;,
        ///  &quot;SteamVR_Actions.dll&quot;,
        ///  &quot;System.ComponentModel.Composition.dll [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string BaseDLLS {
            get {
                return ResourceManager.GetString("BaseDLLS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {
        ///  &quot;Campaigns&quot;: [
        ///    {
        ///      &quot;CampaignID&quot;: &quot;av42cTheIsland&quot;,
        ///      &quot;CampaignName&quot;: &quot;The Island&quot;,
        ///      &quot;Description&quot;: &quot;The Island campaign.&quot;,
        ///      &quot;Vehicle&quot;: &quot;AV-42C&quot;,
        ///      &quot;Scenarios&quot;: [
        ///        {
        ///          &quot;Id&quot;: &quot;01_preparations&quot;,
        ///          &quot;Name&quot;: &quot;Preparations&quot;,
        ///          &quot;Description&quot;: &quot;Help make preparations to defend the island. Transport personnel to coastal SAM site.&quot;,
        ///          &quot;CampaignOrderIdx&quot;: 0
        ///        },
        ///        {
        ///          &quot;Id&quot;: &quot;02_minesweeper&quot;,
        ///          &quot;Name&quot;: &quot;Mines [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CampaignsJsonString {
            get {
                return ResourceManager.GetString("CampaignsJsonString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to https://20fc2038e3e34ef99e7cfb790618cc00@o411102.ingest.sentry.io/5434499.
        /// </summary>
        internal static string Dsn {
            get {
                return ResourceManager.GetString("Dsn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] vtolvr_mod_boilerplate_master {
            get {
                object obj = ResourceManager.GetObject("vtolvr_mod_boilerplate_master", resourceCulture);
                return ((byte[])(obj));
            }
        }
    }
}
