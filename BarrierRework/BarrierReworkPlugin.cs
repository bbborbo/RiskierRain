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
using MoreStats;

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
        public const string version = "2.1.3";
        #endregion

        private bool _useDynamicDecay = true;
        private float _barrierDecayRateStatic = 30f; //30
        private float _barrierDecayHighFactor = 5f; //3f
        private float _barrierDecayLowFactor = 0.33f; //0.5f

        #region config
        internal static ConfigFile CustomConfigFile { get; private set; }
        public static ConfigEntry<float> BarrierDecayRateStatic { get; set; }
        public static ConfigEntry<float> BarrierDecayHighFactor { get; set; }
        public static ConfigEntry<float> BarrierDecayLowFactor { get; set; }
        public static ConfigEntry<bool> AegisRework { get; set; }
        public static ConfigEntry<float> AegisBarrierFlat { get; set; }
        public static ConfigEntry<float> AegisBarrierPercent { get; set; }
        #endregion

        public void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\FruityBarrierDecay.cfg", true);


            BarrierDecayRateStatic = CustomConfigFile.Bind<float>(
                "Barrier Stats",
                "Flat Decay Time",
                _barrierDecayRateStatic,
                "Base barrier decay rate before modifiers, vanilla is 30. Expressed in seconds to deplete maximum barrier.");

            BarrierDecayHighFactor = CustomConfigFile.Bind<float>(
                "Barrier Stats",
                "Dynamic Decay Factor (High)",
                _barrierDecayHighFactor,
                "Decay rate modifier when at HIGH barrier, vanilla is 3. Expressed as a multiplication of base decay.");
            BarrierDecayLowFactor = CustomConfigFile.Bind<float>(
                "Barrier Stats",
                "Dynamic Decay Factor (Low)",
                _barrierDecayLowFactor,
                "Decay rate modifier when at LOW barrier, vanilla is 0.5. Expressed as a multiplication of base decay.");

            RoR2Application.onLoad += BuffBarrier;
        }
        void BuffBarrier()
        {
            BaseStats.BarrierDecayStaticMaxHealthTime = BarrierDecayRateStatic.Value;
            BaseStats.BarrierHighDecayFactor = BarrierDecayHighFactor.Value;
            BaseStats.BarrierLowDecayFactor = BarrierDecayLowFactor.Value;
            //GetMoreStatCoefficients += ChangeBarrierDecay;
        }

        private void ChangeBarrierDecay(CharacterBody sender, MoreStatHookEventArgs args)
        {
            //args.FOR_REWORK_MODS_barrierBaseStaticDecayRateMaxHealthTime = BarrierDecayRateStatic.Value;
            //args.FOR_REWORK_MODS_barrierBaseDynamicDecayRateHalfLife = BarrierDecayRateDynamic.Value;
        }
    }
}
