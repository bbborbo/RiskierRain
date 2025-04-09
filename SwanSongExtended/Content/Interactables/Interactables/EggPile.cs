using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SwanSongExtended.Items;
using R2API;
using SwanSongExtended.Modules;
using SwanSongExtended.Components;
using UnityEngine.Events;

namespace SwanSongExtended.Interactables
{
    class EggPile : InteractableBase<EggPile>
    {
        public override string ConfigName => "Interactables : Egg";
        public override string InteractableName => "Egg";

        public override string InteractableContext => "Found an egg";

        public override string InteractableLangToken => "EGG_PILE";

        public override GameObject InteractableModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/eggPile.prefab");

        public override string modelName => "mdlEggPile";

        public override string prefabName => "eggPile";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0.05f;

        public override int normalWeight => 100;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Barrels;

        public override int spawnCost => 1;

        public override CostTypeIndex costTypeIndex => CostTypeIndex.None;

        public override int interactionCost => 0;
        public override SimpleInteractableData InteractableData => new SimpleInteractableData
            (
                unavailableDuringTeleporter: false,
                sacrificeWeightScalar: 1,
                maxSpawnsPerStage: 1
            );


        public override string[] validScenes => new string[]
        {
            "blackbeach",
            "blackbeach2",
            "wispgraveyard",
            "shipgraveyard",
            "rootjungle",
            "habitat",
            "habitatfall",
            "lakes",
            "lakesnight",
            "foggyswamp"
        };
        public override string[] favoredScenes => new string[] { };

        public override void Init()
        {
            base.Init();
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

        public override void Hooks()
        {

        }

        WeightedSelection<PickupIndex> weightedSelection;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
    }
}
