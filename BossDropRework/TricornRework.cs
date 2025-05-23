﻿using BepInEx;
using R2API;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;

namespace BossDropRework
{
    public partial class BossDropReworkPlugin : BaseUnityPlugin
    {
        public static BuffDef bossHunterDebuff;

        int tricornDebuffDuration = 999;
        bool reworkTricorn = true;

        void TricornRework()
        {
            On.RoR2.EquipmentSlot.FireBossHunter += FireTricornFix;
            ModifyBossItemDropChance += TricornDropChance;

            LanguageAPI.Add("EQUIPMENT_BOSSHUNTER_PICKUP", "Cripple a large monster and claim its <style=cIsDamage>trophy</style> after it dies. Consumed on use.");
            LanguageAPI.Add("EQUIPMENT_BOSSHUNTER_DESC", 
                $"Targets any enemy capable of dropping a <style=cIsDamage>unique reward</style>, " +
                $"dealing <style=cIsDamage>{TricornDamageCoefficient.Value * 100}% damage</style>, " +
                $"then <style=cIsUtility>Crippling and Hemorrhaging</style> it " +
                $"for <style=cIsUtility>{tricornDebuffDuration}</style> seconds. " +
                $"When the enemy dies, it has a 100% chance to drop it's <style=cIsDamage>trophy</style>. " +
                $"Equipment is <style=cIsUtility>consumed</style> on use.");
            //"<style=cIsDamage>Execute</style> any enemy capable of spawning a <style=cIsDamage>unique reward</style>,
            //and it will drop that <style=cIsDamage>item</style>. Equipment is <style=cIsUtility>consumed</style> on use.");
        }

        private void TricornDropChance(CharacterBody victim, CharacterBody attacker, ref float dropChance)
        {
            if (victim.HasBuff(bossHunterDebuff) && dropChance != 0)
            {
                dropChance = 100;
            }
        }

        public delegate void TricornFireHandler(CharacterBody attacker, CharacterBody victim, ref bool shouldFire);
        public static event TricornFireHandler ShouldTricornFireAndBreak;
        public static bool GetTricornFireAndBreak(CharacterBody attacker, CharacterBody victim, ref bool shouldFire)
        {
            ShouldTricornFireAndBreak?.Invoke(attacker, victim, ref shouldFire);
            return shouldFire;
        }

