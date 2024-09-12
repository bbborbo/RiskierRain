using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRainContent.Items;
using R2API;
using RiskierRainContent.CoreModules;

namespace RiskierRainContent.Interactables
{
    class EggPile : InteractableBase<EggPile>
    {
        public override string interactableName => "Egg";

        public override string interactableContext => "Found an egg";

        public override string interactableLangToken => "EGG_PILE";

        public override GameObject interactableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/eggPile.prefab");

        public override string modelName => "eggPile";

        public override string prefabName => "eggPile";

        public override bool modelIsCloned => false;

        public override float voidSeedWeight => 0.05f;

        public override int normalWeight => 100;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Barrels;

        public override int spawnCost => 1;

        public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.None);

        public override int costTypeIndex => 0;

        public override int costAmount => 0;

        public override int interactableMinimumStageCompletions => 0;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => false;

        public override bool orientToFloor => true;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 5;

        public string[] validScenes =
        {
            "blackbeach",
            "blackbeach2",
            "wispgraveyard",
            "shipgraveyard",
            "rootjungle",
            "foggyswamp"
        };

        public override void Init(ConfigFile config)
        {
            hasAddedInteractable = false;
            On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += EggPileBehavior;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;            
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        private void EggPileBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
                EggReward(self.gameObject);
                GameObject.Destroy(self.gameObject);
            }
        }

        public void EggReward(GameObject interactableObject)
        {
            PickupIndex pickupIndex = PickupIndex.none;
            GenerateWeightedSelection();
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            pickupIndex = PickupDropTable.GenerateDropFromWeightedSelection(rng, weightedSelection);
            dropletOrigin = interactableObject.transform;
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 3f + dropletOrigin.up * 5f);
        }
        private void GenerateWeightedSelection()
        {
            weightedSelection = new WeightedSelection<PickupIndex>();
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(Egg.instance.ItemsDef.itemIndex), 1f);
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(GoldenEgg.instance.ItemsDef.itemIndex), 0.2f);
        }
        WeightedSelection<PickupIndex> weightedSelection;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
    }
}
