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

namespace RainrotSharedUtils.Difficulties
{
    public class MoreDifficultyStats
    {
        public enum StartingDifficulty
        {
            Easy = 0,
            Medium = 3,
            Hard = 6,
            VeryHard = 9
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
        private static bool _hooksEnabled = false;
        private static bool _tpContrasted = false;
        public static bool CompensateRewardsForDifficultyScaling = false;
        public static bool CompensateRewardsForDifficultyBoost = false;
        public static float BoostedRewardCompensationCoefficient = 0f;
        public static float GoldRewardMultiplierGlobal = 1f;
        public static float ExpRewardMultiplierGlobal = 1f;
        public static float DefaultTeleParticleRadius = 1f;
        private static bool _useDifficultyStats;
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

        public static MoreDifficultyStats cachedDifficultyStats { get; private set; } = null;
        public static MoreDifficultyStats GetMoreDifficultyStats(DifficultyDef difficulty)
        {
            if (difficulty == null)
                return null;
            return difficultyCustomStats.GetOrCreateValue(difficulty);
        }
        public static FixedConditionalWeakTable<DifficultyDef, MoreDifficultyStats> difficultyCustomStats = new FixedConditionalWeakTable<DifficultyDef, MoreDifficultyStats>();


        public static bool ValidateCachedDifficultyStats()
        {
            if (cachedDifficultyStats == null)
            {
                if (Run.instance == null || Run.instance.selectedDifficulty == DifficultyIndex.Invalid)
                    return false;
                cachedDifficultyStats = GetMoreDifficultyStats(DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty));
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
                cachedDifficultyStats = GetMoreDifficultyStats(DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty));
            }

            return cachedDifficultyStats.startingLevelBoost;
        }


