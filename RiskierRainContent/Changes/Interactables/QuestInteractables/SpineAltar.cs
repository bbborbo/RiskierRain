using BepInEx.Configuration;
using R2API;
using RiskierRainContent.Changes.Components;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace RiskierRainContent.Interactables
{
    class SpineAltar : InteractableBase<SpineAltar>
    {
        public override string InteractableName => "Malachite Spine";

        public override string InteractableContext => "Pick up";

        public override string InteractableLangToken => "SPINEINTERACTABLE";

        public override GameObject InteractableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/spineInteractable.prefab");

        public override string modelName => "mdlSpineInteractable";

        public override string prefabName => "spineInteractable";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 0;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Barrels;

        public override int spawnCost => 1;


        public override CostTypeIndex costTypeIndex => 0;

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
            //"golemplains",
            //"golemplains2",
            //"blackbeach",
            //"blackbeach2",
            //"snowyforest",
            //"foggyswamp",
            //"goolake",
            //"frozenwall",
            //"wispgraveyard",
            //"dampcavesimple",
            //"shipgraveyard",
            //"arena",
            //"skymeadow",
            //"artifactworld",
            //"rootjungle",
            //"ancientloft",
            //"sulfurpools",
			////modded stages
			//"slumberingsatellite",
            //"forgottenhaven",
            //"drybasin",
            //"FBLScene"
        };

        public override void Init(ConfigFile config)
        {
            hasAddedInteractable = false;
            //On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += SpineAltarBehavior;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        private void SpineAltarBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME")
            {
                PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(Items.MalachiteSpine.instance.ItemsDef.itemIndex);

                dropletOrigin = self.gameObject.transform;
                PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 10f);
                GameObject.Destroy(self.gameObject);
            }
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            InteractableDropPickup idp = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idp.dropTable = new WeightedSelection<PickupIndex>();
            idp.dropTable.AddChoice(PickupCatalog.FindPickupIndex(Items.MalachiteSpine.instance.ItemsDef.itemIndex), 1f);
            return idp.OnInteractionBegin;
        }

        public Transform dropletOrigin;
    }
}