        private bool FireTricornFix(On.RoR2.EquipmentSlot.orig_FireBossHunter orig, EquipmentSlot self)
        {
            self.UpdateTargets(DLC1Content.Equipment.BossHunter.equipmentIndex, true);
            HurtBox hurtBox = self.currentTarget.hurtBox;
            DeathRewards deathRewards2 = GetDeathRewardsFromTarget(hurtBox);
            //Debug.Log($"Hurtbox valid {hurtBox != null}, Death reward valid {deathRewards2 != null}");
            if (hurtBox && deathRewards2)
            {
                HealthComponent enemyHealthComponent = hurtBox.healthComponent;
                if (enemyHealthComponent != null)
                {
                    CharacterBody attackerBody = self.characterBody;
                    CharacterBody enemyBody = enemyHealthComponent.body;
                    if (enemyBody != null && attackerBody != null)
                    {
                        bool destroyTricorn = false;
                        Vector3 vector = enemyBody ? enemyBody.corePosition : Vector3.zero;
                        Vector3 normalized = (vector - attackerBody.corePosition).normalized;

                        UnityEngine.Object exists = exists = ((enemyBody != null) ? enemyBody.master : null);
                        if (exists)
                        {
                            //hurtBox.healthComponent.body.master.TrueKill(base.gameObject, null, DamageType.Generic);
                            //destroyTricorn = true;
                            bool shouldFire = true; 
                            if(GetTricornFireAndBreak(attackerBody, enemyBody, ref shouldFire))
                            {
                                enemyBody.AddBuff(bossHunterDebuff);
                                destroyTricorn = true;
                            }
                            /*bool hasScalpel = (self.characterBody.inventory.GetItemCount(DisposableScalpel.instance.ItemsDef) > 0);
                            if (hasScalpel)
                            {
                                DisposableScalpel.ConsumeScalpel(attackerBody);
                                enemyBody.AddBuff(CoreModules.Assets.bossHunterDebuffWithScalpel);
                            }*/

                            if (reworkTricorn)
                            {
                                DamageInfo damageInfo = new DamageInfo();
                                damageInfo.attacker = self.gameObject;
                                damageInfo.force = normalized * 1500f;
                                damageInfo.damage = attackerBody.damage * TricornDamageCoefficient.Value;
                                damageInfo.procCoefficient = TricornProcCoefficient.Value;
                                enemyHealthComponent.TakeDamage(damageInfo);

                                enemyBody.AddTimedBuffAuthority(RoR2Content.Buffs.Cripple.buffIndex, tricornDebuffDuration);
                                DotController.InflictDot(enemyHealthComponent.gameObject, damageInfo.attacker,
                                    DotController.DotIndex.SuperBleed, tricornDebuffDuration, 1f);
                            }
                            else
                            {
                                enemyBody.master.TrueKill(base.gameObject, null, default(DamageTypeCombo));
                            }
                        }

                        #region overlay fx
                        CharacterModel component = hurtBox.hurtBoxGroup.GetComponent<CharacterModel>();
                        if (component)
                        {
                            TemporaryOverlayInstance temporaryOverlay = TemporaryOverlayManager.AddOverlay(component.gameObject);
                            temporaryOverlay.duration = 0.1f;
                            temporaryOverlay.animateShaderAlpha = true;
                            temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                            temporaryOverlay.destroyComponentOnEnd = true;
                            temporaryOverlay.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
                            temporaryOverlay.AddToCharacterModel(component);
                            TemporaryOverlayInstance temporaryOverlay2 = TemporaryOverlayManager.AddOverlay(component.gameObject);
                            temporaryOverlay2.duration = 1.2f;
                            temporaryOverlay2.animateShaderAlpha = true;
                            temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                            temporaryOverlay2.destroyComponentOnEnd = true;
                            temporaryOverlay2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matGhostEffect");
                            temporaryOverlay2.AddToCharacterModel(component);
                        }
                        #endregion

                        #region knockback force
                        DamageInfo selfKnockbackForce = new DamageInfo();
                        selfKnockbackForce.attacker = self.gameObject;
                        selfKnockbackForce.force = -normalized * 2500f;
                        self.healthComponent.TakeDamageForce(selfKnockbackForce, true, false);
                        #endregion

                        #region gun fx
                        GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BossHunterKillEffect");
                        Quaternion rotation = Util.QuaternionSafeLookRotation(normalized, Vector3.up);
                        EffectManager.SpawnEffect(effectPrefab, new EffectData
                        {
                            origin = vector,
                            rotation = rotation
                        }, true);
                        #endregion

                        #region animation
                        ModelLocator component2 = base.gameObject.GetComponent<ModelLocator>();
                        CharacterModel characterModel;
                        if (component2 == null)
                        {
                            characterModel = null;
                        }
                        else
                        {
                            Transform modelTransform = component2.modelTransform;
                            characterModel = ((modelTransform != null) ? modelTransform.GetComponent<CharacterModel>() : null);
                        }
                        CharacterModel characterModel2 = characterModel;
                        if (characterModel2)
                        {
                            foreach (GameObject gameObject2 in characterModel2.GetEquipmentDisplayObjects(DLC1Content.Equipment.BossHunter.equipmentIndex))
                            {
                                if (gameObject2.name.Contains("DisplayTricorn"))
                                {
                                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BossHunterHatEffect"), new EffectData
                                    {
                                        origin = gameObject2.transform.position,
                                        rotation = gameObject2.transform.rotation,
                                        scale = gameObject2.transform.localScale.x
                                    }, true);
                                }
                                else
                                {
                                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BossHunterGunEffect"), new EffectData
                                    {
                                        origin = gameObject2.transform.position,
                                        rotation = Util.QuaternionSafeLookRotation(vector - gameObject2.transform.position, Vector3.up),
                                        scale = gameObject2.transform.localScale.x
                                    }, true);
                                }
                            }
                        }
                        #endregion

                        #region replace equipment
                        if (((attackerBody != null) ? attackerBody.inventory : null) && destroyTricorn == true)
                        {
                            CharacterMasterNotificationQueue.PushEquipmentTransformNotification(self.characterBody.master,
                                self.characterBody.inventory.currentEquipmentIndex, DLC1Content.Equipment.BossHunterConsumed.equipmentIndex,
                                CharacterMasterNotificationQueue.TransformationType.Default);
                            self.characterBody.inventory.SetEquipmentIndex(DLC1Content.Equipment.BossHunterConsumed.equipmentIndex);
                        }
                        #endregion
                        self.InvalidateCurrentTarget();
                        return true;
                    }
                }

            }
            return false;
        }
    }
}