        private static void DoBoostedTpContrast()
        {
            if (_tpContrasted)
                return;
            _tpContrasted = true;


            AssetReferenceT<Material> ref1 = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Teleporters.matTeleporterFresnelOverlay_mat);
            AssetAsyncReferenceManager<Material>.LoadAsset(ref1).Completed += (ctx) =>
            {
                Material mat = ctx.Result;

                mat.SetFloat("_SoftFactor", 2f);
                mat.SetFloat("_BrightnessBoost", 10.34f);
                mat.SetFloat("_AlphaBoost", 4.01f);
                mat.SetFloat("_AlphaBias", 0.05f);
                mat.SetFloat("_FresnelPower", 4.23f);
                mat.SetFloat("_VertexOffsetAmount", 0.18f);
            };
            //AssetReferenceT<Material> ref2 = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Teleporters.);
            //AssetAsyncReferenceManager<Material>.LoadAsset(ref2).Completed += (ctx) =>
            //{
            //    Material mat = ctx.Result;
            //
            //
            //};
        }
        private static void SetHooks()
        {
            if (_hooksEnabled)
                return;
            _hooksEnabled = true;

            On.RoR2.Run.OnRuleBookUpdated += CacheDifficultyStats;
            IL.RoR2.UI.DifficultyBarController.DoBarUpdates += CorrectDifficultyBar;
            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += AddDifficultyStats;
            On.RoR2.TeleporterInteraction.BaseTeleporterState.OnEnter += TeleporterParticleScale;
            IL.RoR2.TeleporterInteraction.ChargingState.OnEnter += CompensateBossCredits;

            ILHook goldRewardFix = new ILHook(typeof(DeathRewards).GetMethod("set_goldReward", (BindingFlags)(-1)), FixGoldRewards);
            ILHook expRewardFix = new ILHook(typeof(DeathRewards).GetMethod("set_expReward", (BindingFlags)(-1)), FixExpRewards);
        }

        private static void CompensateBossCredits(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<Run>(nameof(Run.compensatedDifficultyCoefficient))
                );
            if (!b)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(CompensateBossCredits));
                return;
            }
            c.EmitDelegate<Func<float, float>>((difficultyCoefficient) =>
            {
                if (!ValidateCachedDifficultyStats() || !cachedDifficultyStats.compensateBossCredits)
                    return difficultyCoefficient;

                return difficultyCoefficient - cachedDifficultyStats.startingDifficultyCoefficientBoost;
            });
        }

        [SystemInitializer(typeof(CombatDirector))]
        private static void FixEliteSpawn()
        {
            if (!UseDifficultyStats)
                return;

            foreach (CombatDirector.EliteTierDef etd in CombatDirector.eliteTiers) //EliteAPI.VanillaEliteTiers)//
            {
                List<EliteDef> eliteDefs = etd.eliteTypes.ToList();
                if (etd.eliteTypes.Contains(DLC2Content.Elites.Aurelionite))
                {
                    etd.isAvailable = (SpawnCard.EliteRules rules) => 
                        CombatDirector.NotEliteOnlyArtifactActive() && rules == SpawnCard.EliteRules.Default && IsPastMinimumStage(false);
                }
                if (etd.eliteTypes.Contains(DLC2Content.Elites.AurelioniteHonor))
                {
                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                        CombatDirector.IsEliteOnlyArtifactActive() && IsPastMinimumStage(false);
                }
                if (etd.eliteTypes.Contains(RoR2Content.Elites.Poison) || etd.eliteTypes.Contains(RoR2Content.Elites.Haunted))
                {
                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                        rules == SpawnCard.EliteRules.Default
                        && IsPastMinimumStage(true);
                }
            }

            bool IsPastMinimumStage(bool isTier2)
            {
                int minStage = isTier2 ? 4 : 2;

                if (ValidateCachedDifficultyStats())
                {
                    minStage = (isTier2 ? cachedDifficultyStats.tier2EliteStage : cachedDifficultyStats.tier1AndHalfEliteStage) - 1;
                }
                return Run.instance.stageClearCount >= minStage;
            }
        }

        private static void CacheDifficultyStats(On.RoR2.Run.orig_OnRuleBookUpdated orig, Run self, NetworkRuleBook networkRuleBookComponent)
        {
            orig(self, networkRuleBookComponent);
            cachedDifficultyStats = GetMoreDifficultyStats(DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty));
            if (cachedDifficultyStats.ambientLevelCap != -1)
                Run.ambientLevelCap = cachedDifficultyStats.ambientLevelCap;
            else
                Run.ambientLevelCap = 99;
        }
        private static void TeleporterParticleScale(On.RoR2.TeleporterInteraction.BaseTeleporterState.orig_OnEnter orig, RoR2.TeleporterInteraction.BaseTeleporterState self)
        {
            orig(self);

            if (!ValidateCachedDifficultyStats())
                return;
            float particleScale = cachedDifficultyStats.teleporterParticleRangeMultiplier;

            TeleporterInteraction component = self.GetComponent<TeleporterInteraction>();
            bool flag5 = component && component.modelChildLocator;
            if (flag5)
            {
                Transform transform = component.transform.Find("TeleporterBaseMesh/BuiltInEffects/PassiveParticle, Sphere");
                if (transform)
                {
                    //Debug.Log(transform.localScale);
                    if (particleScale <= 0)
                        transform.gameObject.SetActive(false);
                    else
                    {
                        transform.gameObject.SetActive(true);
                        transform.localScale = Vector3.one * DefaultTeleParticleRadius * particleScale;
                    }
                }
            }
        }

        private static void FixGoldRewards(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<uint, uint>>((money) =>
            {
                float compensated = GetCompensatedDifficultyFraction();
                float value = money * compensated * GoldRewardMultiplierGlobal;
                uint valueFloored = (uint)Mathf.FloorToInt(value);
                if (Util.CheckRoll0To1(value - valueFloored))
                    valueFloored += 1;
                return valueFloored;
            });
            c.Emit(OpCodes.Starg, 1);
        }
        private static void FixExpRewards(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<uint, uint>>((exp) =>
            {
                float compensated = GetCompensatedDifficultyFraction();
                return (uint)Mathf.CeilToInt(exp * compensated * ExpRewardMultiplierGlobal);
            });
            c.Emit(OpCodes.Starg, 1);
        }

        private static void AddDifficultyStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            AddAmbientLevelBoost(c);
            c.Index = 0;
            AddDifficultyCoefficientBoost(c);
        }

        private static void CorrectDifficultyBar(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<Run>("get_ambientLevel"));
            if (!b)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(CorrectDifficultyBar));
                return;
            }
            c.EmitDelegate<Func<float, float>>((levelIn) =>
            {
                if (!ValidateCachedDifficultyStats())
                {
                    return levelIn;
                }
                return levelIn + cachedDifficultyStats.startingDifficultyDisplay - cachedDifficultyStats.startingLevelBoost;
            });
        }

        private static void AddDifficultyCoefficientBoost(ILCursor c)
        {
            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<RoR2.Run>(nameof(Run.compensatedDifficultyCoefficient))
                );
            if (!b1)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(AddDifficultyCoefficientBoost), 1);
                return;
            }
            c.EmitDelegate<Func<float, float>>((compensatedDifficultyCoefficient) =>
            {
                if (!ValidateCachedDifficultyStats())
                {
                    return compensatedDifficultyCoefficient;
                }
                return compensatedDifficultyCoefficient + cachedDifficultyStats.startingDifficultyCoefficientBoost;
            });
        }

        private static void AddAmbientLevelBoost(ILCursor c)
        {
            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<RoR2.Run>("set_ambientLevel")
                );
            if (!b1)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(AddAmbientLevelBoost));
                return;
            }
            c.EmitDelegate<Func<float, float>>((ambientLevel) =>
            {
                if (!ValidateCachedDifficultyStats())
                {
                    return ambientLevel;
                }
                return ambientLevel + cachedDifficultyStats.startingLevelBoost;
            });
        }
    }
}
