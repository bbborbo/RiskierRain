﻿using BepInEx.Configuration;
using static EntityStates.BrotherMonster.Weapon.FireLunarShards;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static RiskierRainContent.CoreModules.StatHooks;
using UnityEngine.AddressableAssets;
using System.Collections;
using RoR2.ExpansionManagement;

namespace RiskierRainContent.Items
{
    class LunarIncreaseCD : ItemBase<LunarIncreaseCD>
    {
        GameObject lunarShardProjectile => EntityStates.BrotherMonster.Weapon.FireLunarShards.projectilePrefab;//LegacyResourcesAPI.Load<GameObject>("RoR2/Base/Brother/LunarShardProjectile.prefab");
        GameObject lunarShardMuzzleFlash => EntityStates.BrotherMonster.Weapon.FireLunarShards.muzzleFlashEffectPrefab;//LegacyResourcesAPI.Load<GameObject>("RoR2/Base/Brother/MuzzleflashLunarShard.prefab");
        float lunarShardDamageCoefficient = 0.1f;
        float lunarShardProcCoefficient = 0.5f;

        float cdIncreaseBase = 2;
        float cdIncreaseStack = 2;
        float shardsPerSecondBase = 0.5f;
        float shardsPerSecondStack = 0;

        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;

        public override string ItemName => "Elegy of Extinction";

        public override string ItemLangTokenName => "LUNARINCREASECD";

        public override string ItemPickupDesc => "Using skills launches lunar shards; longer cooldown skills launch more shards. All cooldowns are increased.";

        public override string ItemFullDescription => "TBA";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/LunarPortalOnUse/PickupLunarPortalOnUse.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/LunarPortalOnUse/texLunarPortalOnUseIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Equipment.LunarPortalOnUse);
            On.RoR2.GenericSkill.CalculateFinalRechargeInterval += EnableCooldownAddition; //move this
            On.RoR2.CharacterBody.OnSkillActivated += FireShards;
            On.RoR2.CharacterBody.RecalculateStats += IncreaseCDs;
            RecalculateStatsAPI.GetStatCoefficients += IncreaseCooldowns;
        }

        private void IncreaseCooldowns(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                float cdIncreaseAmount = cdIncreaseBase + (cdIncreaseStack * itemCount - 1);

                args.cooldownReductionAdd -= cdIncreaseAmount;

                //SkillLocator skillLocator = sender.skillLocator;
                //if (skillLocator != null)
                //{
                //    skillLocator.primary.flatCooldownReduction -= cdIncreaseAmount;
                //    skillLocator.secondary.flatCooldownReduction -= cdIncreaseAmount;
                //    skillLocator.utility.flatCooldownReduction -= cdIncreaseAmount;
                //    skillLocator.special.flatCooldownReduction -= cdIncreaseAmount;
                //}
            }
        }

        private float EnableCooldownAddition(On.RoR2.GenericSkill.orig_CalculateFinalRechargeInterval orig, GenericSkill self)
        {
            //return self.baseRechargeInterval > 0 ? Mathf.Max(0.5f, self.baseRechargeInterval * self.cooldownScale - self.flatCooldownReduction) : 0

            float calculatedRechargeInterval = self.baseRechargeInterval * self.cooldownScale - self.flatCooldownReduction;

            if (self.baseRechargeInterval <= 0 && calculatedRechargeInterval <= self.baseRechargeInterval)
                return 0;

            return Mathf.Max(0.5f, calculatedRechargeInterval);
        }

        private void IncreaseCDs(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            SkillLocator skillLocator = self.skillLocator;
            if(skillLocator != null)
            {
                GenericSkill primary = skillLocator.primary;
                if(primary != null)
                {
                    //float calculatedCooldown = primary.recha
                    //if(primary.cooldownRemaining)
                }
            }
        }

        private void FireShards(On.RoR2.CharacterBody.orig_OnSkillActivated orig, RoR2.CharacterBody self, RoR2.GenericSkill skill)
        {
            orig(self, skill);
            int itemCount = GetCount(self);
            if (itemCount > 0)
            {
                float shardsPerSecond = shardsPerSecondBase + (shardsPerSecondStack * itemCount);
                float skillCD = skill.finalRechargeInterval;
                float skillStock = skill.rechargeStock;
                //Util.PlaySound(FireVoidspikes.attackSoundString, self.gameObject);
                if (lunarShardMuzzleFlash != null)
                {
                    EffectManager.SimpleMuzzleFlash(lunarShardMuzzleFlash, self.gameObject, "Head", false);
                }
                else
                {
                    Debug.Log("muzzleflash null");
                }
                Ray aimRay = new Ray(self.inputBank.aimOrigin, self.inputBank.aimDirection);

                int shardCount = (int)Mathf.CeilToInt(shardsPerSecond * skillCD / skillStock);
                if (lunarShardProjectile != null)
                {
                    for (int i = 0; i < shardCount; i++)
                    {
                        float bonusYaw = UnityEngine.Random.Range(-1f, 1f);
                        float bonusPitch = UnityEngine.Random.Range(-1f, 1f);
                        float projectileSpeed = 200 * (0.3f * (i + 1));

                        Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 0f, 1f, 1f, bonusYaw * shardCount, bonusPitch * i);
                        ProjectileManager.instance.FireProjectile(lunarShardProjectile, aimRay.origin,
                            Util.QuaternionSafeLookRotation(forward), self.gameObject,
                            self.damage * lunarShardDamageCoefficient, 0f,
                            Util.CheckRoll(self.crit, self.master),
                            DamageColorIndex.Default, null, projectileSpeed);
                    }
                }
                else
                {
                    Debug.Log("lunarshard null");
                }
                
            }
        }


        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
            LoadItemBehavior();
        }

        public void LoadItemBehavior()
        {
            //lunarShardProjectile = LegacyResourcesAPI.Load<GameObject>("RoR2/Base/Brother/LunarShardProjectile.prefab");
            //lunarShardMuzzleFlash = LegacyResourcesAPI.Load<GameObject>("RoR2/Base/Brother/MuzzleflashLunarShard.prefab");
        }

    }
}
