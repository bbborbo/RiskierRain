using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;
namespace SwanSongExtended.Items
{
    class RAP : ItemBase<RAP>
    {
        public override string ConfigName => "Reworks: Repulsion Armor Plate";
        [AutoConfig("Armor Increase Base", 5)]
        public static int rapArmorBase = 5;
        [AutoConfig("Armor Increase Stack", 5)]
        public static int rapArmorStack = 5;
        [AutoConfig("Max Health Increase Base", 15)]
        public static int rapMaxHealthBase = 15;
        [AutoConfig("Max Health Increase Stack", 15)]
        public static int rapMaxHealthStack = 15;
        #region abstract
        public override string ItemName => "Repulsion Armor Plate";

        public override string ItemLangTokenName => "RAP";

        public override string ItemPickupDesc => "Gain flat health and armor.";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility};

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/pickupmodels/PickupArmorPlate");
        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/itemicons/texArmorPlate");

        #endregion
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return IDR;  
        }

        public override void Hooks()
        {
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, RoR2Content.Items.ArmorPlate);
            GetStatCoefficients += RAPStatCoefficients;
        }

        private void RAPStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount <= 0)
            {
                return;
            }
            args.armorAdd += rapArmorBase + rapArmorStack * (itemCount--);
            args.baseHealthAdd += rapMaxHealthBase + rapMaxHealthStack * (itemCount--);
        }

        public override void Init()
        {
            base.Init();
            SwanSongPlugin.RetierItem(nameof(RoR2Content.Items.ArmorPlate));
        }
    }
}
