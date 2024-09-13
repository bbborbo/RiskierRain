using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        public const string version = "1.0.1";
        #endregion

        public void Awake()
        {
            ReworkAegis();
            RoR2Application.onLoad += BuffBarrier;
        }
        private float barrierDecayRateStatic = 0.033f;
        private float barrierDecayRateDynamic = 0.33f;
        bool useDynamicDecay = true;
        void BuffBarrier()
        {
            On.RoR2.CharacterBody.FixedUpdate += this.BarrierBuff;
        }
        private void BarrierBuff(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            //Only set barrier decay if it's not currently disabled.
            if (self.barrierDecayRate > 0)
            {
                self.barrierDecayRate = useDynamicDecay ? 
                    Mathf.Max(1f, self.healthComponent.barrier * this.barrierDecayRateDynamic) : 
                    self.maxBarrier * barrierDecayRateStatic;
            }

            orig(self);
        }
    }
}
