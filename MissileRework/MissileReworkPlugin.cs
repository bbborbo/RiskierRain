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
using MoreStats;
using System.Runtime.CompilerServices;
using ProcSolver;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MissileRework
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(ModularEclipsePlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DamageAPI))]
    public partial class MissileReworkPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }

        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "IAmBecomeMissiles";
        public const string version = "1.2.0";
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

        ItemDef icbmItemDef;

        private static AssetBundle _assetBundle;
        public static AssetBundle assetBundle
        {
            get
            {
                if (_assetBundle == null)
                    _assetBundle = AssetBundle.LoadFromFile(GetAssetBundlePath("missilereworkassets"));
                return _assetBundle;
            }
            set
            {
                _assetBundle = value;
            }
        }
        public static string GetAssetBundlePath(string bundleName)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(MissileReworkPlugin.PInfo.Location), bundleName);
        }

        public void Awake()
        {
            PInfo = Info;

            CustomConfigFile = new ConfigFile(Paths.ConfigPath + $"\\{modName}.cfg", true);

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
            if(ShouldReworkEnemyMissileTargeting.Value == true)
            {
                On.RoR2.Projectile.MissileController.FixedUpdate += MissileController_FixedUpdate;
                On.RoR2.Projectile.MissileController.FindTarget += MissileController_FindTarget;
            }
        }

        private void MissileController_FixedUpdate(On.RoR2.Projectile.MissileController.orig_FixedUpdate orig, RoR2.Projectile.MissileController self)
        {
            if(self.teamFilter != null && self.teamFilter.teamIndex != TeamIndex.Player)
            {
                if(self.targetComponent.target != null)
                {
                    CharacterMotor targetMotor = self.targetComponent.target.GetComponent<HurtBox>()?.healthComponent?.body.characterMotor;
                    if(targetMotor != null)
                    {
                        if (targetMotor.isGrounded)
                        {
                            self.targetComponent.target = self.FindTarget();
                        }
                    }
                }
            }
            orig(self);
        }

        private Transform MissileController_FindTarget(On.RoR2.Projectile.MissileController.orig_FindTarget orig, RoR2.Projectile.MissileController self)
        {
            if (self.teamFilter.teamIndex == TeamIndex.Player)
            {
                return orig(self);
            }

            self.search.searchOrigin = self.transform.position;
            self.search.searchDirection = self.transform.forward;
            self.search.teamMaskFilter.RemoveTeam(self.teamFilter.teamIndex);
            self.search.sortMode = BullseyeSearch.SortMode.Distance;
            self.search.RefreshCandidates();

            self.search.candidatesEnumerable = (from v in self.search.candidatesEnumerable.AsEnumerable<BullseyeSearch.CandidateInfo>()
                                        where !((v.hurtBox.healthComponent.body.characterMotor != null) 
                                            && v.hurtBox.healthComponent.body.characterMotor.isGrounded == true)
                                        select v).ToList();
            /*self.search.candidatesEnumerable = from v in self.search.candidatesEnumerable
                                        where !(v.hurtBox.transform.gameObject.GetComponent<CharacterMotor>()?.isGrounded ?? false)
                                        select v;*/

            HurtBox hurtBox = self.search.GetResults().FirstOrDefault<HurtBox>();
            if (hurtBox == null)
            {
                return null;
            }
            return hurtBox.transform;
        }

        public static float GetProcRate(DamageInfo damageInfo)
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos[ProcSolverPlugin.guid] == null)
            {
                return 1;
            }
            return _GetProcRate(damageInfo);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static float _GetProcRate(DamageInfo damageInfo)
        {
            float mod = ProcSolverPlugin.GetProcRateMod(damageInfo);
            return mod;
        }
    }
}