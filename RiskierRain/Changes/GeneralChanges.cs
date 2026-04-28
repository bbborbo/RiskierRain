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

namespace RiskierRain.Changes
{
    public static class GeneralChanges
    {
        public static void Initialize()
        {
            RemoveOspForever();
            RemoveAspdScalingOnCooldownsForever();
            AddPotentialProtection();
            AddPityCharge();
            FixPickupStats();
            DoSacrificeDropLimit();
        }
        #region oneshot protection aka osp
        public static void RemoveOspForever()
        {
            // removes one-shot protection (OSP)
            Hook hookTuah = new Hook(
              typeof(CharacterBody).GetMethod("get_hasOneShotProtection", (BindingFlags)(-1)),
              typeof(RiskierRainPlugin).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );
        }

        public static bool ReflectOnThatThang(orig_getHasOneShotProtection orig, CharacterBody self)
        {
            return false;
        }
        public delegate bool orig_getHasOneShotProtection(CharacterBody self);
        #endregion
        #region aspd scaling on cooldowns
        public static void RemoveAspdScalingOnCooldownsForever()
        {
            IL.RoR2.GenericSkill.RunRecharge += FuckAspdScalingOnCooldowns;
            IL.RoR2.Skills.SkillDef.GetRechargeInterval += FuckAspdScalingOnCooldowns;

            void FuckAspdScalingOnCooldowns(ILContext il)
            {
                ILCursor c = new ILCursor(il);

                bool ilFound = c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<RoR2.Skills.SkillDef>(nameof(RoR2.Skills.SkillDef.attackSpeedBuffsRestockSpeed))
                    );
                if (!ilFound)
                {
                    DebugBreakpoint(nameof(FuckAspdScalingOnCooldowns));
                    return;
                }
                c.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
            }
        }
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
    }
}