using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Interactables.QuestInteractables
{
    class EnergyCrystalAltar : InteractableBase<EnergyCrystalAltar>
    {
        #region abstract
        public override string interactableName => "Energy Crystal";

        public override string interactableContext => "Pick up";

        public override string interactableLangToken => "ENERGY_CRYSTAL_ALTAR";

        public override GameObject interactableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/CrystalInteractable.prefab");

        public override string modelName => "energyCrystal";

        public override string prefabName => "CrystalInteractable";

        public override bool modelIsCloned => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 5;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Chests;

        public override int spawnCost => 30;

        public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.None);

        public override int costTypeIndex => 0;

        public override int costAmount => 0;

        public override int interactableMinimumStageCompletions => 1;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => false;

        public override bool orientToFloor => true;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 3;
        #endregion
        public string[] validScenes = {           
            "foggyswamp",            
            "dampcavesimple",            
            "sulfurpools",
			//modded stages
            "drybasin",
            "FBLScene"
        };
        public override void Init(ConfigFile config)
        {
            hasAddedInteractable = false;
            //On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += CrystalAltarBehavior;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        private void CrystalAltarBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
                PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(Items.EnergyCrystal.instance.ItemsDef.itemIndex);

                dropletOrigin = self.gameObject.transform;
                PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 10f);
                GameObject.Destroy(self.gameObject);
            }
        }
        public Transform dropletOrigin;

    }
}
