using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using R2API.Utils;
using RainrotSharedUtils.Components;
using RainrotSharedUtils.Difficulties;
using RainrotSharedUtils.Frost;
using RainrotSharedUtils.Shelters;
using RoR2;
using RoR2.Skills;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using static RainrotSharedUtils.Components.ElementCounter;

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
    [BepInDependency(SnowtimeToybox.SnowtimeToyboxMod.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Skillsmas.SkillsmasPlugin.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]

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
        public static bool ModLoaded(string modGuid) { return !modGuid.IsNullOrWhiteSpace() && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid); }
        //public static bool infernoLoaded => ModLoaded(Inferno.Main.PluginGUID);
        public static bool snowtimeLoaded => ModLoaded(SnowtimeToybox.SnowtimeToyboxMod.GUID);
        public static bool skillsmasLoaded => ModLoaded(Skillsmas.SkillsmasPlugin.PluginGUID);
        public static bool riskierLoaded => ModLoaded("com.RiskOfBrainrot.RiskierRain");

        void Awake()
        {
            //if (infernoLoaded)
            //    DoInfernoCompat();
            if (snowtimeLoaded)
                DoSnowtimeCompat();
            //if (skillsmasLoaded)
            //    DoSkillsmasCompat();
            //Run.onRunStartGlobal += OnSkillCatalogAvailable;
            //RoR2Application.onStart += OnSkillCatalogAvailable;
            SkillCatalog.skillsDefined.onAvailable += OnSkillCatalogAvailable;
        }

        private void OnSkillCatalogAvailable()
        {
            if(skillsmasLoaded == true)
            {
                Register("Skillsmas_FireWall", CounterElement.Fire);
                Register("Skillsmas_FlameBomb", CounterElement.Fire);
                Register("Skillsmas_CryoBolt"    , CounterElement.Ice);
                Register("Skillsmas_FrostBarrier", CounterElement.Ice);
                Register("Skillsmas_LightningPillar", CounterElement.Lightning);
                Register("Skillsmas_RockBolt"    , CounterElement.Earth);
                Register("Skillsmas_RockBomb"    , CounterElement.Earth);
                Register("Skillsmas_RockMeteor"  , CounterElement.Earth);
                Register("Skillsmas_RockPlatform", CounterElement.Earth);
                Register("Skillsmas_WaterBolt" , CounterElement.Water);
                Register("Skillsmas_WaterBomb" , CounterElement.Water);
                Register("Skillsmas_WaterCloud", CounterElement.Water);
                Register("Skillsmas_WaterWave" , CounterElement.Water);
                Register("Skillsmas_SpikyRoots", CounterElement.Plant);
                Register("Skillsmas_WalkingHealer", CounterElement.Robo);
            }
            void Register(string nameToken, CounterElement element)
            {
                int index = SkillCatalog.FindSkillIndexByName(nameToken);
                Debug.Log($"{nameToken} index {index}");
                if (index == -1)
                    return;
                SkillDef skill = SkillCatalog.GetSkillDef(index);
                if(skill != null)
                    ElementCounter.RegisterSkillElement(skill, element);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void DoSkillsmasCompat()
        {

        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void DoSnowtimeCompat()
        {
            DifficultyIndex difficultyDef = SnowtimeToybox.SnowtimeToyboxMod.SnowtimeLegendaryDiffIndex;

            MoreDifficultyStats legendaryStats = DifficultyUtilsModule.GetMoreDifficultyStats(difficultyDef);
            legendaryStats.startingLevelBoost = 9;
            legendaryStats.startingDifficultyCoefficientBoost = 0;
            legendaryStats.startingDifficultyDisplay = (float)MoreDifficultyStats.StartingDifficulty.Insane;
            legendaryStats.ambientLevelCap = int.MaxValue;
            legendaryStats.tier2EliteStage = 3;
            legendaryStats.tier1AndHalfEliteStage = 1;
            legendaryStats.delayFirstStorm_ForSwanSong = true;
            legendaryStats.desiredStormTime_ForSwanSong = 4f;
            legendaryStats.desiredStormWarningTime_ForSwanSong = 1f;
            legendaryStats.stormIntensifyStrength_ForSwanSong = 0.7f;
            DifficultyUtilsModule.difficultyCustomStats[difficultyDef] = legendaryStats;
        }

        //#region inferno
        //[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        //private static void DoInfernoCompat()
        //{
        //    DifficultyIndex difficultyDef = Inferno.Main.InfernoDiffIndex;
        //
        //    MoreDifficultyStats infernoStats = DifficultyUtilsModule.GetMoreDifficultyStats(difficultyDef);
        //    infernoStats.startingLevelBoost = Inferno.Main.LevelDiffBoost.Value;
        //    infernoStats.startingDifficultyDisplay = Inferno.Main.LevelDiffBoost.Value;
        //    infernoStats.ambientLevelCap = int.MaxValue;
        //    infernoStats.tier2EliteStage = 4;
        //    infernoStats.tier1AndHalfEliteStage = 2;
        //    infernoStats.delayFirstStorm_ForSwanSong = false;
        //    infernoStats.desiredStormTime_ForSwanSong = 3.5f;
        //    infernoStats.desiredStormWarningTime_ForSwanSong = 1f;
        //    infernoStats.stormIntensifyStrength_ForSwanSong = 0.6f;
        //    DifficultyUtilsModule.difficultyCustomStats[difficultyDef] = infernoStats;
        //
        //    DifficultyUtilsModule.CompensateRewardsForDifficultyBoost = true;
        //
        //    //Run.onRunSetRuleBookGlobal -= Inferno.Main.;
        //    Run.onRunStartGlobal += RemoveInfernoHooks;
        //}
        //
        //private static void RemoveInfernoHooks(Run obj)
        //{
        //    if(riskierLoaded)
        //        On.RoR2.Run.RecalculateDifficultyCoefficentInternal -= Inferno.Skill_Misc.Hooks.AmbientLevelBoost; 
        //}
        //
        //public delegate bool orig_ChangeAmbientCap(Inferno.Main main, Run run, RuleBook ruleBook);
        //public static void InfernoAmbientCap(orig_ChangeAmbientCap orig, Inferno.Main main, Run run, RuleBook ruleBook)
        //{
        //    //Hook infernoAmbientCap = new Hook(
        //    //  typeof(Inferno.Main).GetMethod(nameof(Inferno.Main.ChangeAmbientCap), (BindingFlags)(-1)),
        //    //  typeof(SharedUtilsCompatPlugin).GetMethod(nameof(InfernoAmbientCap), (BindingFlags)(-1))
        //    //);
        //}
        //#endregion
    }
}
