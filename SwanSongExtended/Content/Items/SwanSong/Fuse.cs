﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    class Fuse : ItemBase<Fuse>
    {
        public override string ConfigName => "Items : Fuse";
        public static GameObject fuseNovaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static BuffDef fuseRecharge;
        public static float fuseRechargeTime = 1;

        public static float baseShield = 40;
        public static float radiusBase = 16;
        public static float radiusStack = 4;

        public static float minStunDuration = 1f;
        public static float maxStunDuration = 6f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override string ItemName => "Volatile Fuse";

        public override string ItemLangTokenName => "BORBOFUSE";

        public override string ItemPickupDesc => "Creates a stunning nova when your shields break.";

        public override string ItemFullDescription => $"Gain <style=cIsHealing>{baseShield} shield</style> <style=cStack>(+{baseShield} per stack)</style>. " +
            $"<style=cIsUtility>Breaking your shields</style> creates a nova that " +
            $"<style=cIsUtility>Stuns</style> enemies within <style=cIsUtility>{radiusBase}m</style> " +
            $"<style=cStack>(+{radiusStack} per stack)</style>. " +
            $"<style=cIsDamage>Shock duration scales with shield health</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };
        //testing egg model
        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/egg.prefab");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            fuseRecharge = Content.CreateAndAddBuff(
                "bdFuseCooldown",
                LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffTeslaIcon"),
                Color.gray,
                false, true);
            fuseRecharge.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            fuseRecharge.isHidden = true;
            base.Init();
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += FuseTakeDamage;
            GetStatCoefficients += FuseShieldBonus;
        }

        private void FuseShieldBonus(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                args.baseShieldAdd += baseShield * itemCount;
            }
        }

        private void FuseTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            bool hadShieldBefore = HasShield(self);
            CharacterBody body = self.body;
            int fuseItemCount = GetCount(body);

            orig(self, damageInfo);

            if (hadShieldBefore && !HasShield(self) && self.alive)
            {
                if (fuseItemCount > 0 && !body.HasBuff(fuseRecharge))
                {
                    float maxShield = self.body.maxShield;
                    float maxHealth = self.body.maxHealth;
                    float shieldHealthFraction = maxShield / (maxHealth + maxShield);

                    float currentRadius = radiusBase + radiusStack * (fuseItemCount - 1);

                    EffectManager.SpawnEffect(fuseNovaEffectPrefab, new EffectData
                    {
                        origin = self.transform.position,
                        scale = currentRadius
                    }, true);
                    BlastAttack fuseNova = new BlastAttack()
                    {
                        baseDamage = self.body.damage,
                        radius = currentRadius,
                        procCoefficient = Mathf.Lerp(minStunDuration, maxStunDuration, shieldHealthFraction),
                        position = self.transform.position,
                        attacker = self.gameObject,
                        crit = Util.CheckRoll(self.body.crit, self.body.master),
                        falloffModel = BlastAttack.FalloffModel.None,
                        damageType = DamageType.Stun1s,
                        teamIndex = TeamComponent.GetObjectTeam(self.gameObject)
                    };
                    fuseNova.Fire();

                    self.body.AddTimedBuffAuthority(fuseRecharge.buffIndex, fuseRechargeTime);
                }
            }
        }

        public static bool HasShield(HealthComponent hc)
        {
            return hc.shield > 1;
        }
    }
}
