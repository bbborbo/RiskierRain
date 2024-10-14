using BepInEx.Configuration;
using R2API;
using RiskierRainContent.Changes.Components;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace RiskierRainContent.Interactables
{
    class VoidHusk : InteractableBase<VoidHusk>
    {
        #region abstract
        public override string InteractableName => "Fractured Husk";

        public override string InteractableContext => "Break open";

        public override string InteractableLangToken => "VOID_HUSK";

        public override GameObject InteractableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/VoidHusk.prefab");

        public override string modelName => "mdlVoidHusk";

        public override string prefabName => "VoidHusk";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0.4f;

        public override int normalWeight => 5;

        public override int favoredWeight => 20;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Rare;

        public override int spawnCost => 20;


        public override CostTypeIndex costTypeIndex => CostTypeIndex.VoidCoin;

        public override int costAmount => 1;

        public override int interactableMinimumStageCompletions => 1;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => false;

        public override bool orientToFloor => true;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => true;//idk

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 1;
        #endregion
        public string[] validScenes = {
            "foggyswamp",            
            "shipgraveyard",
            "ancientloft",            
			//modded stages
			"slumberingsatellite",            
            "FBLScene"
        };
        public string[] favoredStages =
        {
            "dampcavesimple",
            "sulfurpools",
            //modded stages
            "forgottenhaven",
            "drybasin"
        };
        public override void Init(ConfigFile config)
        {

            hasAddedInteractable = false;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            var favored = CreateInteractableSpawnCard(true);
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes, favored.interactableSpawnCard, favored.directorCard, favoredStages);
        }

        private void VoidHuskBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME")
            {
                HuskReward(self.gameObject);
                GameObject.Destroy(self.gameObject);
            }
        }

        private void HuskReward(GameObject gameObject)
        {
            PickupIndex pickupIndex = PickupIndex.none;
            GenerateWeightedSelection();
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            pickupIndex = PickupDropTable.GenerateDropFromWeightedSelection(rng, weightedSelection);
            dropletOrigin = gameObject.transform;
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 3f + dropletOrigin.up * 5f);
        }
        private WeightedSelection<PickupIndex> GenerateWeightedSelection()
        {
            WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>();
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(VoidIchorRed.instance.ItemsDef.itemIndex), 1f);
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(VoidIchorViolet.instance.ItemsDef.itemIndex), 1f);
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(VoidIchorYellow.instance.ItemsDef.itemIndex), 1f);
            return weightedSelection;
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            InteractableDropPickup idp = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idp.dropTable = GenerateWeightedSelection();
            return idp.OnInteractionBegin;
        }

        WeightedSelection<PickupIndex> weightedSelection;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
    }
}
