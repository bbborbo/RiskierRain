using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class Permafrost : ItemBase
    {
        float freezeChancePerPercentBase = 1;
        float freezeChancePerPercentStack = 2;
        float freezeDamageHealthFraction = 0.05f;
        float freezeProcCoefficient = 0.75f;

        public override string ItemName => "Permafrost";

        public override string ItemLangTokenName => "GOODEXECUTIONITEM";

        public override string ItemPickupDesc => "Chance to Freeze enemies on heavy hits, instantly killing them at low health.";

        public override string ItemFullDescription => 
            $"<style=cIsDamage>{freezeChancePerPercentBase + freezeChancePerPercentStack}%</style> " +
            $"<style=cStack>(+{freezeChancePerPercentStack}% per stack)</style> chance on hit " +
            $"<style=cIsDamage>per % of enemy maximum health dealt in damage</style> " +
            $"to <style=cIsUtility>Freeze</style> enemies in place for <style=cIsUtility>{freezeProcCoefficient * 2} seconds. </style>" +
            $"Frozen enemies are <style=cIsHealth>instantly killed</style> at low health.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };
        public override BalanceCategory Category => BalanceCategory.StateOfInteraction;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += PermafrostBehavior;
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void PermafrostBehavior(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig(self, damageInfo);

            bool isFreeze = (damageInfo.damageType & DamageType.Freeze2s) > DamageType.Generic;
            bool isPermafrost = isFreeze && damageInfo.procCoefficient == freezeProcCoefficient;
            if (self.alive && damageInfo.attacker != null && !isPermafrost)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                if (attackerBody != null && damageInfo.procCoefficient > 0)
                {
                    CharacterMaster attackerMaster = attackerBody.master;
                    int permafrostCount = GetCount(attackerBody);
                    if (permafrostCount > 0)
                    {
                        float victimMaxHealth = self.fullCombinedHealth;
                        float attackEndDamage = damageInfo.damage;

                        float maxHealthFractionDealt = (attackEndDamage / victimMaxHealth) * 100;
                        float endFreezeChance = freezeChancePerPercentBase + freezeChancePerPercentStack * permafrostCount;

                        if (Util.CheckRoll(maxHealthFractionDealt * endFreezeChance * damageInfo.procCoefficient, attackerBody.master))
                        {
                            DamageInfo freezeHit = new DamageInfo()
                            {
                                attacker = damageInfo.attacker,
                                crit = damageInfo.crit,
                                damage = victimMaxHealth * freezeDamageHealthFraction,
                                damageType = DamageType.Freeze2s,
                                force = Vector3.zero,
                                position = self.transform.position,
                                procChainMask = damageInfo.procChainMask,
                                procCoefficient = freezeProcCoefficient
                            };

                            self.TakeDamage(freezeHit);
                        }
                    }
                }
            }
        }
    }
}
