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
        float xpFraction = 0.01f;
        int xpFlat = 5;
        public override string ItemName => "Ichor (violet)";

        public override string ItemLangTokenName => "ICHORVIOLET";

        public override string ItemPickupDesc => "Gain bonus experience on kill. Corrupts all Monster Teeth.";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnKillEffect};

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/egg.prefab");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.GlobalEventManager.OnCharacterDeath += IchorXPGain;
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

            ulong percentXP = TeamManager.instance.GetTeamNextLevelExperience(xpRecipient.teamIndex) * (ulong)xpFraction;
            ulong xpToGive = percentXP + ((ulong)xpFlat * (ulong)(itemCount - 1));
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
