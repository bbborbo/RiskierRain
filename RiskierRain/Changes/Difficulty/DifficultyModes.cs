using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using RainrotSharedUtils.Difficulties;
using UnityEngine.AddressableAssets;
using static MoreStats.StatHooks;
using RiskierRain.Changes.Components;

namespace RiskierRain.Changes
{
    public static partial class DifficultyChanges
    {
        #region difficulty stats
        public static float drizzleDifficultyBoost = 0;
        public static float rainstormDifficultyBoost = 0;
        public static float monsoonDifficultyBoost = 3;
        public static float eclipseDifficultyBoost = 6;

        public static int ambientLevelCapDrizzle = 99;//99
        public static int ambientLevelCap = 999;//99

        public static float easyTeleParticleRadius = 1f;
        public static float normalTeleParticleRadius = 0.8f;
        public static float hardTeleParticleRadius = 0.4f;
        public static float eclipseTeleParticleRadius = 0.4f;
        public static float defaultTeleParticleRadius = 0.9f;

        public static int Tier2EliteMinimumStageDefault = 6;
        public static int Tier2EliteMinimumStageDrizzle = 11;
        public static int Tier2EliteMinimumStageRainstorm = 6;
        public static int Tier2EliteMinimumStageMonsoon = 4;
        public static int Tier2EliteMinimumStageEclipse = 4;
        public static void AddDifficultyStats()
        {
            for (int i = (int)DifficultyIndex.Easy; i < (int)DifficultyIndex.Count; i++)
            {
                DifficultyIndex difficultyIndex = (DifficultyIndex)i;
                DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(difficultyIndex);
                MoreDifficultyStats.StartingDifficulty startingDifficulty = 0;
                float levelBoost = 0;
                float difficultyBoost = difficultyDef.scalingValue - 1;
                int levelCap = ambientLevelCap;
                int tier2Stage = 5;
                float teleParticleRangeMultiplier = 1;

                switch (difficultyIndex)
                {
                    case DifficultyIndex.Easy:
                        startingDifficulty = MoreDifficultyStats.StartingDifficulty.Easy;
                        levelBoost = drizzleDifficultyBoost;
                        tier2Stage = Tier2EliteMinimumStageDrizzle;
                        teleParticleRangeMultiplier = easyTeleParticleRadius;
                        levelCap = ambientLevelCapDrizzle;
                        break;
                    case DifficultyIndex.Normal:
                        startingDifficulty = MoreDifficultyStats.StartingDifficulty.Medium;
                        levelBoost = rainstormDifficultyBoost;
                        tier2Stage = Tier2EliteMinimumStageRainstorm;
                        teleParticleRangeMultiplier = normalTeleParticleRadius;
                        break;
                    case DifficultyIndex.Hard:
                        startingDifficulty = MoreDifficultyStats.StartingDifficulty.Hard;
                        levelBoost = monsoonDifficultyBoost;
                        tier2Stage = Tier2EliteMinimumStageMonsoon;
                        teleParticleRangeMultiplier = hardTeleParticleRadius;
                        break;
                    //assumes eclipse
                    default:
                        startingDifficulty = MoreDifficultyStats.StartingDifficulty.Hard;
                        levelBoost = monsoonDifficultyBoost;
                        tier2Stage = Tier2EliteMinimumStageMonsoon;
                        teleParticleRangeMultiplier = hardTeleParticleRadius;
                        break;
                }

                MoreDifficultyStats difficultyStats = DifficultyUtilsModule.GetMoreDifficultyStats(difficultyIndex);
                difficultyStats.startingDifficultyDisplay = (float)startingDifficulty;
                difficultyStats.startingLevelBoost = levelBoost;
                difficultyStats.startingDifficultyCoefficientBoost = difficultyBoost;
                difficultyStats.ambientLevelCap = levelCap;
                difficultyStats.tier2EliteStage = tier2Stage;
                difficultyStats.teleporterParticleRangeMultiplier = teleParticleRangeMultiplier;
                DifficultyUtilsModule.difficultyCustomStats[difficultyIndex] = difficultyStats;
            }
        }
        #endregion
        #region monsoon-exclusive
        public static void AddMonsoonScalingStats()
        {
            monsoonDesc += $"\n>Enemies gain <style=cIsHealth>enhanced scaling</style></style>";

            GetStatCoefficients += MonsoonPlusStatBuffs2;
        }
        private static void MonsoonPlusStatBuffs2(CharacterBody sender, StatHookEventArgs args)
        {
            DifficultyIndex selectedDifficulty = Run.instance.selectedDifficulty;
            float ambientLevelBoost = GetAmbientLevelBoost();
            if (sender.teamComponent.teamIndex != TeamIndex.Player)
            {
                if (selectedDifficulty >= DifficultyIndex.Hard)
                {
                    float compensatedLevel = sender.level - ambientLevelBoost;
                    float clamped = Mathf.Clamp01(compensatedLevel / 300f);

                    float attackSpeedFactor =
                        (sender.baseNameToken == "CLAYBRUISER_BODY_NAME"
                            || sender.baseNameToken == "LUNARWISP_BODY_NAME"
                            || sender.baseNameToken == "JELLYFISH_BODY_NAME")
                        ? 2f : 4f;
                    args.attackSpeedMultAdd += clamped * attackSpeedFactor;

                    if (sender.isChampion)
                    {
                        args.armorAdd += 3 * compensatedLevel;
                    }
                    else
                    {
                        args.moveSpeedMultAdd += clamped * 2f;
                    }
                }
            }
        }
        #endregion

