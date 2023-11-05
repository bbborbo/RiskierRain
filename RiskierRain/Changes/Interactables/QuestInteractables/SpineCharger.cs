using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Interactables
{
    class SpineCharger : InteractableBase<SpineCharger>
    {
        public override string interactableName => "Broken Reactor";

        public override string interactableContext => "yea";

        public override string interactableLangToken => "SPINECHARGER";

        public override GameObject interactableModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/spineCharger.prefab");

        public override string modelName => "mdlSpineCharger";

        public override string prefabName => "spineCharger";

        public override bool modelIsCloned => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 0;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Shrines;

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

        public override int maxSpawnsPerStage => 2;

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
            //On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
            //On.RoR2.PurchaseInteraction.OnInteractionBegin += SpineChargerBehavior;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);

            On.RoR2.CostTypeDef.PayCost += SpineChargerPayCostHook;

        }

        private CostTypeDef.PayCostResults SpineChargerPayCostHook(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
        {
            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (purchasedObject.GetComponent<GenericDisplayNameProvider>()?.displayToken == "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME" && activatorBody != null)
            {
                cost = 0;
                Inventory activatorInventory = activatorBody.inventory;
                if (activatorInventory == null)
                {
                    return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
                }
                int flameOrbCount = activatorInventory.GetItemCount(Items.MalachiteSpine.instance.ItemsDef);
                if (flameOrbCount == 0)
                {
                    return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
                }
                activatorInventory.RemoveItem(Items.MalachiteSpine.instance.ItemsDef);
                activatorInventory.GiveItem(Items.ChargedSpine.instance.ItemsDef);
                //CharacterMasterNotificationQueue.SendTransformNotification(activatorBody.master,
                //            Items.FlameOrb.instance.ItemsDef.itemIndex, Items.LunarBrand.instance.ItemsDef.itemIndex,
                //            CharacterMasterNotificationQueue.TransformationType.Default);
            }
            return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
        }

        private void SpineChargerBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
                
            }
        }
        public Transform dropletOrigin;
    }
}
