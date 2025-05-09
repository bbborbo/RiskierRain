﻿using BepInEx.Configuration;
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
using UnityEngine.Networking;

namespace SwanSongExtended.Interactables
{
    class EggPile : InteractableBase<EggPile>
    {

        [AutoConfig("Hideable Eggs Per Player", 3)]
        public static int eggsPerPlayer = 3;
        public override string InteractableName => "Egg";

        public override string InteractableContext => "Found an egg";

        public override string InteractableLangToken => "EGG_PILE";

        public override GameObject InteractableModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlEggPile.prefab");

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

        private Xoroshiro128Plus rng;

        public override void Init()
        {
            base.Init();
            InteractableDropPickup idi = InteractionComponent.gameObject.AddComponent<InteractableDropPickup>();
            idi.dropTable = GenerateWeightedSelection();
            idi.destroyOnUse = true;
            idi.purchaseInteraction = InteractionComponent;
            //InteractionComponent.onPurchase.AddListener(idi.OnInteractionBegin);
            //return new UnityAction<Interactor>(idi.OnInteractionBegin);
            //return idi.OnInteractionBegin;
        }

        public override void Hooks()
        {
            On.RoR2.SceneDirector.PopulateScene += HideEggs;
        }

        private void HideEggs(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            orig(self);

            this.rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
            int eggsToHide = 0;
            using (IEnumerator<CharacterMaster> enumerator = CharacterMaster.readOnlyInstancesList.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.inventory.GetItemCount(Egg.instance.ItemsDef) > 0)
                    {
                        eggsToHide += eggsPerPlayer;
                    }
                }
            }
            for (int j = 0; j < eggsToHide; j++)
            {
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(EggPile.instance.customInteractable.spawnCard, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random
                }, rng));
            }
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            return null;
            InteractableDropPickup idi = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idi.dropTable = GenerateWeightedSelection();
            idi.destroyOnUse = true;
            return new UnityAction<Interactor>(idi.OnInteractionBegin);
            return idi.OnInteractionBegin;
        }
        private ExplicitPickupDropTable GenerateWeightedSelection()
        {
            ExplicitPickupDropTable dropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();

            List<ExplicitPickupDropTable.PickupDefEntry> pickupDefEntries = new List<ExplicitPickupDropTable.PickupDefEntry>();
            pickupDefEntries.Add(
                new ExplicitPickupDropTable.PickupDefEntry
                    {
                        pickupDef = Egg.instance.ItemsDef,
                        pickupWeight = 1f
                    }
                );
            pickupDefEntries.Add(
                new ExplicitPickupDropTable.PickupDefEntry
                    {
                        pickupDef = GoldenEgg.instance.ItemsDef,
                        pickupWeight = 0.2f
                    }
                );
            dropTable.pickupEntries = pickupDefEntries.ToArray();

            return dropTable;
        }
    }
}
