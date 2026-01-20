using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.ContentManagement;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RainrotSharedUtils.Difficulties.DifficultyStatHooks;

namespace RainrotSharedUtils.Difficulties
{
    public class MoreDifficultyStats
    {
        public MoreDifficultyStats(DifficultyIndex difficultyIndex)
        {
            this.difficultyIndex = difficultyIndex;
        }
        public DifficultyIndex difficultyIndex { get; private set;} = DifficultyIndex.Invalid;
        public enum StartingDifficulty
        {
            Easy = 0,
            Medium = 3,
            Hard = 6,
            VeryHard = 9,
            Insane = 12
        }
        /// <summary>
        /// Use MoreDifficultyStats.StartingDifficulty enum for shorthand
        /// </summary>
        public float startingDifficultyDisplay = 0;
        public float startingLevelBoost = 0;
        public float startingDifficultyCoefficientBoost = 0;
        public bool compensateBossCredits = true;
        public int ambientLevelCap = -1;

        public int tier2EliteStage = 6;
        public int tier1AndHalfEliteStage = 3;
        public float teleporterParticleRangeMultiplier = 1;

        public float desiredStormTime_ForSwanSong = -1;
        public float desiredStormWarningTime_ForSwanSong = -1;
        public bool delayFirstStorm_ForSwanSong = true;
        public float stormIntensifyStrength_ForSwanSong = -1f;
    }
    public static class DifficultyUtilsModule
    {
        internal static bool _hooksEnabled = false;
        internal static bool _tpContrasted = false;
        public static bool CompensateRewardsForDifficultyScaling = false;
        public static bool CompensateRewardsForDifficultyBoost = false;
        public static float BoostedRewardCompensationCoefficient = 0f;
        public static float GoldRewardMultiplierGlobal = 1f;
        public static float ExpRewardMultiplierGlobal = 1f;
        public static float DefaultTeleParticleRadius = 1f;
        internal static bool _useDifficultyStats;
        public static bool UseDifficultyStats
        {
            get
            {
                return _useDifficultyStats;
            }
            set
            {
                if (value == true)
                    SetHooks();
                _useDifficultyStats = value;
            }
        }
        private static bool _boostTeleporterContrast;
        public static bool BoostTeleporterContrast
        {
            get
            {
                return _boostTeleporterContrast;
            }
            set
            {
                if (value == true)
                    DoBoostedTpContrast();
                _boostTeleporterContrast = value;
            }
        }

        public static void EnableAll()
        {
            UseDifficultyStats = true;
            BoostTeleporterContrast = true;
            CompensateRewardsForDifficultyScaling = true;
            CompensateRewardsForDifficultyBoost = true;
        }

        public static Dictionary<DifficultyIndex, MoreDifficultyStats> difficultyCustomStats = new Dictionary<DifficultyIndex, MoreDifficultyStats>();
        public static MoreDifficultyStats GetMoreDifficultyStats(DifficultyIndex difficulty)
        {
            if (difficultyCustomStats.ContainsKey(difficulty))
                return difficultyCustomStats[difficulty];

            MoreDifficultyStats stats = new MoreDifficultyStats(difficulty);
            difficultyCustomStats.Add(difficulty, stats);
            return stats;
        }

        public static MoreDifficultyStats cachedDifficultyStats { get; internal set; } = null;
        public static bool ValidateCachedDifficultyStats()
        {
            bool cacheIsNotNull = cachedDifficultyStats != null;

            if (Run.instance == null)
                return cacheIsNotNull;

            DifficultyIndex selectedDifficulty = Run.instance.selectedDifficulty;
            if (selectedDifficulty == DifficultyIndex.Invalid || selectedDifficulty == DifficultyIndex.Count)
                return cacheIsNotNull;

            if (!cacheIsNotNull /*cache is null*/ || cachedDifficultyStats.difficultyIndex != Run.instance.selectedDifficulty)
            {
                cachedDifficultyStats = GetMoreDifficultyStats(Run.instance.selectedDifficulty);
            }
            return true;
        }
        public static float GetCompensatedDifficultyFraction()
        {
            if (!UseDifficultyStats)
            {
                return 1;
            }
            float entryDiffCoeff = GetCompensatedStageEntryDifficulty();

            return (1 + entryDiffCoeff) / (1 + Run.instance.compensatedDifficultyCoefficient);

            float GetCompensatedStageEntryDifficulty()
            {
                if (!CompensateRewardsForDifficultyScaling && !CompensateRewardsForDifficultyBoost)
                    return Run.instance.compensatedDifficultyCoefficient;

                float entryDiffCoeff = CompensateRewardsForDifficultyScaling ? Stage.instance.entryDifficultyCoefficient : Run.instance.difficultyCoefficient;

                if (CompensateRewardsForDifficultyBoost && BoostedRewardCompensationCoefficient == 1)
                    return entryDiffCoeff;

                if (!ValidateCachedDifficultyStats())
                {
                    return entryDiffCoeff;
                }

                float compensation = CompensateRewardsForDifficultyBoost ? BoostedRewardCompensationCoefficient : 0;

                return entryDiffCoeff + cachedDifficultyStats.startingDifficultyCoefficientBoost * (1 - compensation);
            }
        }

        public static float GetAmbientLevelBoost()
        {
            if (!UseDifficultyStats)
            {
                return 1;
            }

            if (cachedDifficultyStats == null)
            {
                if (Run.instance == null || Run.instance.selectedDifficulty == DifficultyIndex.Invalid)
                    return 0;
                cachedDifficultyStats = GetMoreDifficultyStats(Run.instance.selectedDifficulty);
            }

            return cachedDifficultyStats.startingLevelBoost;
        }
        private static void SetHooks()
        {
            if (_hooksEnabled)
                return;
            _hooksEnabled = true;

            Run.onRunSetRuleBookGlobal += CacheDifficultyStats;
            IL.RoR2.UI.DifficultyBarController.DoBarUpdates += CorrectDifficultyBar;
            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += RecalculateDifficultyCoefficient_DifficultyStats;
            On.RoR2.TeleporterInteraction.BaseTeleporterState.OnEnter += TeleporterParticleScale;
            IL.RoR2.TeleporterInteraction.ChargingState.OnEnter += CompensateBossCredits;

            ILHook goldRewardFix = new ILHook(typeof(DeathRewards).GetMethod("set_goldReward", (BindingFlags)(-1)), FixGoldRewards);
            ILHook expRewardFix = new ILHook(typeof(DeathRewards).GetMethod("set_expReward", (BindingFlags)(-1)), FixExpRewards);
        }
    }
}
