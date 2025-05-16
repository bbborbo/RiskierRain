using BepInEx.Configuration;
using static EntityStates.BrotherMonster.Weapon.FireLunarShards;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static MoreStats.OnHit;
using UnityEngine.AddressableAssets;
using System.Collections;
using RoR2.ExpansionManagement;
using static SwanSongExtended.Modules.Language.Styling;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    class LunarIncreaseCD : ItemBase<LunarIncreaseCD>
    {
        GameObject lunarShardProjectile;// => EntityStates.BrotherMonster.Weapon.FireLunarShards.projectilePrefab;//LegacyResourcesAPI.Load<GameObject>("RoR2/Base/Brother/LunarShardProjectile.prefab");
        GameObject lunarShardMuzzleFlash => EntityStates.BrotherMonster.Weapon.FireLunarShards.muzzleFlashEffectPrefab;//LegacyResourcesAPI.Load<GameObject>("RoR2/Base/Brother/MuzzleflashLunarShard.prefab");
        
        [AutoConfig("Damage Coefficient", 1.2f)]
        public static float shardDamageCoefficient = 1.2f;
        [AutoConfig("Proc Coefficient", 0.5f, desc = "Vanilla is 1.0")]
        public static float shardProcCoefficient = 0.5f;
        [AutoConfig("Shard Steer Speed", 35, desc = "Vanilla is 20")]
        public static float shardSteerSpeed = 35; //20
        [AutoConfig("Cooldown Increase Base", 1)]
        public static float cdIncreaseBase = 1;
        [AutoConfig("Cooldown Increase Stack", 1)]
        public static float cdIncreaseStack = 1;
        [AutoConfig("Seconds Per Shard Base", 1)]
        public static float secondsPerShardBase = 1;
        [AutoConfig("Seconds Per Shard Reduction Per Stack", 0.2f)]
        public static float secondsPerShardReductionStack = 0.2f;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override string ItemName => "Elegy of Extinction";

        public override string ItemLangTokenName => "LUNARINCREASECD";

        public override string ItemPickupDesc => $"All skills fire lunar shards... {RedText($"BUT all skills have increased cooldowns")}";

        public override string ItemFullDescription => $"On any skill use, fire {DamageColor("lunar shards")} " +
            $"for {DamageValueText(shardDamageCoefficient)} (increases with ability cooldown). " +
            $"{RedText($"Increase the cooldowns of all skills by {cdIncreaseBase} second")} {StackText($"+{cdIncreaseStack}")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/LunarPortalOnUse/PickupLunarPortalOnUse.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/LunarPortalOnUse/texLunarPortalOnUseIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            CreateProjectile();
            base.Init();
        }

        private void CreateProjectile()
        {
            lunarShardProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/LunarShardProjectile.prefab").WaitForCompletion();

            ProjectileImpactExplosion pie = lunarShardProjectile.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.blastDamageCoefficient = 1;
                pie.blastProcCoefficient = shardProcCoefficient;
            }
            ProjectileSteerTowardTarget steer = lunarShardProjectile.GetComponent<ProjectileSteerTowardTarget>();
            if (steer)
            {
                steer.rotationSpeed = shardSteerSpeed;
            }

            Content.AddProjectilePrefab(lunarShardProjectile);
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
                float secondsPerShard = secondsPerShardBase;
                if (itemCount > 1)
                    secondsPerShard *= Mathf.Pow(1 - secondsPerShardReductionStack, itemCount - 1);

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

                int shardCount = (int)Mathf.CeilToInt(skillCD / (skillStock * secondsPerShard));
                if (lunarShardProjectile != null)
                {
                    bool crit = Util.CheckRoll(self.crit, self.master);
                    for (int i = 0; i < shardCount; i++)
                    {
                        float bonusYaw = UnityEngine.Random.Range(-1f, 1f);
                        float bonusPitch = UnityEngine.Random.Range(-1f, 1f);
                        float projectileSpeed = 200 * (0.3f * (i + 1));

                        Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 0f, 1f, 1f, bonusYaw * i, bonusPitch * i);
                        FireProjectileInfo fpi = new FireProjectileInfo
                        {
                            projectilePrefab = lunarShardProjectile,
                            position = aimRay.origin,
                            rotation = Util.QuaternionSafeLookRotation(forward),
                            owner = self.gameObject,
                            damage = self.damage * shardDamageCoefficient,
                            force = 0,
                            crit = crit,
                            damageColorIndex = DamageColorIndex.Item,
                            speedOverride = projectileSpeed,
                            useSpeedOverride = true
                        };
                        ProjectileManager.instance.FireProjectile(fpi);
                    }
                }
                else
                {
                    Debug.Log("lunarshard null");
                }
                
            }
        }
    }
}
