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

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static float drizzleDifficultyBoost = 0;
        public static float rainstormDifficultyBoost = 3;
        public static float monsoonDifficultyBoost = 6;
        public static float eclipseDifficultyBoost = 9;

        /// <summary>
        /// linear. increases the difficulty by this amount per minute, affected by the difficulty's scaling value
        /// </summary>
        public static float baseScalingMultiplier = 1f; //1f
        public static float difficultyIncreasePerMinutePerDifficulty = 0.01f; //0f
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
        public static int ambientLevelCap = 999;//99

        public static float easyTeleParticleRadius = 1f;
        public static float normalTeleParticleRadius = 0.8f;
        public static float hardTeleParticleRadius = 0.4f;
        public static float eclipseTeleParticleRadius = 0.4f;
        public static float defaultTeleParticleRadius = 0.9f;

        #region tele particle scale
        private void DifficultyDependentTeleParticles()
        {
            drizzleDesc += $"\n>Teleporter Visuals: <style=cIsHealing>+{Tools.ConvertDecimal(easyTeleParticleRadius / normalTeleParticleRadius - 1)}</style> ";
            rainstormDesc += $"\n>Teleporter Visuals: +{Tools.ConvertDecimal(normalTeleParticleRadius / normalTeleParticleRadius - 1)} ";
            monsoonDesc += $"\n>Teleporter Visuals: <style=cIsHealth>{Tools.ConvertDecimal(1 - hardTeleParticleRadius / normalTeleParticleRadius)}</style> ";

            On.RoR2.TeleporterInteraction.BaseTeleporterState.OnEnter += TeleporterParticleScale;
        }
        private void TeleporterParticleScale(On.RoR2.TeleporterInteraction.BaseTeleporterState.orig_OnEnter orig, BaseState self)
        {
            orig(self);
            float particleScale = 1f;
            switch (Run.instance.selectedDifficulty)
            {
                default:
                    particleScale = eclipseTeleParticleRadius;
                    break;
                case RoR2.DifficultyIndex.Hard:
                    particleScale = hardTeleParticleRadius;
                    break;
                case RoR2.DifficultyIndex.Normal:
                    particleScale = normalTeleParticleRadius;
                    break;
                case RoR2.DifficultyIndex.Easy:
                    particleScale = easyTeleParticleRadius;
                    break;
                case RoR2.DifficultyIndex.Count:
                    break;
                case RoR2.DifficultyIndex.Invalid:
                    break;
            }


            TeleporterInteraction component = self.GetComponent<TeleporterInteraction>();
            bool flag5 = component && component.modelChildLocator;
            if (flag5)
            {
                Transform transform = component.transform.Find("TeleporterBaseMesh/BuiltInEffects/PassiveParticle, Sphere");
                if (transform)
                {
                    //Debug.Log(transform.localScale);
                    transform.localScale = Vector3.one * defaultTeleParticleRadius * particleScale;
                }
            }
        }
        #endregion

        #region ambient level
        internal static float GetAmbientLevelBoost()
        {
            float difficultyBoost = 0f;
            if (!useAmbientLevel)
                return difficultyBoost;

            DifficultyIndex selectedDifficulty = Run.instance.selectedDifficulty;
            switch (selectedDifficulty)
            {
                default:
                    if (selectedDifficulty >= eclipseLevelVeryHard)
                        difficultyBoost = eclipseDifficultyBoost;
                    else
                        difficultyBoost = monsoonDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Hard:
                    difficultyBoost = monsoonDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Normal:
                    difficultyBoost = rainstormDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Easy:
                    difficultyBoost = drizzleDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Count:
                    break;
                case RoR2.DifficultyIndex.Invalid:
                    break;
            }

            return difficultyBoost;
        }

        public static bool useAmbientLevel = false;
        void AmbientLevelDifficulty()
        {
            useAmbientLevel = true;
            Run.ambientLevelCap = ambientLevelCap;
            //IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += AmbientLevelChanges;
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal += DifficultyCoefficientChanges;
            IL.RoR2.CombatDirector.DirectorMoneyWave.Update += DirectorCreditGainChanges;
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
            DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(self.selectedDifficulty);
            float scalingValue = difficultyDef.scalingValue;
            if (self.selectedDifficulty >= eclipseLevelVeryHard)
                scalingValue += 1;
            float runTimerMinutes = self.GetRunStopwatch() * 0.016666668f;
            float baseScalingFactor = 0.0506f * baseScalingMultiplier;

            float timeFactor = GetTimeDifficultyFactor(runTimerMinutes, scalingValue);
            float stageFactor = GetStageDifficultyFactor(self.stageClearCount);
            
            float playerBaseFactor = 1 + playerBaseDifficultyFactor * (self.participatingPlayerCount - 1);
            float playerScaleFactor = Mathf.Pow(self.participatingPlayerCount, playerScalingDifficultyFactor);
            float scalingFactor = baseScalingFactor * scalingValue * playerScaleFactor;


            float difficultyCoefficient = (playerBaseFactor + scalingFactor * runTimerMinutes) * timeFactor * stageFactor;

            self.difficultyCoefficient = difficultyCoefficient;
            self.compensatedDifficultyCoefficient = difficultyCoefficient;
            self.oneOverCompensatedDifficultyCoefficientSquared = 1 / (self.compensatedDifficultyCoefficient * self.compensatedDifficultyCoefficient);
            self.ambientLevel = Mathf.Min(1f + GetAmbientLevelBoost() + 3f * (difficultyCoefficient - playerBaseFactor), (float)Run.ambientLevelCap);

            int ambientLevelFloor = self.ambientLevelFloor;
            self.ambientLevelFloor = Mathf.FloorToInt(self.ambientLevel);
            if (ambientLevelFloor != self.ambientLevelFloor && ambientLevelFloor != 0 && self.ambientLevelFloor > ambientLevelFloor)
            {
                self.OnAmbientLevelUp();
            }

            float GetTimeDifficultyFactor(float timeInMinutes, float scalingValue)
            {
                float timeFactor = Mathf.Pow(difficultyIncreasePerMinuteBase + difficultyIncreasePerMinutePerDifficulty * scalingValue, timeInMinutes);
                return timeFactor;
            }
            float GetStageDifficultyFactor(int stageClearCount)
            {
                float stageFactor = Mathf.Pow(difficultyIncreasePerStage, (float)stageClearCount);

                int totalLoops = Mathf.FloorToInt((float)self.stageClearCount / 5);
                if (self.stageClearCount % 5 <= 1 && Stage.instance && SceneCatalog.GetSceneDefForCurrentScene().isFinalStage)
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
                if (selectedDifficulty >= DifficultyIndex.Hard)
                {
                    float compensatedLevel = sender.level - ambientLevelBoost;

                    if(sender.baseNameToken != "JELLYFISH_BODY_NAME")
                    {
                        args.attackSpeedMultAdd += Mathf.Clamp01(compensatedLevel / 200f) * 9f;
                    }

                    if (sender.isChampion)
                    {
                        args.armorAdd += 3 * compensatedLevel;
                    }
                    else
                    {
                        args.moveSpeedMultAdd += Mathf.Clamp01(compensatedLevel / 200f) * 3f;
                    }
                }
            }
        }
        #endregion

        #region eclipse-exclusive

        public static DifficultyIndex eclipseLevelBossShield = DifficultyIndex.Eclipse1; //
        public static float eclipseBossShieldFraction = 0.1f;
        public static string eclipseOneDesc =
            $"\n<mspace=0.5em>(1)</mspace> Boss Shields: <style=cIsHealth>+{Tools.ConvertDecimal(eclipseBossShieldFraction)}</style>";

        public static DifficultyIndex eclipseLevelHoldoutLoss = DifficultyIndex.Eclipse2;
        public static float eclipseHoldoutLossRate = 0.03f; //pillar of soul is 10%
        public static string eclipseTwoDesc =
            $"\n<mspace=0.5em>(2)</mspace> Holdout Zone Discharge: <style=cIsHealth>-{Tools.ConvertDecimal(eclipseHoldoutLossRate)} per second</style>";

        public static DifficultyIndex eclipseLevelEnemyCdr = DifficultyIndex.Eclipse3; //
        public static float eclipseEnemyCdr = 0.5f;
        public static string eclipseThreeDesc =
            $"\n<mspace=0.5em>(3)</mspace> Enemy Cooldowns: <style=cIsHealth>-{Tools.ConvertDecimal(eclipseEnemyCdr)}</style>";

        public static DifficultyIndex eclipseLevelSmallHoldout = DifficultyIndex.Eclipse4; //
        public static float eclipseHoldoutScale = 0.7f;
        public static string eclipseFourDesc =
            $"\n<mspace=0.5em>(4)</mspace> Holdout Zone Radius: <style=cIsHealth>-{Tools.ConvertDecimal(1 - eclipseHoldoutScale)}</style>";

        public static DifficultyIndex eclipseLevelEnemyMspd = DifficultyIndex.Eclipse5; //
        public static float eclipseEnemyMspd = 0.25f;
        public static string eclipseFiveDesc =
            $"\n<mspace=0.5em>(5)</mspace> Enemy Speed: <style=cIsHealth>+{Tools.ConvertDecimal(eclipseEnemyMspd)}</style>";

        public static DifficultyIndex eclipseLevelSpiteArtifact = DifficultyIndex.Eclipse6; //
        public static string eclipseSixDesc =
            $"\n<mspace=0.5em>(6)</mspace> On Kill: <style=cIsHealth>Enemies drop exploding bombs</style>";

        public static DifficultyIndex eclipseLevelVeryHard = DifficultyIndex.Eclipse7; //
        public static string eclipseSevenDesc =
            $"\n<mspace=0.5em>(7)</mspace> Difficulty: <style=cIsHealth>Very Hard</style>";

        public static DifficultyIndex eclipseLevelPlayerDegen = DifficultyIndex.Eclipse8; //
        public static float eclipsePlayerDegen = 0.2f;
        public static string eclipseEightDesc =
            $"\n<mspace=0.5em>(8)</mspace> Health Degeneration: <style=cIsHealth>-{Tools.ConvertDecimal(eclipsePlayerDegen)} per level</style>";
        private void EclipseChanges()
        {
            //remove old stuff
            IL.RoR2.CharacterMaster.OnBodyStart += RemoveEclipseEffect; //lv1 starting health
            IL.RoR2.GlobalEventManager.OnCharacterHitGroundServer += RemoveEclipseEffect; //lv3 frailty
            IL.RoR2.HealthComponent.Heal += RemoveEclipseEffect;//lv5 healing
            IL.RoR2.DeathRewards.OnKilledServer += RemoveEclipseEffect;//lv6 gold drops
            IL.RoR2.HealthComponent.TakeDamageProcess += RemoveEclipseEffect;//lv8 eclipse curse :skull:

            IL.RoR2.CharacterBody.RecalculateStats += RemoveEclipseStats; //lv4 enemy speed lv7 enemy cooldowns

            //new stuff
            GetStatCoefficients += this.EclipseStatBuffs;
            On.RoR2.CharacterBody.RecalculateStats += this.EclipseCdr;
            On.RoR2.RunArtifactManager.SetArtifactEnabled += EclipseSpiteArtifact;
            IL.RoR2.HoldoutZoneController.DoUpdate += EclipseHoldoutScale;
            On.RoR2.HoldoutZoneController.Start += EclipseHoldoutDischarge;

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

        private void EclipseCdr(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            DifficultyIndex selectedDifficulty = Run.instance.selectedDifficulty;
            if (self.teamComponent.teamIndex != TeamIndex.Player)
            {
                //enemy cooldowns
                if (selectedDifficulty >= eclipseLevelEnemyCdr)
                {
                    float cdrBoost = 1 - eclipseEnemyCdr;

                    SkillLocator skillLocator = self.skillLocator;
                    if (skillLocator != null)
                    {
                        ApplyCooldownScale(skillLocator.primary, cdrBoost);
                        ApplyCooldownScale(skillLocator.secondary, cdrBoost);
                        ApplyCooldownScale(skillLocator.utility, cdrBoost);
                        ApplyCooldownScale(skillLocator.special, cdrBoost);
                    }
                }
            }
        }

        private void RemoveEclipseStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<RoR2.Run>("get_selectedDifficulty")
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, (int)DifficultyIndex.Invalid);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<RoR2.Run>("get_selectedDifficulty")
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, (int)DifficultyIndex.Invalid);
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
                self.dischargeRate = Mathf.Max(self.dischargeRate, eclipseHoldoutLossRate);
            }
            orig(self);
        }

        private void EclipseHoldoutScale(ILContext il)
        {
            ILCursor c = new ILCursor(il);

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

        private void EclipseStatBuffs(CharacterBody sender, StatHookEventArgs args)
        {
            DifficultyIndex selectedDifficulty = Run.instance.selectedDifficulty;
            if (sender.teamComponent.teamIndex != TeamIndex.Player)
            {
                //boss shield
                if (selectedDifficulty >= eclipseLevelBossShield)
                {
                    if (sender.isBoss)
                    {
                        args.baseShieldAdd += sender.maxHealth * eclipseBossShieldFraction;
                    }
                }
                else return;

                //enemy cooldowns
                if (selectedDifficulty >= eclipseLevelEnemyCdr)
                {
                    //args.cooldownMultAdd *= 1 - eclipseEnemyCdr;
                }
                else return;

                //enemy speed
                if (selectedDifficulty >= eclipseLevelEnemyMspd)
                {
                    args.moveSpeedMultAdd += eclipseEnemyMspd;
                }
                else return;
            }
            if (sender.teamComponent.teamIndex == TeamIndex.Player)
            {
                //player degen
                if (selectedDifficulty >= eclipseLevelPlayerDegen)
                {
                    args.baseRegenAdd -= (sender.baseRegen + (sender.levelRegen * sender.level)) * (eclipsePlayerDegen * sender.level);
                }
            }
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
        public static float fastDirectorEliteBias = 0.75f;//1
        public static float fastDirectorCreditMultiplier = 0.75f;//0.75f
        public static float slowDirectorEliteBias = 1f;//1
        public static float slowDirectorCreditMultiplier = 1.5f;//0.75f

        public static float teleLesserEliteBias = 1f;//1
        public static float teleLesserCreditMultiplier = 1f;//1f
        public static float teleBossEliteBias = 1f;//1
        public static float teleBossCreditMultiplier = 1f;//1f
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
            GameObject teleporterDefault = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Director.prefab").WaitForCompletion();
            GameObject teleporterLunar = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Director.prefab").WaitForCompletion();
            AdjustTeleporterDirectors(teleporterDefault.GetComponents<CombatDirector>());
            AdjustTeleporterDirectors(teleporterLunar.GetComponents<CombatDirector>());

            void AdjustTeleporterDirectors(CombatDirector[] directors)
            {
                if (directors != null && directors.Length > 0)
                {
                    foreach (CombatDirector director in directors)
                    {
                        if (director.customName == "Boss")
                        {
                            director.eliteBias = teleBossEliteBias;
                            director.creditMultiplier = teleBossCreditMultiplier;
                        }
                        if (director.customName == "Monsters")
                        {
                            director.eliteBias = teleLesserEliteBias;
                            director.creditMultiplier = teleLesserCreditMultiplier;
                        }
                    }
                }
            }
        }
        #endregion

        #region tp boss weaken
        public void AddTpBossWeaken()
        {
            On.RoR2.TeleporterInteraction.ChargingState.FixedUpdate += WeakenBossPostTpCharge;
        }

        static bool wasTpCharged = false;
        private void WeakenBossPostTpCharge(On.RoR2.TeleporterInteraction.ChargingState.orig_FixedUpdate orig, BaseState baseState)
        {
            orig(baseState);
            if (NetworkServer.active)
            {
                TeleporterInteraction.ChargingState self = baseState as TeleporterInteraction.ChargingState;
                if(self.teleporterInteraction.holdoutZoneController.charge >= 1f)
                {
                    if (!wasTpCharged)
                    {
                        wasTpCharged = true;
                        if (!self.teleporterInteraction.monstersCleared)
                        {
                            BossGroup bg = self.teleporterInteraction.bossGroup;
                            foreach (BossGroup.BossMemory bossMemory in bg.bossMemories)
                            {
                                CharacterBody body = bossMemory.cachedBody;
                                if(body == null && bossMemory.cachedMaster != null)
                                {
                                    body = bossMemory.cachedMaster.GetBody();
                                }
                                if(body != null)
                                {
                                    body.AddTimedBuff(RoR2Content.Buffs.Cripple, 9999);
                                }
                            }
                        }
                    }
                }
                else
                {
                    wasTpCharged = false;
                }
            }
        }
        #endregion
    }
}