        #region eclipse-exclusive
        public static DifficultyIndex eclipseLevelBossElite = DifficultyIndex.Eclipse1; //NA
        public static string eclipseOneDesc =
            $"\n<mspace=0.5em>(1)</mspace> Boss Enemies: <style=cIsHealth>Always Elite</style>";

        public static DifficultyIndex eclipseLevelEnemyMspd = DifficultyIndex.Eclipse2; //4
        public static float eclipseEnemyMspd = 0.3f; //0.3f
        public static string eclipseTwoDesc =
            $"\n<mspace=0.5em>(2)</mspace> Enemy Speed: <style=cIsHealth>+{eclipseEnemyMspd.AsPercent()}</style>";

        public static DifficultyIndex eclipseLevelSmallHoldout = DifficultyIndex.Eclipse3; //NA
        public static DifficultyIndex eclipseLevelHoldoutLoss = DifficultyIndex.Eclipse3; //2
        public static float eclipseHoldoutLossRate = 0.02f; //pillar of soul is 10%
        public static float eclipseHoldoutScale = 0.6f; //0.5f
        public static string eclipseThreeDesc =
            $"\n<mspace=0.5em>(3)</mspace> All Holdout Zones are <style=cIsHealth>Eclipsed</style>";
        //$"\n<mspace=0.5em>(3)</mspace> Enemy Cooldowns: <style=cIsHealth>-{Tools.ConvertDecimal(eclipseEnemyCdr)}</style>";

        public static DifficultyIndex eclipseHealingLoss = DifficultyIndex.Eclipse4; //5
        public static float eclipseHealingMultiplier = 0.75f;
        public static string eclipseFourDesc =
            $"\n<mspace=0.5em>(4)</mspace> Ally Healing: <style=cIsHealth>-{(1 - eclipseHealingMultiplier).AsPercent()}</style>";

        public static DifficultyIndex eclipseLevelEnemyCdr = DifficultyIndex.Eclipse5; //7
        public static float eclipseEnemyCooldownScale = 0.6f; //0.5f
        public static string eclipseFiveDesc =
            $"\n<mspace=0.5em>(5)</mspace> Enemy Cooldowns: <style=cIsHealth>-{(1 - eclipseEnemyCooldownScale).AsPercent()}</style>";

        public static DifficultyIndex eclipseLevelSpiteArtifact = DifficultyIndex.Eclipse6; //
        public static string eclipseSixDesc =
            $"\n<mspace=0.5em>(6)</mspace> On Kill: <style=cIsHealth>Enemies drop exploding bombs</style>";

        public static DifficultyIndex eclipseLevelItemTax = DifficultyIndex.Eclipse7; //
        public static int eclipseItemTaxCount = 2;
        public static float eclipseItemTaxPercent = 0.2f;
        public static string eclipseSevenDesc =
            $"\n<mspace=0.5em>(7)</mspace> Item Tax: <style=cIsHealth>{eclipseItemTaxPercent.AsPercent()} per Stage</style>";

