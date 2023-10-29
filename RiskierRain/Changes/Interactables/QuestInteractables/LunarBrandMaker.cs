using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Interactables
{
    class LunarBrandMaker : InteractableBase<LunarBrandMaker>
    {
        public override string interactableName => "???";

        public override string interactableContext => "yea";

        public override string interactableLangToken => "LUNARBRANDMAKER";

        public override GameObject interactableModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/lunarBrandMaker.prefab");

        public override string modelName => "mdlLunarBrandMaker";

        public override string prefabName => "lunarBrandMaker";

        public override bool modelIsCloned => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 0;

        public override int favoredWeight => 0;

        public override int category => 4;

        public override int spawnCost => 1;

        public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.None);

        public override int costTypeIndex => 0;

        public override int costAmount => 0;

        public override int interactableMinimumStageCompletions => 0;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => true;

        public override bool orientToFloor => false;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 20;

        public string[] validScenes = {
            "golemplains",
            "golemplains2",
            "blackbeach",
            "blackbeach2",
            "snowyforest",
            "foggyswamp",
            "goolake",
            "frozenwall",
            "wispgraveyard",
            "dampcavesimple",
            "shipgraveyard",
            "arena",
            "skymeadow",
            "artifactworld",
            "rootjungle",
            "ancientloft",
            "sulfurpools",
			//modded stages
			"slumberingsatellite",
            "forgottenhaven",
            "drybasin",
            "FBLScene"
        };

        public override void Init(ConfigFile config)
        {
            hasAddedInteractable = false;
            On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += LunarBrandMakerBehavior;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        private void LunarBrandMakerBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
                PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(Items.MalachiteSpine.instance.ItemsDef.itemIndex);

                dropletOrigin = self.gameObject.transform;
                PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 10f);
            }
        }
        public Transform dropletOrigin;
    }
}
