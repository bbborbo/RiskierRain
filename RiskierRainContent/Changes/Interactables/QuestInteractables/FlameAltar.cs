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
    class FlameAltar : InteractableBase<FlameAltar>
    {
        public override string InteractableName => "Flame Altar";

        public override string InteractableContext => "Pick up (Flame Orb)";

        public override string InteractableLangToken => "FLAMEALTAR";

        public override GameObject InteractableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/flameAltar.prefab");

        public override string modelName => "mdlFlameAltar";

        public override string prefabName => "flameAltar";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 30;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Barrels;

        public override int spawnCost => 10;


        public override CostTypeIndex costTypeIndex => 0;

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
            "drybasin"
        };

        public override void Init(ConfigFile config)
        {
            hasAddedInteractable = false;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            InteractableDropPickup idp = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idp.dropTable = new WeightedSelection<PickupIndex>();
            idp.dropTable.AddChoice(PickupCatalog.FindPickupIndex(Items.FlameOrb.instance.ItemsDef.itemIndex), 1f);
            return idp.OnInteractionBegin;
        }
        public Transform dropletOrigin;
    }
}
