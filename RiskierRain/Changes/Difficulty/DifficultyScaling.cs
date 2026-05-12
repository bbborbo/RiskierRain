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
using static RoR2.GivePickupsOnStart;

namespace RiskierRain.Changes
{
    public static partial class DifficultyChanges
    {
        #region freeze on final levels
        public static void FreezeDifficultyScalingOnFinalLevels()
        {
            On.RoR2.Run.ShouldUpdateRunStopwatch += ModifyShouldUpdateRunStopwatch;
        }

        public static bool ModifyShouldUpdateRunStopwatch(On.RoR2.Run.orig_ShouldUpdateRunStopwatch orig, Run self)
        {
            bool b = orig(self);
            //idc, if stopwatch is already frozen
            if (!b)
                return b;
            //run stopwatch if stage is not final
            return !SceneCatalog.mostRecentSceneDef.isFinalStage;
        }
        #endregion

        #region difficulty coefficient calculation
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
        public static void ChangeDifficultyCoefficientCalculation()
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
        }


        public static void DirectorCreditGainChanges(ILContext il)
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

        public static void AmbientLevelChanges(ILContext il)
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

        public static void DifficultyCoefficientChanges(On.RoR2.Run.orig_RecalculateDifficultyCoefficentInternal orig, Run self)
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

        #region void fields
        public static float voidFieldsTimeCost = 120; //0
        public static void VoidFieldsStageType()
        {
            SceneDef voidFieldsScene = Addressables.LoadAssetAsync<SceneDef>("RoR2/Base/arena/arena.asset").WaitForCompletion();
            voidFieldsScene.sceneType = SceneType.Intermission;
        }
        public static void VoidFieldsTimeCost()
        {
            On.EntityStates.Missions.Arena.NullWard.WardOnAndReady.OnExit += AddVoidFieldsTimeCost;
        }
        public static void AddVoidFieldsTimeCost(On.EntityStates.Missions.Arena.NullWard.WardOnAndReady.orig_OnExit orig, EntityStates.Missions.Arena.NullWard.WardOnAndReady self)
        {
            orig(self);
            Run.instance.SetRunStopwatch(Run.instance.GetRunStopwatch() + voidFieldsTimeCost);
        }
        #endregion

        #region chest scaling
        static GameObject awu = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/SuperRoboBallBossBody");
        static CharacterBody awuBody;
        static float awuArmor = 40;
        static float awuAdditionalArmor = 0;
        static int awuAdaptiveArmorCount = 1;

        static float costExponent = 1.00f;
        public static void ChangeChestCostScaling()
        {
            On.RoR2.Run.GetDifficultyScaledCost_int_float += ChangeScaledCost;

            // adjusting AWU armor to compensate for chest cost increases
            awuBody = awu.GetComponent<CharacterBody>();
            if (awuBody)
            {
                awuBody.baseArmor = awuArmor;
                if (awuAdaptiveArmorCount <= 0)
                {
                    awuBody.armor += awuAdditionalArmor;
                }
                else
                {
                    GivePickupsOnStart gpos = awuBody.gameObject.AddComponent<GivePickupsOnStart>();
                    if (gpos)
                    {
                        ItemInfo adaptiveArmor = new ItemInfo();
                        adaptiveArmor.count = awuAdaptiveArmorCount;
                        adaptiveArmor.itemString = Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/AdaptiveArmor/AdaptiveArmor.asset").WaitForCompletion().nameToken;

                        gpos.itemInfos = new ItemInfo[1] { adaptiveArmor };
                    }
                }
            }
        }

        private static int ChangeScaledCost(On.RoR2.Run.orig_GetDifficultyScaledCost_int_float orig, RoR2.Run self, int baseCost, float difficultyCoefficient)
        {
            //this is hardcoded for force spawned interactables like the gold chest on abyssal
            //not gonna do it for the large chest on verdant falls though because $50 is a common price :/
            //im just cool with the modded collateral from these two ig
            //2r4r is no stranger to a little bit of collateral
            switch (baseCost)
            {
                //tc-280 drone
                case 350:
                    baseCost = InteractableChanges.bigDroneTypeCost;
                    break;
                //the gold chest on stage 4
                case 400:
                    baseCost = InteractableChanges.goldChestTypeCost;
                    break;
            }

            float costMultiplierExponential = Mathf.Pow(difficultyCoefficient, costExponent);
            float costMultiplierLinear = (difficultyCoefficient * 2.5f - 1.5f); //arbitrary, unused

            float endMultiplier = costMultiplierExponential;
            if (costMultiplierLinear < costMultiplierExponential)
            {
                //endMultiplier = costMultiplierLinear;
                //Debug.Log("Using Liner multiplier!");
            }

            return (int)((float)baseCost * endMultiplier);
        }
        #endregion

        #region rewards
        static float goldRewardMultiplierGlobal = 0.35f;
        static float expRewardMultiplierGlobal = 0.4f;
        static float compensationForStartingLevel = 1.0f;
        private static void ChangeEnemyRewards()
        {
            //On.RoR2.TeleporterInteraction.Awake += ReduceTeleDirectorReward;
            DifficultyUtilsModule.BoostedRewardCompensationCoefficient = compensationForStartingLevel;
            DifficultyUtilsModule.GoldRewardMultiplierGlobal = goldRewardMultiplierGlobal;
            DifficultyUtilsModule.ExpRewardMultiplierGlobal = expRewardMultiplierGlobal;
        }
        #endregion
    }
}
