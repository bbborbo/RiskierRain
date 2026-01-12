using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class Permafrost : ItemBase<Permafrost>
    {
        public static float freezeChancePerPercentBase = 1;
        public static float freezeChancePerPercentStack = 2;
        public static float freezeDamageHealthFraction = 0.05f;
        public static float freezeProcCoefficient = 0.75f;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
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

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlPermafrost.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/permafrost.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
        }
    }

    public class PermafrostBehavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => Permafrost.instance.ItemsDef;

        public void OnDamageDealtServer(DamageReport damageReport)
        {
            DamageInfo damageInfo = damageReport.damageInfo;
            HealthComponent victimHealthComponent = damageReport.victimBody?.healthComponent;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (victimHealthComponent == null || !victimHealthComponent.alive || attackerBody == null || damageInfo.procCoefficient == 0)
                return;

            bool isFreeze = (damageInfo.damageType & DamageType.Freeze2s) > DamageType.Generic;
            bool isPermafrost = isFreeze && damageInfo.procCoefficient == Permafrost.freezeProcCoefficient;
            if (isFreeze)
                return;


            float victimMaxHealth = victimHealthComponent.fullCombinedHealth;
            float attackEndDamage = damageInfo.damage;

            float maxHealthFractionDealt = (attackEndDamage / victimMaxHealth) * 100;
            float endFreezeChance = Permafrost.freezeChancePerPercentBase + Permafrost.freezeChancePerPercentStack * stack;

            if (Util.CheckRoll(maxHealthFractionDealt * endFreezeChance * damageInfo.procCoefficient, attackerBody.master))
            {
                DamageInfo freezeHit = new DamageInfo()
                {
                    attacker = damageInfo.attacker,
                    crit = damageInfo.crit,
                    damage = victimMaxHealth * Permafrost.freezeDamageHealthFraction,
                    damageType = DamageType.Freeze2s,
                    force = Vector3.zero,
                    position = victimHealthComponent.transform.position,
                    procChainMask = damageInfo.procChainMask,
                    procCoefficient = Permafrost.freezeProcCoefficient
                };

                victimHealthComponent.TakeDamage(freezeHit);
            }
        }
    }
}
