using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Items
{
    class VoidIchorRed : ItemBase<VoidIchorRed>
    {
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        int damageBase = 5;
        int damageStack = 5;
        public override string ItemName => "Metamorphic Ichor (Red)";

        public override string ItemLangTokenName => "ICHORRED";

        public override string ItemPickupDesc => "Retaliate with flat damage when hit. Corrupts all Replusion Armor Plates.";

        public override string ItemFullDescription => "Retaliate with flat damage when hit. Corrupts all Replusion Armor Plates.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.Damage, ItemTag.AIBlacklist };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlIchorR.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_ICHORRED.png");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += IchorTakeDamage;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }

        private void IchorTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            if (self.body == null || damageInfo.attacker == null)
            {
                return;
            }
            int itemCount = GetCount(self.body);
            if (itemCount <= 0 || damageInfo.attacker == self.body)
            {
                return;
            }
            HealthComponent attackerHC = damageInfo.attacker.GetComponent<HealthComponent>();
            if (attackerHC == null)
            {
                Debug.LogWarning("ichorredwhoopsie");
                return;
            }
            DamageInfo retaliateHit = new DamageInfo
            {
                attacker = self.body.gameObject,
                crit = false,
                damage = damageBase + (damageStack * (itemCount -1)),
                damageType = DamageType.Generic,
                damageColorIndex = DamageColorIndex.Item,
                force = Vector3.zero,
                position = damageInfo.attacker.transform.position,
                procChainMask = damageInfo.procChainMask,
                procCoefficient = 0
            };
            attackerHC.TakeDamage(retaliateHit);
        }
        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.ArmorPlate, //consumes RAP
                itemDef2 = instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            ItemDef.Pair rockPaperScissors = new ItemDef.Pair()
            {
                itemDef1 = VoidIchorYellow.instance.ItemsDef, //:3
                itemDef2 = instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(rockPaperScissors);
            orig();
        }
    }
}
