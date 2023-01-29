using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static RiskierRain.JumpStatHook;
using On.RoR2.Items;
using HarmonyLib;

namespace RiskierRain.Items
{
    class BottleFart : ItemBase<BottleFart>
    {
        public override string ItemName => "Fart In A Jar";

        public override string ItemLangTokenName => "FARTBOTTLE";

        public override string ItemPickupDesc => "Gain an extra jump.";

        public override string ItemFullDescription => "Gain an extra jump.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override BalanceCategory Category => BalanceCategory.StateOfDefenseAndHealing;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            JumpStatCoefficient += FartJump;
            OnJumpEvent += FartOnJump;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }



        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = BottleCloud.instance.ItemsDef, //consumes cloud in a bottle
                itemDef2 = BottleFart.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }

        private void FartJump(CharacterBody sender, ref int jumpCount)
        {
            if (GetCount(sender) > 0)
            {
                jumpCount += 1;
            }
        }

        private void FartOnJump(CharacterMotor obj)
        {

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