        public static string eclipseEightDesc =
            $"\n<mspace=0.5em>(8)</mspace> Allies recieve <style=cIsHealth>permanent damage</style>";
        public static void ChangeEclipse()
        {
            //remove old stuff
            IL.RoR2.CharacterMaster.OnBodyStart += (ctx) => RemoveEclipseEffect(ctx, "E1"); //lv1 starting health
            IL.RoR2.GlobalEventManager.OnCharacterHitGroundServer += (ctx) => RemoveEclipseEffect(ctx, "E3"); //lv3 frailty
            IL.RoR2.HealthComponent.Heal += (ctx) => RemoveEclipseEffect(ctx, "E5");//lv5 healing
            IL.RoR2.DeathRewards.OnKilledServer += (ctx) => RemoveEclipseEffect(ctx, "E6");//lv6 gold drops
            //IL.RoR2.HealthComponent.TakeDamageProcess += RemoveEclipseEffect;//lv8 eclipse curse :skull:

            IL.RoR2.CharacterBody.RecalculateStats += RemoveEclipseStats; //lv4 enemy speed lv7 enemy cooldowns

            //new stuff
            DifficultyUtilsModule.ForceEliteMasterProvider += EclipseForceEliteMaster;
            //DifficultyUtilsModule.ForceEliteSpawnProvider += EclipseForceEliteSpawn;
            //GetStatCoefficients += this.EclipseStatBuffs;
            GetMoreStatCoefficients += EclipseStatBuffs2;
            //On.RoR2.CharacterBody.RecalculateStats += this.EclipseCdr;
            On.RoR2.RunArtifactManager.SetArtifactEnabled += EclipseSpiteArtifact;
            IL.RoR2.HoldoutZoneController.DoUpdate += EclipseHoldoutScale;
            On.RoR2.HoldoutZoneController.Start += EclipseHoldoutDischarge;
            Stage.onServerStageBegin += EclipseItemTax;


            string eclipse8Prefix = "\"You only celebrate in the light... because I allow it.\" \n\n";
            string eclipseStart = "Starts at baseline Monsoon difficulty.<style=cSub>\n";
            string eclipseEnd = "</style>";

            LanguageAPI.Add("ECLIPSE_1_DESCRIPTION", eclipseStart + eclipseOneDesc + eclipseEnd);
            LanguageAPI.Add("ECLIPSE_2_DESCRIPTION", eclipseStart + eclipseOneDesc + eclipseTwoDesc + eclipseEnd);
            LanguageAPI.Add("ECLIPSE_3_DESCRIPTION", eclipseStart + eclipseOneDesc + eclipseTwoDesc + eclipseThreeDesc + eclipseEnd);
            LanguageAPI.Add("ECLIPSE_4_DESCRIPTION", eclipseStart + eclipseOneDesc + eclipseTwoDesc + eclipseThreeDesc + eclipseFourDesc + eclipseEnd);
            LanguageAPI.Add("ECLIPSE_5_DESCRIPTION", eclipseStart + eclipseOneDesc + eclipseTwoDesc + eclipseThreeDesc
                + eclipseFourDesc + eclipseFiveDesc + eclipseEnd);
            LanguageAPI.Add("ECLIPSE_6_DESCRIPTION", eclipseStart + eclipseOneDesc + eclipseTwoDesc + eclipseThreeDesc
                + eclipseFourDesc + eclipseFiveDesc + eclipseSixDesc + eclipseEnd);
            LanguageAPI.Add("ECLIPSE_7_DESCRIPTION", eclipseStart + eclipseOneDesc + eclipseTwoDesc + eclipseThreeDesc
                + eclipseFourDesc + eclipseFiveDesc + eclipseSixDesc + eclipseSevenDesc + eclipseEnd);
            LanguageAPI.Add("ECLIPSE_8_DESCRIPTION", eclipse8Prefix + eclipseStart + eclipseOneDesc + eclipseTwoDesc + eclipseThreeDesc
                + eclipseFourDesc + eclipseFiveDesc + eclipseSixDesc + eclipseSevenDesc + eclipseEightDesc + eclipseEnd);
        }
        private static void EclipseStatBuffs2(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.teamComponent.teamIndex != TeamIndex.Player || Run.instance.selectedDifficulty <= eclipseLevelItemTax)
                return;
            args.healingPercentIncreaseMult *= eclipseHealingMultiplier;
        }
        private static void EclipseItemTax(Stage obj)
        {
            //only tax items on players in difficulty levels at or higher than eclipse 7
            if (Run.instance.selectedDifficulty < eclipseLevelItemTax)
                return;
            //only tax items on stages, not hidden realms
            if (obj.sceneDef.sceneType != SceneType.Stage
                && obj.sceneDef.sceneType != SceneType.UntimedStage)
                return;

            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                if (master.teamIndex != TeamIndex.Player)
                    continue;
                if (master.inventory == null)
                    continue;

                EclipseItemTaxer taxer;
                if (!master.TryGetComponent(out taxer))
                {
                    taxer = master.gameObject.AddComponent<EclipseItemTaxer>();
                    taxer.master = master;
                }

                taxer.TaxItems();
            }
        }
        private static bool EclipseForceEliteSpawn(CharacterSpawnCard card)
        {
            if (!Run.instance || Run.instance.selectedDifficulty < eclipseLevelBossElite)
                return false;

            if (card.noElites)
                return false;

            if (card.prefab && card.prefab.TryGetComponent(out CharacterMaster master))
                return EclipseForceEliteMasterInternal(master);
            return false;
        }
        private static bool EclipseForceEliteMaster(CharacterMaster sender)
        {
            if (!Run.instance || Run.instance.selectedDifficulty < eclipseLevelBossElite)
                return false;
            if (sender.isBoss)
                return true;
            return EclipseForceEliteMasterInternal(sender);
        }
        private static bool EclipseForceEliteMasterInternal(CharacterMaster sender)
        {
            if (sender.bodyPrefab && sender.bodyPrefab.TryGetComponent(out CharacterBody body))
            {
                if (body.isChampion)
                    return true;
            }
            return false;
        }

        internal static void ChangeRequiredDifficultyLevelForStats(ILCursor c, DifficultyIndex difficulty, DifficultyIndex difficultyNew = DifficultyIndex.Count, float newFloatValue = -1)
        {
            c.Index = 0;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<RoR2.Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4((int)difficulty)
                );
            if (!b1)
            {
                DebugBreakpoint($"{nameof(ChangeRequiredDifficultyLevelForStats)}/{difficulty}", 1);
                return;
            }
            c.Index--;
            c.Next.Operand = (int)difficultyNew;

            if (newFloatValue == -1)
                return;

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _)
                );
            if (!b2)
            {
                DebugBreakpoint($"{nameof(ChangeRequiredDifficultyLevelForStats)}/{difficulty}", 2);
                return;
            }
            c.Next.Operand = newFloatValue;
        }

        private static void RemoveEclipseStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ChangeRequiredDifficultyLevelForStats(c, DifficultyIndex.Eclipse4, eclipseLevelEnemyMspd, eclipseEnemyMspd);

            ChangeRequiredDifficultyLevelForStats(c, DifficultyIndex.Eclipse7, eclipseLevelEnemyCdr, eclipseEnemyCooldownScale);
        }

        private static void RemoveEclipseEffect(ILContext il, string identifier)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<RoR2.Run>("get_selectedDifficulty")
                );
            if (!b)
            {
                DebugBreakpoint(nameof(RemoveEclipseEffect) + $"/{identifier}");
                return;
            }
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, (int)DifficultyIndex.Invalid);
        }

        private static void EclipseHoldoutDischarge(On.RoR2.HoldoutZoneController.orig_Start orig, HoldoutZoneController self)
        {
            if (Run.instance.selectedDifficulty >= eclipseLevelHoldoutLoss)
            {
                self.baseIndicatorColor = new Color(0.9f, 0.9f, 0.9f);
                self.dischargeRate = Mathf.Max(self.dischargeRate, eclipseHoldoutLossRate);
                if (NetworkServer.active)
                    Chat.ServerAttemptBroadcastChat("<style=cStack>The holdout zone is <style=cIsHealth>Eclipsed!</style></style>");
            }
            orig(self);
        }

        private static void EclipseHoldoutScale(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ChangeRequiredDifficultyLevelForStats(c, DifficultyIndex.Eclipse2, eclipseLevelSmallHoldout, eclipseHoldoutScale);
            return;

            int holdoutScaleLoc = 3;
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<RoR2.Run>("get_selectedDifficulty"),
                x => x.MatchLdcI4(out _)
                );
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, (int)eclipseLevelSmallHoldout);

            return;
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<RoR2.HealthComponent>("get_fullHealth")
                );
            //c.Remove();
            //c.Emit(OpCodes.Ldc_R4, eclipseHoldoutScale);
            c.Next.Operand = eclipseHoldoutScale;
        }

        private static void EclipseSpiteArtifact(On.RoR2.RunArtifactManager.orig_SetArtifactEnabled orig, RunArtifactManager self, ArtifactDef artifactDef, bool newEnabled)
        {
            if (Run.instance == null)
            {
                orig(self, artifactDef, newEnabled);
                return;
            }

            if (Run.instance.selectedDifficulty >= eclipseLevelSpiteArtifact)
            {
                if (artifactDef == RoR2Content.Artifacts.bombArtifactDef)
                    newEnabled = true;
            }

            orig(self, artifactDef, newEnabled);
        }
        #endregion
    }
}
