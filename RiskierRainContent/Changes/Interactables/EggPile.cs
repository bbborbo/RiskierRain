﻿using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRainContent.Items;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Changes.Components;
using UnityEngine.Events;

namespace RiskierRainContent.Interactables
{
    class EggPile : InteractableBase<EggPile>
    {
        public override string InteractableName => "Egg";

        public override string InteractableContext => "Found an egg";

        public override string InteractableLangToken => "EGG_PILE";

        public override GameObject InteractableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/eggPile.prefab");

        public override string modelName => "eggPile";

        public override string prefabName => "eggPile";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0.05f;

        public override int normalWeight => 100;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Barrels;

        public override int spawnCost => 1;

        public override CostTypeIndex costTypeIndex => 0;

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
            
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            InteractableDropPickup idi = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idi.dropTable = GenerateWeightedSelection();
            return idi.OnInteractionBegin;
        }
        private WeightedSelection<PickupIndex> GenerateWeightedSelection()
        {
            WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>();
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(Egg.instance.ItemsDef.itemIndex), 1f);
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(GoldenEgg.instance.ItemsDef.itemIndex), 0.2f);
            return weightedSelection;
        }

        WeightedSelection<PickupIndex> weightedSelection;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
    }
}
