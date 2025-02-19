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

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable
namespace BarrierRework
{
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
        private float _barrierDecayRateStatic = 0.033f;
        private float _barrierDecayRateDynamic = 0.33f;
        public static ConfigEntry<bool> UseDynamicDecay { get; set; }
        public static ConfigEntry<float> BarrierDecayRateStatic { get; set; }
        public static ConfigEntry<float> BarrierDecayRateDynamic { get; set; }
        public static ConfigEntry<bool> AegisRework { get; set; }
        public static ConfigEntry<float> AegisBarrierFlat { get; set; }

        #region config
        internal static ConfigFile CustomConfigFile { get; private set; }
        #endregion
        #region interfacing
        public static FixedConditionalWeakTable<CharacterBody, BarrierStats> characterBarrierStats = new FixedConditionalWeakTable<CharacterBody, BarrierStats>();
        public delegate void BarrierHookEventHandler(CharacterBody body, BarrierStats barrierStats);
        public static event BarrierHookEventHandler GetBarrierStats;
        public static BarrierStats GetBarrierStatsFromBody(CharacterBody body)
        {
            BarrierStats stats = null;

            if (!characterBarrierStats.TryGetValue(body, out stats))
                return new BarrierStats();

            return stats;
        }
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

            UseDynamicDecay = CustomConfigFile.Bind<bool>(
                "Barrier Stats",
                "Use Dynamic Decay",
                _useDynamicDecay,
                "Set to true to use dynamic decay, set to false for vanilla's max-health based flat decay."
                );

            BarrierDecayRateStatic = CustomConfigFile.Bind<float>(
                "Barrier Stats",
                "Flat Decay Rate",
                _barrierDecayRateStatic,
                "Vanilla flat barrier decay. Expressed as a fraction of maximum barrier.");

            BarrierDecayRateDynamic = CustomConfigFile.Bind<float>(
                "Barrier Stats",
                "Dynamic Decay Rate",
                _barrierDecayRateDynamic,
                "Fruity dynamic barrier decay. Expressed as a fraction of current barrier per frame.");

            if(AegisRework.Value == true)
                ReworkAegis();
            RoR2Application.onLoad += BuffBarrier;
        }
        void BuffBarrier()
        {
            On.RoR2.CharacterBody.FixedUpdate += this.BarrierBuff;
            On.RoR2.CharacterBody.RecalculateStats += DoBarrierStats;
        }

        private void DoBarrierStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            BarrierStats stats = characterBarrierStats.GetOrCreateValue(self);
            stats.barrierFreezeCount = 0;
            stats.barrierDecayIncreaseMultiplier = 1;
            stats.barrierDecayDecreaseDivisor = 1;
            stats.barrierGenPerSecondFlat = 0;
            stats.barrierDecayPerSecondFlat = 0;
            GetBarrierStats?.Invoke(self, stats);
        }

        private void BarrierBuff(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            if (NetworkServer.active)
            {
                BarrierStats barrierStats = characterBarrierStats.GetOrCreateValue(self);

                float decayMultiplier = barrierStats.barrierDecayIncreaseMultiplier / barrierStats.barrierDecayDecreaseDivisor;

                self.barrierDecayRate = 0;
                //Only set barrier decay if it's not currently disabled.
                if (barrierStats.barrierFreezeCount <= 0 && barrierStats.barrierDecayDecreaseDivisor > 0)
                {
                    float baseDecayRate = UseDynamicDecay.Value ?
                        Mathf.Max(1f, self.healthComponent.barrier * BarrierDecayRateDynamic.Value) :
                        self.maxBarrier * BarrierDecayRateStatic.Value;
                    float flatDecay = barrierStats.barrierDecayPerSecondFlat;
                    self.barrierDecayRate = (baseDecayRate + flatDecay) * decayMultiplier;
                }

                if (barrierStats.barrierGenPerSecondFlat > 0)
                {
                    float flatGen = barrierStats.barrierGenPerSecondFlat * decayMultiplier;
                    if (flatGen > self.barrierDecayRate)
                    {
                        float barrierExcess = flatGen - self.barrierDecayRate;
                        self.barrierDecayRate = 0;
                        self.healthComponent.AddBarrier(barrierExcess * Time.fixedDeltaTime);
                    }
                    else
                    {
                        self.barrierDecayRate += flatGen;
                    }
                }
            }

            orig(self);
        }
    }

    public class BarrierStats
    {
        public int barrierFreezeCount = 0;

        public float barrierDecayIncreaseMultiplier = 1;
        public float barrierDecayDecreaseDivisor = 1;

        public float barrierGenPerSecondFlat = 0;
        public float barrierDecayPerSecondFlat = 0;
    }
}
