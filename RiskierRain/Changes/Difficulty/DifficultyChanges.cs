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

namespace RiskierRain.Changes
{
    public static partial class DifficultyChanges
    {
        public static string Tier2EliteName = "Rare";
        internal static float GetAmbientLevelBoost()
        {
            return DifficultyUtilsModule.GetAmbientLevelBoost();
        }

        public static void Initialize()
        {
            //mechanics
            RemoveOspForever();
            AddPotentialProtection();
            AddPityCharge();

            //scaling
            FreezeDifficultyScalingOnFinalLevels();
            ChangeDifficultyCoefficientCalculation();
            //VoidFieldsTimeCost();
            ChangeEnemyRewards();

            #region difficulty descriptions
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
            #endregion

            //difficulty modes
            DifficultyUtilsModule.EnableAll();
            RoR2Application.onLoad += AddDifficultyStats;
            AddMonsoonScalingStats();
            ChangeEclipse();

            //directors
            ChangeDirectorStats();
            DirectorAPI.StageSettingsActions += IncreaseStageMonsterCredits;

            //rewards
            On.RoR2.MoneyPickup.Start += (orig, self) =>
            {
                //same as vanilla's but scaled cost uses entry difficulty snapshot
                if (NetworkServer.active)
                {
                    self.goldReward = (self.shouldScale ? Run.instance.GetDifficultyScaledCost(self.baseGoldReward, Stage.instance.entryDifficultyCoefficient) : self.baseGoldReward);
                }
            };

            //quests
            ChangeTrialDropTables();
            ChangeTrialOfGreed();
        }
        #region oneshot protection aka osp
        public static void RemoveOspForever()
        {
            // removes one-shot protection (OSP)
            Hook hookTuah = new Hook(
              typeof(CharacterBody).GetMethod("get_hasOneShotProtection", (BindingFlags)(-1)),
              typeof(DifficultyChanges).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
            // removes one-shot protection (OSP)
            Hook hook2ah = new Hook(
              typeof(CharacterBody).GetMethod("get_oneShotProtectionFraction", (BindingFlags)(-1)),
              typeof(DifficultyChanges).GetMethod(nameof(ReflectOnThatThang2), (BindingFlags)(-1))
            );
        }

        public static bool ReflectOnThatThang(orig_getHasOneShotProtection orig, CharacterBody self)
        {
            return false;
        }
        public delegate bool orig_getHasOneShotProtection(CharacterBody self);
        public static System.Single ReflectOnThatThang2(get_oneShotProtectionFraction orig, CharacterBody self)
        {
            return 0f;
        }
        public delegate System.Single get_oneShotProtectionFraction(CharacterBody self);
        #endregion
        #region potential protection
        public static bool potentialProtectionVisibility = true;
        public static bool potentialProtectionLimitedTime = false;
        public static float potentialProtectionDurationUnlimited = 0.5f;
        public static float potentialProtectionDurationLimited = 4;
        public static void AddPotentialProtection()
        {
            BuffIndex GetBuffIndex()
            {
                if (potentialProtectionVisibility == true)
                    return RoR2.RoR2Content.Buffs.Immune.buffIndex;
                return RoR2.RoR2Content.Buffs.HiddenInvincibility.buffIndex;
            }

            if (potentialProtectionLimitedTime == false)
            {
                On.RoR2.NetworkUIPromptController.SetParticipantMasterId += (orig, self, newMasterObjectInstanceId) =>
                {
                    CharacterMaster current = self.currentParticipantMaster;
                    orig(self, newMasterObjectInstanceId);
                    PickupPickerPanelProtection(current, self.currentParticipantMaster);
                };
                On.RoR2.NetworkUIPromptController.OnControlEnd += (orig, self) =>
                {
                    orig(self);
                    self.ClearParticipant();
                };

                void PickupPickerPanelProtection(CharacterMaster currentParticipantMaster, CharacterMaster newParticipantMaster)
                {
                    if (NetworkServer.active == false)
                        return;

                    if (currentParticipantMaster != null && newParticipantMaster != currentParticipantMaster)
                    {
                        CharacterBody body = currentParticipantMaster.GetBody();
                        if (body != null && body.HasBuff(GetBuffIndex()))
                        {
                            body.ClearTimedBuffs(GetBuffIndex());
                            while(body.HasBuff(GetBuffIndex()))
                                body.RemoveBuff(GetBuffIndex());
                            body.AddTimedBuff(GetBuffIndex(), potentialProtectionDurationUnlimited);
                        }
                    }

                    if (newParticipantMaster != null)
                    {
                        CharacterBody body = newParticipantMaster.GetBody();
                        if (body != null && body.isPlayerControlled)
                        {
                            body.ClearTimedBuffs(GetBuffIndex());
                            while (body.HasBuff(GetBuffIndex()))
                                body.RemoveBuff(GetBuffIndex());
                            body.AddBuff(GetBuffIndex());
                        }
                    }
                }
            }
            else
            {
                On.RoR2.UI.PickupPickerPanel.Awake += CommandOrPotentialArmor;

                void CommandOrPotentialArmor(On.RoR2.UI.PickupPickerPanel.orig_Awake orig, RoR2.UI.PickupPickerPanel self)
                {
                    RoR2.LocalUser user = RoR2.LocalUserManager.GetFirstLocalUser();
                    RoR2.CharacterBody body = user.cachedBody;
                    if(body.isPlayerControlled)
                        body.AddTimedBuffAuthority(GetBuffIndex(), potentialProtectionDurationLimited);
                    orig(self);
                };
            }
        }

        #endregion
        #region pity charge / teleporter overcharge

        public static bool pityChargeOn = false;
        public static float pityChargeShrinkDelta = 0;
        public static float pityChargeRecolorDelta = 0;
        public static void AddPityCharge()
        {
            On.RoR2.TeleporterInteraction.ChargingState.FixedUpdate += WeakenBossPostTpCharge;
            On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.ChargingState.FixedUpdate += WeakenBossPostCatastopheCharge;
            On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState.FixedUpdate += WeakenBossPostCatastopheCharge2;
            On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState.OnExit += PityChargeOnExitCatastrophe;
            On.RoR2.TeleporterInteraction.ChargingState.OnExit += PityChargeOnExit;
        }

        public static void PerformPityCharge(TeleporterInteraction self, bool isCatastrophe = false, BossGroup extraBossGroup = null)
        {
            if (!SwanSongExtended.Storms.StormRunBehavior.IsStormStage(Stage.instance.sceneDef))
                return;

            if (self.holdoutZoneController.charge >= self.holdoutZoneController.maxCharge - 0.01f)
            {
                if (!self.monstersCleared && self.holdoutZoneController.isAnyoneCharging)
                {
                    if (!pityChargeOn)
                    {
                        pityChargeOn = true;
                        self.holdoutZoneController.calcColor += PityChargeCalcColor;
                        self.holdoutZoneController.calcRadius += PityChargeCalcRadius;

                        // send chat message
                        if(isCatastrophe == false)
                            RoR2.Chat.AddMessage("<style=cIsUtility>The overcharged teleporter begins its Convergence...</style>");
                        // add tutorial popup
                    }
                    if (pityChargeRecolorDelta < 1)
                        pityChargeRecolorDelta += Time.fixedDeltaTime;

                    pityChargeShrinkDelta += Time.fixedDeltaTime * 0.01f;

                    if (NetworkServer.active)
                    {
                        BossGroup bg = self.bossGroup;
                        foreach (BossGroup.BossMemory bossMemory in bg.bossMemories)
                        {
                            CharacterBody body = bossMemory.cachedBody;
                            if (body != null)
                                ApplyOverchargeToBody(body, isCatastrophe);
                            else
                            {
                                ApplyOverchargeToMaster(bossMemory.cachedMaster, isCatastrophe);
                            }
                        }
                        if(extraBossGroup != null)
                        {
                            foreach(CharacterMaster master in extraBossGroup.combatSquad.membersList)
                            {
                                ApplyOverchargeToMaster(master, isCatastrophe);
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

        private static void ApplyOverchargeToMaster(CharacterMaster master, bool isCatastrophe = false)
        {
            if (master == null)
                return;

            ApplyOverchargeToBody(master.GetBody(), isCatastrophe);
        }
        private static void ApplyOverchargeToBody(CharacterBody body, bool isCatastrophe = false)
        {
            if (body == null)
                return;

            body.AddTimedBuff(RoR2Content.Buffs.Cripple, 9999);
            body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, 9999);
            HealthComponent hc = body.healthComponent;
            if (hc && hc.health > 1)
            {
                DamageInfo di = new DamageInfo();
                di.damage = (body.maxHealth + body.maxShield) * 0.01f * Time.fixedDeltaTime;
                if (isCatastrophe)
                    di.damage *= 0.5f;
                di.damageType = new DamageTypeCombo(DamageType.Silent,
                    DamageTypeExtended.Generic, DamageSource.NoneSpecified);
                di.damageType |= DamageType.BypassArmor;
                di.damageType |= DamageType.BypassBlock;
                di.procCoefficient = 1;
                di.position = body.corePosition;
                hc.TakeDamage(di);
            }
        }
        public static void CancelPityCharge(TeleporterInteraction self)
        {
            if (pityChargeOn)
            {
                pityChargeOn = false;
                pityChargeShrinkDelta = 0;
                pityChargeRecolorDelta = 0;
                self.holdoutZoneController.calcColor -= PityChargeCalcColor;
                self.holdoutZoneController.calcRadius -= PityChargeCalcRadius;
            }
        }

        public static void PityChargeOnExit(On.RoR2.TeleporterInteraction.ChargingState.orig_OnExit orig, TeleporterInteraction.ChargingState self)
        {
            orig(self);
            CancelPityCharge(self.teleporterInteraction);
        }

        private static void PityChargeOnExitCatastrophe(On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState.orig_OnExit orig, EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState self)
        {
            orig(self);
            CancelPityCharge(self.teleporterInteraction);
        }

        public static void PityChargeCalcRadius(ref float radius)
        {
            radius = Mathf.Max(radius * (1 - pityChargeShrinkDelta), 10f);
        }

        public static void PityChargeCalcColor(ref Color color)
        {
            color = HoldoutZoneController.FocusConvergenceController.convergenceMaterialColor;
        }

        private static void WeakenBossPostCatastopheCharge(On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.ChargingState.orig_FixedUpdate orig, EntityStates.TeleporterCatastrophe.DLC4.Teleporter.ChargingState baseState)
        {
            orig(baseState);
            PerformPityCharge(baseState.teleporterInteraction);
        }

        private static void WeakenBossPostCatastopheCharge2(On.EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState.orig_FixedUpdate orig, EntityStates.TeleporterCatastrophe.DLC4.Teleporter.OverchargingState baseState)
        {
            orig(baseState);
            PerformPityCharge(baseState.teleporterInteraction, true, baseState.extraBossGroupServer);
        }
        public static void WeakenBossPostTpCharge(On.RoR2.TeleporterInteraction.ChargingState.orig_FixedUpdate orig, RoR2.TeleporterInteraction.ChargingState baseState)
        {
            orig(baseState);
            PerformPityCharge(baseState.teleporterInteraction);
        }
        #endregion

        #region directors
        public static float playerSpawnRateFactor = 0.5f;//0.5f, linear
        public static float difficultySpawnRateFactor = 0.4f;//0.4f, additive
        /// <summary>
        /// elite bias is inversed (lower bias = more elites)
        /// </summary>
        public static float fastDirectorEliteBias = 0.75f;//1
        public static float fastDirectorCreditMultiplier = 0.75f;//0.75f
        /// <summary>
        /// elite bias is inversed (lower bias = more elites)
        /// </summary>
        public static float slowDirectorEliteBias = 0.5f;//1
        public static float slowDirectorCreditMultiplier = 1f;//0.75f

        /// <summary>
        /// elite bias is inversed (lower bias = more elites)
        /// </summary>
        public static float teleLesserEliteBias = 0.5f;//1
        public static float teleLesserCreditMultiplier = 0.8f;//1f
        /// <summary>
        /// elite bias is inversed (lower bias = more elites)
        /// </summary>
        public static float teleBossEliteBias = 1f;//1
        public static float teleBossCreditMultiplier = 1.0f;//1f
        /// <summary>
        /// unused
        /// </summary>
        public static float teleBossCreditMultiplierStage1 = 0.5f;//1f
        /// <summary>
        /// Elite Bias, Credit Income
        /// </summary>
        public static void ChangeDirectorStats()
        {
            GameObject baseDirector = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Director.prefab").WaitForCompletion();
            CombatDirector[] directors1 = baseDirector.GetComponents<CombatDirector>();
            if (directors1.Length > 0)
            {
                CombatDirector fastDirector = directors1[0];
                if (fastDirector != null)
                {
                    fastDirector.eliteBias = fastDirectorEliteBias;
                    fastDirector.creditMultiplier = fastDirectorCreditMultiplier;
                }

                CombatDirector slowDirector = directors1[1];
                if (slowDirector != null)
                {
                    slowDirector.eliteBias = slowDirectorEliteBias;
                    slowDirector.creditMultiplier = slowDirectorCreditMultiplier;
                }
            }
            On.RoR2.CombatDirector.Awake += AdjustTpDirectors;
            On.RoR2.CombatDirector.SetNextSpawnAsBoss += FixBossDirectorCredits;
            IL.RoR2.CombatDirector.DirectorMoneyWave.Update += DirectorCreditGainChanges;
            //On.RoR2.TeleporterInteraction.Awake += AdjustDirectorsForTeleporter;
            //GameObject teleporterDefault = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Teleporters.Teleporter1_prefab).WaitForCompletion();
            //GameObject teleporterLunar = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Teleporters.LunarTeleporter_Variant_prefab).WaitForCompletion();
            //AdjustTeleporterDirectors(teleporterLunar.GetComponents<CombatDirector>());

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

        private static void FixBossDirectorCredits(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            self.monsterCredit *= teleBossCreditMultiplier;
            orig(self);
        }

        private static void AdjustTpDirectors(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector director)
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

        static void AdjustTpBossDirector(CombatDirector director)
        {
            director.eliteBias = teleBossEliteBias;
            director.creditMultiplier = teleBossCreditMultiplier;
            if (Run.instance.stageClearCount == 0)
                director.creditMultiplier *= teleBossCreditMultiplierStage1;
        }
        static void AdjustTpMonsterDirector(CombatDirector director)
        {
            director.eliteBias = teleLesserEliteBias;
            director.creditMultiplier = teleLesserCreditMultiplier;
        }
        /// <summary>
        /// deprecated
        /// </summary>
        private static void AdjustDirectorsForTeleporter(On.RoR2.TeleporterInteraction.orig_Awake orig, TeleporterInteraction self)
        {
            AdjustTpBossDirector(self.bossDirector);
            AdjustTpBossDirector(self.companionBoss);
            AdjustTpMonsterDirector(self.bonusDirector);
            orig(self);
        }
        #region Stage Credits
        public static float monsterCreditsMultiplier = 1.0f;
        public static void IncreaseStageMonsterCredits(DirectorAPI.StageSettings settings, DirectorAPI.StageInfo currentStage)
        {
            settings.SceneDirectorMonsterCredits = (int)(settings.SceneDirectorMonsterCredits * monsterCreditsMultiplier);
        }
        #endregion
        #endregion
    }
}
