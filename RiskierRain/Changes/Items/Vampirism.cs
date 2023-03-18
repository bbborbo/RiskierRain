using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class Vampirism : ItemBase<Vampirism>
    {
        int maxHeal = 3;
        int bleedPerHeal = 5;


        public override string ItemName => "Yummy";

        public override string ItemLangTokenName => "VAMPIRETHING";

        public override string ItemPickupDesc => "Chance to inflict bleed on hit. Heal when hitting bleeding enemies.";

        public override string ItemFullDescription => "later";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.Damage };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += VampireHit;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            //add bleedchance later
        }

        private void VampireHit(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            int bleedCount = self.body.GetBuffCount(RoR2Content.Buffs.Bleeding);
            if (bleedCount > 0)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody != null)
                {
                    int itemCount = GetCount(attackerBody);
                    if (itemCount > 0)
                    {
                        int healAmount = Mathf.Clamp((bleedCount / bleedPerHeal) + 1, 0, maxHeal * itemCount);
                        attackerBody.healthComponent.Heal(healAmount, new ProcChainMask());
                    }
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Seed, //consumes leeching seed
                itemDef2 = Vampirism.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
