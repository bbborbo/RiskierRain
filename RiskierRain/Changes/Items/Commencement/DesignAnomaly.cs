using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.Items
{
    class DesignAnomaly : ItemBase<DesignAnomaly>
    {
        public static int bonusArmor = 20;
        public static float backstabDamageReduction = 0.4f;
        public override string ItemName => "Relic of Design";

        public override string ItemLangTokenName => "DESIGNANOMALY";

        public override string ItemPickupDesc => "Reduce damage taken from behind.";

        public override string ItemFullDescription => $"Reduce all damage taken from enemies behind you " +
            $"by <style=cIsHealth>-{Tools.ConvertDecimal(backstabDamageReduction)}</style> " +
            $"<style=cStack>(-{Tools.ConvertDecimal(backstabDamageReduction)} per stack)</style>. " +
            $"Gain <style=cIsHealing>{bonusArmor} passive armor</style> " +
            $"<style=cStack>(+{bonusArmor} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Boss;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.WorldUnique, ItemTag.CannotSteal };

        public override BalanceCategory Category => BalanceCategory.StateOfCommencement;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += BackstabDamageReduction;
            GetStatCoefficients += ArmorBoost;
        }

        private void ArmorBoost(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            args.armorAdd += itemCount * bonusArmor;
        }

        private void BackstabDamageReduction(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if(damageInfo.damage > 0 && damageInfo.attacker)
            {
                CharacterBody victimBody = self.body;
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (victimBody && attackerBody) 
                {
                    int itemCount = GetCount(self.body);
                    if (attackerBody)
                    {
                        Vector3 vector = attackerBody.corePosition - damageInfo.position;
                        if (itemCount > 0 && (damageInfo.procChainMask.HasProc(ProcType.Backstab) || BackstabManager.IsBackstab(-vector, victimBody)))
                        {
                            float dmr = Mathf.Pow(1 - backstabDamageReduction, itemCount);
                            damageInfo.damage *= dmr;

                            Debug.Log($"Design Anomaly reduced damage by {Tools.ConvertDecimal(dmr)}!");
                        }
                    }
                }
            }
            orig(self, damageInfo);
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            //CreateBuff();
            Hooks();
        }
    }
}
