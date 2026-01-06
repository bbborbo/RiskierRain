using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using R2API.Utils;
using RainrotSharedUtils.Difficulties;
using RainrotSharedUtils.Frost;
using RainrotSharedUtils.Shelters;
using RoR2;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace RainrotSharedUtils.Compat
{
    //[BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(RainrotSharedUtils.SharedUtilsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(Inferno.Main.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI))]
    public class SharedUtilsCompatPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "RainrotSharedCompats";
        public const string version = "1.0.0";
        #endregion
        public static bool ModLoaded(string modGuid) { return modGuid != "" && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid); }
        public static bool infernoLoaded => ModLoaded(Inferno.Main.PluginGUID);

        void Awake()
        {
            if (infernoLoaded)
                DoInfernoCompat();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void DoInfernoCompat()
        {
            DifficultyDef difficultyDef = Inferno.Main.InfernoDiffDef;

            MoreDifficultyStats infernoStats = DifficultyUtilsModule.GetMoreDifficultyStats(difficultyDef);
            infernoStats.startingLevelBoost = Inferno.Main.LevelDiffBoost.Value;
            infernoStats.startingDifficultyDisplay = Inferno.Main.LevelDiffBoost.Value;
            infernoStats.ambientLevelCap = int.MaxValue;
            infernoStats.tier2EliteStage = 4;
            infernoStats.tier1AndHalfEliteStage = 2;
            infernoStats.delayFirstStorm_ForSwanSong = false;
            infernoStats.desiredStormTime_ForSwanSong = 3.5f;
            infernoStats.desiredStormWarningTime_ForSwanSong = 1f;
            infernoStats.stormIntensifyStrength_ForSwanSong = 0.6f;

            DifficultyUtilsModule.UseDifficultyStats = true;
            DifficultyUtilsModule.CompensateRewardsForDifficultyBoost = true;

            //Run.onRunSetRuleBookGlobal -= Inferno.Main.;
            Run.onRunStartGlobal += RemoveInfernoHooks;
        }

        private static void RemoveInfernoHooks(Run obj)
        {
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal -= Inferno.Skill_Misc.Hooks.AmbientLevelBoost; 
        }

        public delegate bool orig_ChangeAmbientCap(Inferno.Main main, Run run, RuleBook ruleBook);
        public static void InfernoAmbientCap(orig_ChangeAmbientCap orig, Inferno.Main main, Run run, RuleBook ruleBook)
        {
            //Hook infernoAmbientCap = new Hook(
            //  typeof(Inferno.Main).GetMethod(nameof(Inferno.Main.ChangeAmbientCap), (BindingFlags)(-1)),
            //  typeof(SharedUtilsCompatPlugin).GetMethod(nameof(InfernoAmbientCap), (BindingFlags)(-1))
            //);
        }

    }
}
