using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ModularEclipse;
using System.Security.Permissions;
using System.Security;
using System.Linq;
using BepInEx.Configuration;
using System.Collections.Generic;
using BepInEx.Bootstrap;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MissileRework
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(ModularEclipsePlugin.guid, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DamageAPI))]
    public partial class MissileReworkPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }

        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "HouseOfFruits";
        public const string modName = "MissileRework";
        public const string version = "1.0.0";
        #endregion

        #region config
        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> ShouldReworkIcbm { get; set; }
        public static ConfigEntry<bool> ShouldReworkAtg { get; set; }
        public static ConfigEntry<bool> ShouldReworkShrimp { get; set; }
        public static ConfigEntry<bool> ShouldReworkDml { get; set; }
        public static ConfigEntry<bool> ShouldReworkEnemyMissileTargeting { get; set; }
        #endregion

        internal static bool isLoaded(string modguid)
        {
            foreach (KeyValuePair<string, PluginInfo> keyValuePair in Chainloader.PluginInfos)
            {
                string key = keyValuePair.Key;
                PluginInfo value = keyValuePair.Value;
                bool flag = key == modguid;
                if (flag)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool ModularEclipseLoaded = isLoaded(ModularEclipse.ModularEclipsePlugin.guid);

        ArtifactDef MissileArtifact = ScriptableObject.CreateInstance<ArtifactDef>();
        ItemDef icbmItemDef;

        public const float missileSpread = 45;
        public const float projectileSpread = 25;

        public void Awake()
        {

            ShouldReworkIcbm = CustomConfigFile.Bind<bool>(modName + ": Reworks", "Pocket ICBM (incl. Artifact of Warfare)", true,
                "Set to TRUE to rework Pocket ICBM and turn its vanilla effect into an artifact.");
            ShouldReworkAtg = CustomConfigFile.Bind<bool>(modName + ": Reworks", "AtG Missile Mk.3", true,
                "Set to TRUE to rework AtG Missile Mk.1.");
            ShouldReworkShrimp = CustomConfigFile.Bind<bool>(modName + ": Reworks", "Plasma Shrimp", true,
                "Set to TRUE to rework Plasma Shrimp.");
            ShouldReworkDml = CustomConfigFile.Bind<bool>(modName + ": Reworks", "Disposable Missile Launcher", true,
                "Set to TRUE to rework Disposable Missile Launcher.");
            ShouldReworkEnemyMissileTargeting = CustomConfigFile.Bind<bool>(modName + ": Reworks", "Missile Tracking", true,
                "Set to TRUE to rework missile tracking on enemies.");

            if (ShouldReworkIcbm.Value == true)
            {
                CreateArtifact();
                ReworkIcbm();
            }
            if (ShouldReworkAtg.Value == true)
            {
                ReworkAtg();
            }
            if (ShouldReworkShrimp.Value == true)
            {
                ReworkPrimp();
            }
        }
    }
}