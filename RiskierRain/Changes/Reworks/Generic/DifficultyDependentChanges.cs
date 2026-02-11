using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRain.Components;
using static R2API.RecalculateStatsAPI;
using static RiskierRain.CoreModules.StatHooks;
using EntityStates;
using BepInEx;
using R2API;
using System.Collections.ObjectModel;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RainrotSharedUtils.Difficulties;
using static MoreStats.StatHooks;
using RiskierRain.Changes.Components;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static float drizzleDifficultyBoost = 0;
        public static float rainstormDifficultyBoost = 0;
        public static float monsoonDifficultyBoost = 3;
        public static float eclipseDifficultyBoost = 6;

        /// <summary>
        /// linear. increases the difficulty by this amount per minute, affected by the difficulty's scaling value
        /// </summary>
        public static float baseScalingMultiplier = 0.8f; //1f
        /// <summary>
        /// exponential
        /// </summary>
        public static float difficultyIncreasePerMinutePerDifficulty = 0.01f; //0f
        /// <summary>
        /// exponential
        /// </summary>
        public static float difficultyIncreasePerMinuteBase = 1.0f; //1f
        /// <summary>
        /// exponential. increases the difficulty and difficulty scaling by this amount for each stach
        /// </summary>
        public static float difficultyIncreasePerStage = 0.9f; //1.15f, exponential
        /// <summary>
        /// exponential. works the same as difficultyIncreasePerStage, but only once per 5 stages
        /// </summary>
        public static float difficultyIncreasePerLoop = 1.3f; //1.0f, exponential
        public static float playerBaseDifficultyFactor = 0.2f;//0.3f, linear
        public static float playerScalingDifficultyFactor = 0.2f;//0.2f, exponential
        public static float playerSpawnRateFactor = 0.5f;//0.5f, linear
        public static float difficultySpawnRateFactor = 0.4f;//0.4f, additive
        public static int ambientLevelCapDrizzle = 99;//99
        public static int ambientLevelCap = 999;//99

        public static float easyTeleParticleRadius = 1f;
        public static float normalTeleParticleRadius = 0.8f;
        public static float hardTeleParticleRadius = 0.4f;
        public static float eclipseTeleParticleRadius = 0.4f;
        public static float defaultTeleParticleRadius = 0.9f;

        #region ambient level
        internal static float GetAmbientLevelBoost()
        {
            return DifficultyUtilsModule.GetAmbientLevelBoost();
        }
        void FreezeTimeScalingOnFinalLevels()
        {
            On.RoR2.Run.ShouldUpdateRunStopwatch += ModifyShouldUpdateRunStopwatch;
        }

        private bool ModifyShouldUpdateRunStopwatch(On.RoR2.Run.orig_ShouldUpdateRunStopwatch orig, Run self)
        {
            bool b = orig(self);
            //idc, if stopwatch is already frozen
            if (!b)
                return b;
            //run stopwatch if stage is not final
            return !SceneCatalog.mostRecentSceneDef.isFinalStage;
        }

        void ChangeDifficultyCoefficientCalculation()
        {
            Run.ambientLevelCap = ambientLevelCap;
            //IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += AmbientLevelChanges;
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal += DifficultyCoefficientChanges;
            IL.RoR2.CombatDirector.DirectorMoneyWave.Update += DirectorCreditGainChanges;

            drizzleDesc +=
                $"\n>Starting Difficulty: <style=cIsHealing>Easy</style>" + 
                $"\n>Max Enemy Level: <style=cIsHealing>{ambientLevelCapDrizzle - ambientLevelCap}</style> " +
                $"\n>{Tier2EliteName} Elites: <style=cIsHealing>Stage {Tier2EliteMinimumStageDrizzle}</style>" +
                $"\n>Teleporter Visuals: <style=cIsHealing>+{Tools.ConvertDecimal(easyTeleParticleRadius / normalTeleParticleRadius - 1)}</style> ";

            rainstormDesc +=
                $"\n>Starting Difficulty: Medium" + 
                $"\n>{Tier2EliteName} Elites: Stage {Tier2EliteMinimumStageRainstorm}" +
                $"\n>Teleporter Visuals: +{Tools.ConvertDecimal(normalTeleParticleRadius / normalTeleParticleRadius - 1)} ";

            monsoonDesc +=
                $"\n>Starting Difficulty: <style=cIsHealth>Hard</style>" + 
                $"\n>{Tier2EliteName} Elites: <style=cIsHealth>Stage {Tier2EliteMinimumStageMonsoon}</style>" +
                $"\n>Teleporter Visuals: <style=cIsHealth>{Tools.ConvertDecimal(1 - hardTeleParticleRadius / normalTeleParticleRadius)}</style> ";

            RoR2Application.onLoad += AddDifficultyStats;
        }

        private void AddDifficultyStats()
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

        private void DirectorCreditGainChanges(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(out _));
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, 1 - playerSpawnRateFactor);
            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(out _));
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, playerSpawnRateFactor);

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(out _),
                x => x.MatchStloc(out _),
                x => x.MatchLdcR4(out _));
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, difficultySpawnRateFactor);
        }

        private void AmbientLevelChanges(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //num2 (difficulty coefficient)
            int timeLoc = 2;
            int timeMul = 2;
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out timeLoc),
                x => x.MatchLdcR4(out _),
                x => x.MatchMul(),
                x => x.MatchCallOrCallvirt<Mathf>("Floor")
                );
            c.Index--;
            c.Emit(OpCodes.Ldc_R4, baseScalingMultiplier);
            c.Emit(OpCodes.Mul);

            //num9 (difficulty coefficient)
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<RoR2.Run>("stageClearCount")
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, difficultyIncreasePerStage);

            //num10 (ambient level)
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out timeLoc),
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            c.Emit(OpCodes.Ldc_R4, baseScalingMultiplier);
            c.Emit(OpCodes.Mul);

            //num10 (ambient level)
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<RoR2.Run>("stageClearCount")
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, difficultyIncreasePerStage);


            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld<RoR2.Run>("ambientLevelCap")
                );
            c.EmitDelegate<Func<float, float>>((levelIn) =>
            {
                float difficultyBoost = GetAmbientLevelBoost();

                //Run.instance.compensatedDifficultyCoefficient += difficultyBoost * 0.05f; //stage 3 spawnrates at stage 0 monsoon, stage 2 spawnrates at stage 0 rainstorm
                //Run.instance.difficultyCoefficient += difficultyBoost / 2;
                float levelOut = levelIn + difficultyBoost;
                return levelOut;
            });
        }

        private void DifficultyCoefficientChanges(On.RoR2.Run.orig_RecalculateDifficultyCoefficentInternal orig, Run self)
        {
            float runTimerMinutes = self.GetRunStopwatch() * 0.016666668f;
            int stageClearCount = self.stageClearCount;

            float difficultyCoefficient = GetDifficultyCoefficient(self, runTimerMinutes, stageClearCount, out float playerBaseFactor);
            float difficultyBoost = GetCoefficientBoostForDifficulty(self.selectedDifficulty);//GetAmbientLevelBoost() / 2;

            //difficulty coefficient used for interactable costs and etc
            self.difficultyCoefficient = difficultyCoefficient;
            //difficulty coefficient used for enemy spawns
            self.compensatedDifficultyCoefficient = difficultyCoefficient + difficultyBoost;
            self.oneOverCompensatedDifficultyCoefficientSquared = 1 / (self.compensatedDifficultyCoefficient * self.compensatedDifficultyCoefficient);
            self.ambientLevel = Mathf.Min(1f + GetAmbientLevelBoost() + (3f * (difficultyCoefficient - playerBaseFactor)), (float)Run.ambientLevelCap);

            int ambientLevelFloorLast = self.ambientLevelFloor;
            self.ambientLevelFloor = Mathf.FloorToInt(self.ambientLevel);
            if (ambientLevelFloorLast != self.ambientLevelFloor && ambientLevelFloorLast != 0 && self.ambientLevelFloor > ambientLevelFloorLast)
            {
                self.OnAmbientLevelUp();
            }
        }
        public static float GetCoefficientBoostForDifficulty(DifficultyIndex difficulty)
        {
            DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(difficulty);
            float scalingValue = 0;

            if (DifficultyUtilsModule.ValidateCachedDifficultyStats())
            {
                scalingValue = DifficultyUtilsModule.cachedDifficultyStats.startingDifficultyCoefficientBoost;
            }

            return scalingValue;
        }
        public static float GetScalingValueForDifficulty(DifficultyIndex difficulty)
        {
            DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(difficulty);
            return difficultyDef.scalingValue;
        }
        public static float GetDifficultyCoefficient(Run run, float timeInMinutes, int stageClearCount, out float playerBaseFactor)
        {
            float scalingValue = GetScalingValueForDifficulty(run.selectedDifficulty);
            float baseScalingFactor = 0.0506f * baseScalingMultiplier;

            float timeFactor = GetTimeDifficultyFactor(timeInMinutes, scalingValue);
            float stageFactor = GetStageDifficultyFactor(stageClearCount);

            playerBaseFactor = 1 + playerBaseDifficultyFactor * (run.participatingPlayerCount - 1);
            float playerScaleFactor = Mathf.Pow(run.participatingPlayerCount, playerScalingDifficultyFactor);
            float scalingFactor = baseScalingFactor * scalingValue * playerScaleFactor;

            return (playerBaseFactor + scalingFactor * timeInMinutes) * timeFactor * stageFactor;

            float GetTimeDifficultyFactor(float timeInMinutes, float scalingValue)
            {
                float timeFactor = Mathf.Pow(difficultyIncreasePerMinuteBase + difficultyIncreasePerMinutePerDifficulty * scalingValue, timeInMinutes);
                return timeFactor;
            }
            float GetStageDifficultyFactor(int stageClearCount)
            {
                float stageFactor = Mathf.Pow(difficultyIncreasePerStage, (float)stageClearCount);

                int totalLoops = Mathf.FloorToInt((float)stageClearCount / 5);
                if (stageClearCount % 5 <= 1 && Stage.instance && SceneCatalog.GetSceneDefForCurrentScene().isFinalStage)
                    totalLoops -= 1;
                float loopFactor = Mathf.Pow(difficultyIncreasePerLoop, totalLoops);

                return stageFactor * loopFactor;
            }
        }
        #endregion

        #region monsoon-exclusive
        private void MonsoonStatBoost()
        {
            monsoonDesc += $"\n>Enemies gain <style=cIsHealth>unique scaling</style></style>";

            GetStatCoefficients += this.MonsoonPlusStatBuffs2;
        }
        private void MonsoonPlusStatBuffs2(CharacterBody sender, StatHookEventArgs args)
        {
            DifficultyIndex selectedDifficulty = Run.instance.selectedDifficulty;
            float ambientLevelBoost = GetAmbientLevelBoost();
            if (sender.teamComponent.teamIndex != TeamIndex.Player)
            {
                if (selectedDifficulty >= DifficultyIndex.Hard || selectedDifficulty == SwanSongExtended.SwanSongPlugin.difficultyIndexExtinction)
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
        private void EclipseChanges()
        {
            //remove old stuff
            IL.RoR2.CharacterMaster.OnBodyStart += RemoveEclipseEffect; //lv1 starting health
            IL.RoR2.GlobalEventManager.OnCharacterHitGroundServer += RemoveEclipseEffect; //lv3 frailty
            IL.RoR2.HealthComponent.Heal += RemoveEclipseEffect;//lv5 healing
            IL.RoR2.DeathRewards.OnKilledServer += RemoveEclipseEffect;//lv6 gold drops
            //IL.RoR2.HealthComponent.TakeDamageProcess += RemoveEclipseEffect;//lv8 eclipse curse :skull:

            IL.RoR2.CharacterBody.RecalculateStats += RemoveEclipseStats; //lv4 enemy speed lv7 enemy cooldowns

            //new stuff
            DifficultyUtilsModule.ForceEliteMasterProvider += EclipseForceEliteMaster;
            //DifficultyUtilsModule.ForceEliteSpawnProvider += EclipseForceEliteSpawn;
            //GetStatCoefficients += this.EclipseStatBuffs;
            GetMoreStatCoefficients += this.EclipseStatBuffs2;
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


        private void EclipseStatBuffs2(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.teamComponent.teamIndex != TeamIndex.Player || Run.instance.selectedDifficulty <= eclipseLevelItemTax)
                return;
            args.healingPercentIncreaseMult *= eclipseHealingMultiplier;
        }

        private void EclipseItemTax(Stage obj)
        {
            //only tax items on stages, not hidden realms
            if (obj.sceneDef.sceneType != SceneType.Stage
                && obj.sceneDef.sceneType != SceneType.UntimedStage)
                return;

            foreach(CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                //only tax items on players in difficulty levels at or higher than eclipse 7
                if (master.teamIndex != TeamIndex.Player || Run.instance.selectedDifficulty <= eclipseLevelItemTax)
                    continue;
                if (master.inventory == null)
                    continue;

                EclipseItemTaxer taxer;
                if(!master.TryGetComponent(out taxer))
                {
                    taxer = master.gameObject.AddComponent<EclipseItemTaxer>();
                    taxer.master = master;
                }

                taxer.TaxItems();
            }
        }

        private bool EclipseForceEliteSpawn(CharacterSpawnCard card)
        {
            if (!Run.instance || Run.instance.selectedDifficulty < eclipseLevelBossElite)
                return false;

            if (card.noElites)
                return false;

            if (card.prefab && card.prefab.TryGetComponent(out CharacterMaster master))
                return EclipseForceEliteMasterInternal(master);
            return false;
        }
        private bool EclipseForceEliteMaster(CharacterMaster sender)
        {
            if (!Run.instance || Run.instance.selectedDifficulty < eclipseLevelBossElite)
                return false;
            if (sender.isBoss)
                return true;
            return EclipseForceEliteMasterInternal(sender);
        }
        private bool EclipseForceEliteMasterInternal(CharacterMaster sender)
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

        private void RemoveEclipseStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ChangeRequiredDifficultyLevelForStats(c, DifficultyIndex.Eclipse4, eclipseLevelEnemyMspd, eclipseEnemyMspd);

            ChangeRequiredDifficultyLevelForStats(c, DifficultyIndex.Eclipse7, eclipseLevelEnemyCdr, eclipseEnemyCooldownScale);
        }

        private void RemoveEclipseEffect(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<RoR2.Run>("get_selectedDifficulty")
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, (int)DifficultyIndex.Invalid);
        }

        private void EclipseHoldoutDischarge(On.RoR2.HoldoutZoneController.orig_Start orig, HoldoutZoneController self)
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

        private void EclipseHoldoutScale(ILContext il)
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

        private void EclipseSpiteArtifact(On.RoR2.RunArtifactManager.orig_SetArtifactEnabled orig, RunArtifactManager self, ArtifactDef artifactDef, bool newEnabled)
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

        #region void fields
        public static float voidFieldsTimeCost = 120; //0
        void VoidFieldsStageType()
        {
            SceneDef voidFieldsScene = Addressables.LoadAssetAsync<SceneDef>("RoR2/Base/arena/arena.asset").WaitForCompletion();
            voidFieldsScene.sceneType = SceneType.Intermission;
        }
        void VoidFieldsTimeCost()
        {
            On.EntityStates.Missions.Arena.NullWard.WardOnAndReady.OnExit += AddVoidFieldsTimeCost;
        }
        private void AddVoidFieldsTimeCost(On.EntityStates.Missions.Arena.NullWard.WardOnAndReady.orig_OnExit orig, EntityStates.Missions.Arena.NullWard.WardOnAndReady self)
        {
            orig(self);
            Run.instance.SetRunStopwatch(Run.instance.GetRunStopwatch() + voidFieldsTimeCost);
        }
        #endregion

        #region directors
        public static float fastDirectorEliteBias = 1.2f;//1
        public static float fastDirectorCreditMultiplier = 0.75f;//0.75f
        public static float slowDirectorEliteBias = 1.2f;//1
        public static float slowDirectorCreditMultiplier = 1f;//0.75f

        public static float teleLesserEliteBias = 1f;//1
        public static float teleLesserCreditMultiplier = 0.8f;//1f
        public static float teleBossEliteBias = 1f;//1
        public static float teleBossCreditMultiplier = 1.0f;//1f
        public static float teleBossCreditMultiplierStage1 = 0.5f;//1f
        void ChangeDirectorStats()
        {
            GameObject baseDirector = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Director.prefab").WaitForCompletion();
            CombatDirector[] directors1 = baseDirector.GetComponents<CombatDirector>();
            if(directors1.Length > 0)
            {
                CombatDirector fastDirector = directors1[0];
                if(fastDirector != null)
                {
                    fastDirector.eliteBias = fastDirectorEliteBias;
                    fastDirector.eliteBias = fastDirectorCreditMultiplier;
                }

                CombatDirector slowDirector = directors1[1];
                if (slowDirector != null)
                {
                    slowDirector.eliteBias = slowDirectorEliteBias;
                    slowDirector.eliteBias = slowDirectorCreditMultiplier;
                }
            }
            On.RoR2.CombatDirector.Awake += AdjustTpDirectors;
            On.RoR2.CombatDirector.SetNextSpawnAsBoss += FixBossDirectorCredits;
            //On.RoR2.TeleporterInteraction.Awake += AdjustDirectorsForTeleporter;
            //GameObject teleporterDefault = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Teleporters.Teleporter1_prefab).WaitForCompletion();
            //GameObject teleporterLunar = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Teleporters.LunarTeleporter_Variant_prefab).WaitForCompletion();
            //AdjustTeleporterDirectors(teleporterLunar.GetComponents<CombatDirector>());

        }

        private void FixBossDirectorCredits(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            self.monsterCredit *= teleBossCreditMultiplier;
            orig(self);
        }

        private void AdjustTpDirectors(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector director)
        {
            if (director.customName == "Boss")
            {
                AdjustTpBossDirector(director);
            }
            if (director.customName == "Monsters")
            {
                AdjustTpMonsterDirector(director);
            }
            orig(director);
        }

        private void AdjustDirectorsForTeleporter(On.RoR2.TeleporterInteraction.orig_Awake orig, TeleporterInteraction self)
        {
            AdjustTpBossDirector(self.bossDirector);
            AdjustTpBossDirector(self.companionBoss);
            AdjustTpMonsterDirector(self.bonusDirector);
            orig(self);
        }
        void AdjustTeleporterDirectors(CombatDirector[] directors)
        {
            if (directors != null && directors.Length > 0)
            {
                foreach (CombatDirector director in directors)
                {
                }
            }
        }
        void AdjustTpBossDirector(CombatDirector director)
        {
            director.eliteBias = teleBossEliteBias;
            director.creditMultiplier = teleBossCreditMultiplier;
            if (Run.instance.stageClearCount == 0)
                director.creditMultiplier *= teleBossCreditMultiplierStage1;
        }
        void AdjustTpMonsterDirector(CombatDirector director)
        {
            director.eliteBias = teleLesserEliteBias;
            director.creditMultiplier = teleLesserCreditMultiplier;
        }
        #endregion

        #region tp boss weaken
        public void AddPityCharge()
        {
            On.RoR2.TeleporterInteraction.ChargingState.FixedUpdate += WeakenBossPostTpCharge;
            On.RoR2.TeleporterInteraction.ChargingState.OnExit += PityChargeOnExit;
        }

        private void PityChargeOnExit(On.RoR2.TeleporterInteraction.ChargingState.orig_OnExit orig, TeleporterInteraction.ChargingState self)
        {
            orig(self);
            if (pityChargeOn)
            {
                pityChargeOn = false;
                pityChargeShrinkDelta = 0;
                pityChargeRecolorDelta = 0;
                self.teleporterInteraction.holdoutZoneController.calcColor -= PityChargeCalcColor;
                self.teleporterInteraction.holdoutZoneController.calcRadius -= PityChargeCalcRadius;
            }
        }

        private void PityChargeCalcRadius(ref float radius)
        {
            radius = Mathf.Max(radius * (1 - pityChargeShrinkDelta), 10f);
        }

        private void PityChargeCalcColor(ref Color color)
        {
            color = HoldoutZoneController.FocusConvergenceController.convergenceMaterialColor;
        }

        static bool pityChargeOn = false;
        static float pityChargeShrinkDelta = 0;
        static float pityChargeRecolorDelta = 0;
        private void WeakenBossPostTpCharge(On.RoR2.TeleporterInteraction.ChargingState.orig_FixedUpdate orig, RoR2.TeleporterInteraction.ChargingState baseState)
        {
            orig(baseState);

            if (!SwanSongExtended.Storms.StormRunBehavior.IsStormStage(Stage.instance.sceneDef)) 
                return;
            TeleporterInteraction.ChargingState self = baseState as TeleporterInteraction.ChargingState;
            if(self.teleporterInteraction.holdoutZoneController.charge >= 1f)
            {
                if (!self.teleporterInteraction.monstersCleared && self.teleporterInteraction.holdoutZoneController.isAnyoneCharging)
                {
                    if (!pityChargeOn)
                    {
                        pityChargeOn = true;
                        self.teleporterInteraction.holdoutZoneController.calcColor += PityChargeCalcColor;
                        self.teleporterInteraction.holdoutZoneController.calcRadius += PityChargeCalcRadius;

                        // send chat message
                        RoR2.Chat.AddMessage("<style=cIsUtility>The overcharged teleporter begins its Convergence...</style>");
                        // add tutorial popup
                    }
                    if (pityChargeRecolorDelta < 1)
                        pityChargeRecolorDelta += Time.fixedDeltaTime;

                    pityChargeShrinkDelta += Time.fixedDeltaTime * 0.01f;

                    if (NetworkServer.active)
                    {
                        BossGroup bg = self.teleporterInteraction.bossGroup;
                        foreach (BossGroup.BossMemory bossMemory in bg.bossMemories)
                        {
                            CharacterBody body = bossMemory.cachedBody;
                            if (body == null && bossMemory.cachedMaster != null)
                            {
                                body = bossMemory.cachedMaster.GetBody();
                            }
                            if (body != null)
                            {
                                body.AddTimedBuff(RoR2Content.Buffs.Cripple, 9999);
                                body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, 9999);
                                HealthComponent hc = body.healthComponent;
                                if (hc && hc.health > 1)
                                {
                                    DamageInfo di = new DamageInfo();
                                    di.damage = (body.maxHealth + body.maxShield) * 0.01f * Time.fixedDeltaTime;
                                    di.damageType = new DamageTypeCombo(DamageType.Silent,
                                        DamageTypeExtended.Generic, DamageSource.NoneSpecified);
                                    di.damageType |= DamageType.BypassArmor;
                                    di.damageType |= DamageType.BypassBlock;
                                    di.procCoefficient = 1;
                                    di.position = body.corePosition;
                                    hc.TakeDamage(di);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                pityChargeOn = false;
            }
        }
        #endregion
    }
}