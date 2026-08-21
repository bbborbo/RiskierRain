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
        public static float potentialProtectionDuration = 4;
        public static void AddPotentialProtection()
        {
            On.RoR2.UI.PickupPickerPanel.Awake += CommandOrPotentialArmor;
            void CommandOrPotentialArmor(On.RoR2.UI.PickupPickerPanel.orig_Awake orig, RoR2.UI.PickupPickerPanel self)
            {
                RoR2.LocalUser user = RoR2.LocalUserManager.GetFirstLocalUser();
                RoR2.CharacterBody body = user.cachedBody;
                body.AddTimedBuffAuthority(GetBuffIndex(), potentialProtectionDuration);
                orig(self);

                BuffIndex GetBuffIndex()
                {
                    if (potentialProtectionVisibility == true)
                        return RoR2.RoR2Content.Buffs.Immune.buffIndex;
                    return RoR2.RoR2Content.Buffs.HiddenInvincibility.buffIndex;
                }
            };
        }
        #endregion
        #region pity charge / teleporter overcharge
        public static void AddPityCharge()
        {
            On.RoR2.TeleporterInteraction.ChargingState.FixedUpdate += WeakenBossPostTpCharge;
            On.RoR2.TeleporterInteraction.ChargingState.OnExit += PityChargeOnExit;
        }

        public static void PityChargeOnExit(On.RoR2.TeleporterInteraction.ChargingState.orig_OnExit orig, TeleporterInteraction.ChargingState self)
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

        public static void PityChargeCalcRadius(ref float radius)
        {
            radius = Mathf.Max(radius * (1 - pityChargeShrinkDelta), 10f);
        }

        public static void PityChargeCalcColor(ref Color color)
        {
            color = HoldoutZoneController.FocusConvergenceController.convergenceMaterialColor;
        }

        public static bool pityChargeOn = false;
        public static float pityChargeShrinkDelta = 0;
        public static float pityChargeRecolorDelta = 0;
        public static void WeakenBossPostTpCharge(On.RoR2.TeleporterInteraction.ChargingState.orig_FixedUpdate orig, RoR2.TeleporterInteraction.ChargingState baseState)
        {
            orig(baseState);

            if (!SwanSongExtended.Storms.StormRunBehavior.IsStormStage(Stage.instance.sceneDef))
                return;
            TeleporterInteraction.ChargingState self = baseState as TeleporterInteraction.ChargingState;
            if (self.teleporterInteraction.holdoutZoneController.charge >= 1f)
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

        #region directors
        public static float fastDirectorEliteBias = 1.5f;//1
        public static float fastDirectorCreditMultiplier = 0.75f;//0.75f
        public static float slowDirectorEliteBias = 1.5f;//1
        public static float slowDirectorCreditMultiplier = 1f;//0.75f

        public static float teleLesserEliteBias = 1.2f;//1
        public static float teleLesserCreditMultiplier = 0.8f;//1f
        public static float teleBossEliteBias = 1f;//1
        public static float teleBossCreditMultiplier = 1.0f;//1f
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
            //On.RoR2.TeleporterInteraction.Awake += AdjustDirectorsForTeleporter;
            //GameObject teleporterDefault = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Teleporters.Teleporter1_prefab).WaitForCompletion();
            //GameObject teleporterLunar = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Teleporters.LunarTeleporter_Variant_prefab).WaitForCompletion();
            //AdjustTeleporterDirectors(teleporterLunar.GetComponents<CombatDirector>());

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
