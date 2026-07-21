using BepInEx;
using R2API;
using RoR2;
using RoR2.Audio;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BossDropRework
{
    public partial class BossDropReworkPlugin : BaseUnityPlugin
    {
        public static BuffDef NoBossDropsBuff;
        public static BuffDef YesBossDropsBuff;

        public static float trophyHunterMaxHealthDamage => trophyHunterCurseCount / (100f + trophyHunterCurseCount);
        public static int trophyHunterCurseCount = 33;
        public static int trophyHunterDebuffDuration = 999;
        public static bool reworkTricorn = true;

        void TricornRework()
        {
            On.RoR2.EquipmentSlot.FireBossHunter += FireTricornFix;
            ModifyBossItemDropChance += TricornDropChance;

            LanguageAPI.Add("EQUIPMENT_BOSSHUNTER_PICKUP", "Cripple a large monster and claim its <style=cIsDamage>trophy</style>. Consumed on use.");
            LanguageAPI.Add("EQUIPMENT_BOSSHUNTER_DESC", 
                $"Targets any enemy capable of dropping a <style=cIsDamage>trophy item</style>. " +
                $"Blast it for <style=cIsDamage>{Mathf.CeilToInt(trophyHunterMaxHealthDamage * 100)}% max health</style>, " +
                $"instantly severing its <style=cIsDamage>trophy item</style>, and Crippling and Hemorrhaging it " +
                $"for <style=cIsUtility>{trophyHunterDebuffDuration}</style> seconds."
                );
            //"<style=cIsDamage>Execute</style> any enemy capable of spawning a <style=cIsDamage>unique reward</style>,
            //and it will drop that <style=cIsDamage>item</style>. Equipment is <style=cIsUtility>consumed</style> on use.");
        }

        private void TricornDropChance(CharacterBody victim, CharacterBody attacker, ref float dropChance)
        {
            if (victim.HasBuff(NoBossDropsBuff) && dropChance != 0)
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
                                destroyTricorn = true;
                            }
                            /*bool hasScalpel = (self.characterBody.inventory.GetItemCountEffective(DisposableScalpel.instance.ItemsDef) > 0);
                            if (hasScalpel)
                            {
                                DisposableScalpel.ConsumeScalpel(attackerBody);
                                enemyBody.AddBuff(CoreModules.Assets.bossHunterDebuffWithScalpel);
                            }*/

                            if (reworkTricorn)
                            {
                                //DamageInfo damageInfo = new DamageInfo();
                                //damageInfo.attacker = self.gameObject;
                                //damageInfo.force = normalized * 1500f;
                                //damageInfo.damage = attackerBody.damage * TricornDamageCoefficient.Value;
                                //damageInfo.procCoefficient = TricornProcCoefficient.Value;
                                //enemyHealthComponent.TakeDamage(damageInfo);

                                DropItem(attackerBody, enemyBody, attackerBody.master, 100);

                                enemyBody.AddBuff(NoBossDropsBuff);
                                for (int i = 0; i < trophyHunterCurseCount; i++)
                                {
                                    enemyBody.AddBuff(RoR2Content.Buffs.PermanentCurse);
                                }
                                enemyBody.AddTimedBuffAuthority(RoR2Content.Buffs.Cripple.buffIndex, trophyHunterDebuffDuration);
                                DotController.InflictDot(enemyHealthComponent.gameObject, attackerBody.gameObject, hurtBox,
                                    DotController.DotIndex.SuperBleed, trophyHunterDebuffDuration, 1f);
                                FakeEnemyDeath(enemyBody);
                            }
                            else
                            {
                                enemyBody.AddBuff(YesBossDropsBuff);
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
                            CharacterMasterNotificationQueue.SendTransformNotification(self.characterBody.master,
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

        private void FakeEnemyDeath(CharacterBody enemyBody)
        {
            //death ak event
            if (enemyBody.TryGetComponent(out CharacterDeathBehavior deathBehavior))
            {
                if (deathBehavior.deathAkEvent != null)
                {
                    deathBehavior.deathAkEvent.Post(deathBehavior.gameObject);
                }
            }
            //death sound
            SfxLocator sfxLocator = enemyBody.sfxLocator;
            Transform cachedModelTransform = enemyBody.modelLocator.modelBaseTransform;
            if (sfxLocator && sfxLocator.deathSound != "" && cachedModelTransform != null)
            {
                PointSoundManager.EmitSoundLocal(
                    sfxLocator.deathSound,
                    cachedModelTransform.gameObject ? cachedModelTransform.gameObject.transform.position : base.gameObject.transform.position
                    );
            }

            //death gibs
            GameObject deathEffectPrefab = GetDeathEffectPrefab(enemyBody);
            if (deathEffectPrefab == null)
                return;
            if (!EffectManager.ShouldUsePooledEffect(deathEffectPrefab))
            {
                EffectManager.SimpleEffect(deathEffectPrefab, enemyBody.transform.position, enemyBody.transform.rotation, false);
                return;
            }
            EffectManager.GetAndActivatePooledEffect(deathEffectPrefab, enemyBody.transform.position, enemyBody.transform.rotation);
        }

        private GameObject GetDeathEffectPrefab(CharacterBody enemyBody)
        {
            string path = "";
            switch (enemyBody.baseNameToken)
            {
                case "TITAN_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Titan.TitanDeathEffect_prefab;
                    break;
                case "TITANGOLD_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Titan.TitanGoldDeathEffect_prefab;
                    break;
                case "BEETLEQUEEN_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BeetleQueen.BeetleQueenDeathImpact_prefab;
                    break;
                case "CLAYBOSS_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ClayBoss.ClayBossDeath_prefab;
                    break;
                case "GRANDPARENT_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Grandparent.GrandparentDeathEffect_prefab;
                    break;
                case "GRAVEKEEPER_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Gravekeeper.GravekeeperDeathImpact_prefab;
                    break;
                case "IMPBOSS_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ImpBoss.ImpBossDeathEffect_prefab;
                    break;
                case "MAGMAWORM_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_MagmaWorm.MagmaWormDeath_prefab;
                    break;
                case "ELECTRICWORM_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_MagmaWorm.MagmaWormDeath_prefab;
                    break;
                case "SCAV_BODY_NAME":
                    path = "";
                    break;
                case "ROBOBALLBOSS_BODY_NAME":
                case "SUPERROBOBALLBOSS_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_RoboBallBoss.OmniExplosionVFXRoboBallBossDeath_prefab;
                    break;
                case "VAGRANT_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Vagrant.VagrantDeathExplosion_prefab;
                    break;
                case "MEGACONSTRUCT_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_MajorAndMinorConstruct.MajorConstructDeathEffect_prefab;
                    break;
                case "VOIDMEGACRAB_BODY_NAME":
                    path = "";// RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidMegaCrab.VoidMegaCrabDeathExplosion_prefab;
                    break;
                case "SOLUSAMALGAMATOR_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_SolusAmalgamator.SolusAmalgamatorExplosionVFXDeath_prefab;
                    break;
                case "VULTUREHUNTER_BODY_NAME":
                    path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_VultureHunter.VultureHunterSkyDeath_prefab;
                    break;
            }
            if (string.IsNullOrEmpty(path))
                return null;
            return Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();
        }
    }
}
