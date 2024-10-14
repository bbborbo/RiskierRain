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

namespace RiskierRainContent.Interactables.QuestInteractables
{
    class EnergyCrystalAltar : InteractableBase<EnergyCrystalAltar>
    {
        #region abstract
        public override string InteractableName => "Energy Crystal";

        public override string InteractableContext => "Pick up";

        public override string InteractableLangToken => "ENERGY_CRYSTAL_ALTAR";

        public override GameObject InteractableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/CrystalInteractable.prefab");

        public override string modelName => "energyCrystal";

        public override string prefabName => "CrystalInteractable";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 5;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Barrels;

        public override int spawnCost => 30;

        public override CostTypeIndex costTypeIndex => 0;

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
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            InteractableDropPickup idp = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idp.dropTable = new WeightedSelection<PickupIndex>();
            idp.dropTable.AddChoice(PickupCatalog.FindPickupIndex(Items.EnergyCrystal.instance.ItemsDef.itemIndex), 1f);
            return idp.OnInteractionBegin;
        }

        public Transform dropletOrigin;

    }
}
