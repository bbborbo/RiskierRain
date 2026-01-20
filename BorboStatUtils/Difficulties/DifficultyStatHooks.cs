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
using static RainrotSharedUtils.Difficulties.DifficultyUtilsModule;

namespace RainrotSharedUtils.Difficulties
{
    internal static class DifficultyStatHooks
    {
        internal static void CacheDifficultyStats(On.RoR2.Run.orig_OnRuleBookUpdated orig, Run self, NetworkRuleBook networkRuleBookComponent)
        {
            cachedDifficultyStats = GetMoreDifficultyStats(networkRuleBookComponent.ruleBook.FindDifficulty());
            if (cachedDifficultyStats.ambientLevelCap != -1)
                Run.ambientLevelCap = cachedDifficultyStats.ambientLevelCap;
            else
                Run.ambientLevelCap = 99;
            orig(self, networkRuleBookComponent);
        }

        internal static void DoBoostedTpContrast()
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
                //i have no idea which ones right so im just trying everything
                mat.SetFloat("_OffsetAmount", 0.18f);
                mat.SetFloat("_OffsetAmt", 0.18f);
                mat.SetFloat("_VertexOffsetAmt", 0.18f);
            };
            //AssetReferenceT<Material> ref2 = new AssetReferenceT<Material>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Teleporters.);
            //AssetAsyncReferenceManager<Material>.LoadAsset(ref2).Completed += (ctx) =>
            //{
            //    Material mat = ctx.Result;
            //
            //
            //};
        }
        internal static void CompensateBossCredits(ILContext il)
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
        internal static void FixEliteSpawn()
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

        internal static void TeleporterParticleScale(On.RoR2.TeleporterInteraction.BaseTeleporterState.orig_OnEnter orig, RoR2.TeleporterInteraction.BaseTeleporterState self)
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

        internal static void FixGoldRewards(ILContext il)
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
        internal static void FixExpRewards(ILContext il)
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

        internal static void RecalculateDifficultyCoefficient_DifficultyStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            AddAmbientLevelBoost(c);
            c.Index = 0;
            AddDifficultyCoefficientBoost(c);
        }

        internal static void CorrectDifficultyBar(ILContext il)
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

        internal static void AddDifficultyCoefficientBoost(ILCursor c)
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

        internal static void AddAmbientLevelBoost(ILCursor c)
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
