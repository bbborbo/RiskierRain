using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.DamageAPI;
using static MoreStats.StatHooks;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable
namespace BarrierRework
{
    [BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition))]
    public partial class BarrierReworkPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }

        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "FruityBarrierDecay";
        public const string version = "2.0.0";
        #endregion

        private bool _useDynamicDecay = true;
        private float _barrierDecayRateStatic = 0f; //30
        private float _barrierDecayRateDynamic = 3f; //0
        public static ConfigEntry<float> BarrierDecayRateStatic { get; set; }
        public static ConfigEntry<float> BarrierDecayRateDynamic { get; set; }
        public static ConfigEntry<bool> AegisRework { get; set; }
        public static ConfigEntry<float> AegisBarrierFlat { get; set; }

        #region config
        internal static ConfigFile CustomConfigFile { get; private set; }
        #endregion

        public void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\FruityBarrierDecay.cfg", true);

            AegisRework = CustomConfigFile.Bind<bool>(
                "Aegis Rework",
                "Enable Aegis Rework",
                true,
                "Set to true to use fruity aegis, set to false for vanilla aegis."
                );

            AegisBarrierFlat = CustomConfigFile.Bind<float>(
                "Aegis Rework",
                "Aegis Barrier On Interactable",
                _aegisBarrierFlat,
                "How much barrier the reworked Aegis grants on using interactables.");

            BarrierDecayRateStatic = CustomConfigFile.Bind<float>(
                "Barrier Stats",
                "Flat Decay Time",
                _barrierDecayRateStatic,
                "Flat barrier decay, vanilla is 30 seconds. Expressed in seconds to deplete maximum barrier.");

            BarrierDecayRateDynamic = CustomConfigFile.Bind<float>(
                "Barrier Stats",
                "Dynamic Decay Half-Life",
                _barrierDecayRateDynamic,
                "Fruity dynamic barrier decay. Expressed in seconds of half-life.");

            if(AegisRework.Value == true)
                ReworkAegis();
            RoR2Application.onLoad += BuffBarrier;
        }
        void BuffBarrier()
        {
            GetMoreStatCoefficients += ChangeBarrierDecay;
        }

        private void ChangeBarrierDecay(CharacterBody sender, MoreStatHookEventArgs args)
        {
            args.barrierBaseStaticDecayRateMaxHealthTime = BarrierDecayRateStatic.Value;
            args.barrierBaseDynamicDecayRateHalfLife = BarrierDecayRateDynamic.Value;
        }
    }
}
