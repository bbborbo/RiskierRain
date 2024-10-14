using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRainContent.Items;
using R2API;
using RiskierRainContent.CoreModules;
using UnityEngine.Events;
using RiskierRainContent.Changes.Components;

namespace RiskierRainContent.Interactables
{
    class CurseStatue : InteractableBase<CurseStatue>
    {
        public override string InteractableName => "Curse Statue";

        public override string InteractableContext => "Curse yourself";

        public override string InteractableLangToken => "CURSE_STATUE";

        public override GameObject InteractableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/curseStatue.prefab");
        public override string modelName => "mdlCurseStatue";

        public override string prefabName => "curseStatue";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 100;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Shrines;

        public override int spawnCost => 0;

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
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            InteractableDropPickup idi = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idi.dropTable = new WeightedSelection<PickupIndex>();
            idi.dropTable.AddChoice(PickupCatalog.FindPickupIndex(Items.Helpers.EnemyHealthUp.instance.ItemsDef.itemIndex), 1f);
            idi.destroyOnUse = false;
            return idi.OnInteractionBegin;
        }
        private WeightedSelection<PickupIndex> GenerateWeightedSelection()
        {
            WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>();
            return weightedSelection;
        }

        public Transform dropletOrigin;
    }
}
