using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RainrotSharedUtils.Components;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
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
        internal static bool _hooksEnabledForceElite = false;
        internal static bool _tpContrasted = false;
        public static bool CompensateRewardsForDifficultyScaling = false;
        public static bool CompensateRewardsForDifficultyBoost = false;
        public static float BoostedRewardCompensationCoefficient = 0f;
        public static float GoldRewardMultiplierGlobal = 1f;
        public static float ExpRewardMultiplierGlobal = 1f;
        public static float DefaultTeleParticleRadius = 1f;
        /// <summary>
        /// Sets these to true:
        /// UseDifficultyStats, UseForceElite, BoostTeleporterContrast, CompensateRewardsForDifficultyScaling, CompensateRewardsForDifficultyBoost
        /// </summary>
        public static void EnableAll()
        {
            UseDifficultyStats = true;
            UseForceElite = true;
            BoostTeleporterContrast = true;
            DisplayCurrentStageTime = true;
            CompensateRewardsForDifficultyScaling = true;
            CompensateRewardsForDifficultyBoost = true;
        }
        #region visual enhancements
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
        private static bool _displayCurrentStageTime;
        public static bool DisplayCurrentStageTime
        {
            get
            {
                return _displayCurrentStageTime;
            }
            set
            {
                if (value == true)
                    AddCurrentStageTimer();
                _displayCurrentStageTime = value;
            }
        }

        private static bool currentStageTimerAdded = false;
        internal static void AddCurrentStageTimer()
        {
            if (currentStageTimerAdded == true)
                return;
            currentStageTimerAdded = true;

            SharedUtilsPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ClassicRun.ClassicRunInfoHudPanel_prefab, (runInfoHudPanel) =>
            {
                Transform timerPanel = runInfoHudPanel.transform.GetChild(0);
                if(timerPanel != null)
                {
                    Transform wormGear = timerPanel.transform.GetChild(0);
                    if(wormGear != null)
                    {
                        wormGear.gameObject.SetActive(false);
                    }
                    Transform timerText1 = timerPanel.transform.GetChild(1);
                    if(timerText1 != null)
                    {
                        GameObject timerText2 = timerText1.gameObject.InstantiateClone("RunTimerText");
                        timerText1.transform.localPosition = new Vector3(-40.5f, 0, 0f);
                        timerText2.transform.parent = timerPanel;
                        timerText2.transform.localPosition = new Vector3(-38f, -14.5f, 0f);
                        if(timerText2.TryGetComponent(out HGTextMeshProUGUI tmp))
                        {
                            tmp.fontSizeMin = 10;
                            tmp.fontSize = 15;
                        }
                        if(timerText2.TryGetComponent(out RunTimerUIController runTimerUIController))
                        {
                            StageTimerUIController stageTimerUIController = timerText2.gameObject.AddComponent<StageTimerUIController>();
                            stageTimerUIController.timerTextController = runTimerUIController.runStopwatchTimerTextController;
                            if(timerText1.TryGetComponent(out RunTimerUIController other))
                            {
                                stageTimerUIController.otherTimerUiController = other;
                            }

                            runTimerUIController.enabled = false;
                        }
                    }
                }
            });
        }
        #endregion

        #region force elite
        internal static bool _useForceElite;
        public static bool UseForceElite
        {
            get
            {
                return _useForceElite;
            }
            set
            {
                if (value == true)
                    SetHooksForceElite();
                _useForceElite = value;
            }
        }
        private static void SetHooksForceElite()
        {
            if (_hooksEnabledForceElite)
                return;
            _hooksEnabledForceElite = true;

            IL.RoR2.Artifacts.EliteOnlyArtifactManager.PromoteIfHonor += OverridePromoteIfHonor;
            IL.RoR2.Artifacts.EliteOnlyArtifactManager.PromoteIfHonorAndApplyStats += OverridePromoteIfHonor;
            //On.RoR2.CombatDirector.PrepareNewMonsterWave += ForceEliteMonsterWave;
            //On.RoR2.CombatDirector.ResetEliteType += ForceEliteType;
            //On.RoR2.CombatDirector.AttemptSpawnOnTarget += ForceEliteSpawn;

            //if (!_hooksEnabled)
            //    On.RoR2.CombatDirector.Spawn += ForceSpawnToBeElite;
            On.RoR2.BossGroup.OnMemberDiscovered += ForceEliteBossGroup;
        }

        internal static bool forceNextSpawnAsElite;
        private static event ForceEliteMasterEventHandler _forceEliteMasterProvider;
        /// <summary>
        /// Use this one. Called on spawn using master
        /// </summary>
        public static event ForceEliteMasterEventHandler ForceEliteMasterProvider
        {
            add
            {
                if (_forceEliteMasterProvider == null)
                {
                    _forceEliteMasterProvider = new ForceEliteMasterEventHandler(value);
                    return;
                }
                _forceEliteMasterProvider += value;
            }
            remove
            {
                _forceEliteMasterProvider -= value;
            }
        }

        public delegate bool ForceEliteMasterEventHandler(CharacterMaster sender);

        private static event ForceEliteSpawnEventHandler _forceEliteSpawnProvider;
        /// <summary>
        /// Not functional. For use with spawn cards
        /// </summary>
        public static event ForceEliteSpawnEventHandler ForceEliteSpawnProvider
        {
            add
            {
                if (_forceEliteSpawnProvider == null)
                {
                    _forceEliteSpawnProvider = new ForceEliteSpawnEventHandler(value);
                    return;
                }
                _forceEliteSpawnProvider += value;
            }
            remove
            {
                _forceEliteSpawnProvider -= value;
            }
        }
        public delegate bool ForceEliteSpawnEventHandler(CharacterSpawnCard card);

        public static bool IsForceEliteTrueForMaster(CharacterMaster sender)
        {
            if (!_hooksEnabledForceElite)
                return false;
            if (sender == null)
                return false;

            foreach (ForceEliteMasterEventHandler feeh in _forceEliteMasterProvider.GetInvocationList())
            {
                if (feeh.Invoke(sender))
                    return true;
            }

            return false;
        }
        public static bool IsForceEliteTrueForSpawncard(CharacterSpawnCard card)
        {
            if (!_hooksEnabledForceElite)
                return false;
            if (card == null)
                return false;

            foreach (ForceEliteSpawnEventHandler feeh in _forceEliteSpawnProvider.GetInvocationList())
            {
                if (feeh.Invoke(card))
                    return true;
            }

            return false;
        }

        private static bool ForceSpawnToBeElite(On.RoR2.CombatDirector.orig_Spawn orig, CombatDirector self, SpawnCard spawnCard, EliteDef eliteDef, Transform spawnTarget, DirectorCore.MonsterSpawnDistance spawnDistance, bool preventOverhead, float valueMultiplier, DirectorPlacementRule.PlacementMode placementMode, bool singleScaledBoss)
        {
            bool b = orig(self, spawnCard, eliteDef, spawnTarget, spawnDistance, preventOverhead, valueMultiplier, placementMode, singleScaledBoss);
            //if(eliteDef == null && (spawnCard as CharacterSpawnCard).noElites == false)
            //    RoR2.Artifacts.EliteOnlyArtifactManager.PromoteIfHonor(memberMaster, Run.instance.spawnRng);

            return b;
        }
        #endregion

        #region difficulty stats
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
        private static void SetHooks()
        {
            if (_hooksEnabled)
                return;
            _hooksEnabled = true;

            On.RoR2.Run.OnRuleBookUpdated += CacheDifficultyStats;
            IL.RoR2.UI.DifficultyBarController.DoBarUpdates += CorrectDifficultyBar;
            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += RecalculateDifficultyCoefficient_DifficultyStats;
            On.RoR2.TeleporterInteraction.BaseTeleporterState.OnEnter += TeleporterParticleScale;
            IL.RoR2.TeleporterInteraction.ChargingState.OnEnter += CompensateBossCredits;

            ILHook goldRewardFix = new ILHook(typeof(DeathRewards).GetMethod("set_goldReward", (BindingFlags)(-1)), FixGoldRewards);
            ILHook expRewardFix = new ILHook(typeof(DeathRewards).GetMethod("set_expReward", (BindingFlags)(-1)), FixExpRewards);
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
        #endregion
    }
}
