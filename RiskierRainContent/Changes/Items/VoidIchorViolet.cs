using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Items
{
    class VoidIchorViolet : ItemBase<VoidIchorViolet>
    {
        int xpDivisor = 10;
        int xpFlat = 1;
        public override string ItemName => "Metamorphic Ichor (violet)";

        public override string ItemLangTokenName => "ICHORVIOLET";

        public override string ItemPickupDesc => "Gain bonus experience immediately and when killing enemies. Corrupts all Monster Teeth.";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnKillEffect};

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlIchorV.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_ICHORVIOLET.png");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.GlobalEventManager.OnCharacterDeath += IchorXPGain;
            On.RoR2.CharacterBody.OnInventoryChanged += IchorPickup;
        }

        private void IchorPickup(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            int itemCount = GetCount(self);
            if (itemCount <= 0) return;
            CharacterMaster xpRecipient = self.master;
            ulong percentXP = TeamManager.instance.GetTeamNextLevelExperience(xpRecipient.teamIndex);// * (ulong)xpFraction;
            percentXP /= (ulong)xpDivisor;
            ulong xpToGive = (ulong)Mathf.Max(percentXP, 1) * (ulong)(itemCount - 1);
            xpRecipient.GiveExperience(xpToGive);
            Debug.Log($"gave {xpToGive} xp!!; {percentXP}");
        }

        private void IchorXPGain(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);
            if (damageReport.attackerBody == null || damageReport.attackerMaster == null)
            {
                return;
            }
            int itemCount = GetCount(damageReport.attackerBody);
            if (itemCount <= 0)
            {
                return;
            }
            CharacterMaster xpRecipient = damageReport.attackerMaster;

            ulong percentXP = TeamManager.instance.GetTeamNextLevelExperience(xpRecipient.teamIndex);// * (ulong)xpFraction;
            percentXP /= (ulong)xpDivisor;
            ulong xpToGive = (ulong)xpFlat * TeamManager.instance.GetTeamLevel(xpRecipient.teamIndex) * (ulong)(itemCount - 1);
            xpRecipient.GiveExperience(xpToGive);
            Debug.Log($"gave {xpToGive} xp!!; {percentXP}");
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
                itemDef1 = RoR2Content.Items.Tooth, //consumes monster tooth
                itemDef2 = instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            ItemDef.Pair rockPaperScissors = new ItemDef.Pair()
            {
                itemDef1 = VoidIchorRed.instance.ItemsDef, //:3
                itemDef2 = instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(rockPaperScissors);
            orig();
        }
    }
}
