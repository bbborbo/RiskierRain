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
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Interactables
{
    class FlowerBloom : InteractableBase<FlowerBloom>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return ManaFlower.GetBloomConfig();
        }

        public static bool broadcastDoubleBloom = true;
        public static float bloomChance = 15;
        public static float bloomDoubleChance = 30;
        public static int bloomCtMin = 2;
        public static int bloomCtMax = 4;
        public static int bloomCtDouble = 3;
        public static int bloomCtPerAdditionalPlayer = 2;

        public override string InteractableName => "Oh...?";

        public override string InteractableContext => "It's pretty, oh so pretty...";

        public override string InteractableLangToken => "FLOWERBLOOM";

        public override GameObject InteractableModel => LoadInteractableModel(modelName);

        public override string modelName => "mdlFlowerBloom";

        public override string prefabName => "flowerBloom";

        public override bool ShouldCloneModel => true;

        public override float voidSeedWeight => 0.00f;

        public override int normalWeight => 0;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Barrels;

        public override int spawnCost => 1;

        public override CostTypeIndex costTypeIndex => CostTypeIndex.None;

        public override int interactionCost => 0;
        public override SimpleInteractableData InteractableData => new SimpleInteractableData
            (
                unavailableDuringTeleporter: false,
                sacrificeWeightScalar: 1,
                maxSpawnsPerStage: 3
            );

        public override string[] validScenes => new string[]
        {
            //"blackbeach",
            //"blackbeach2",
            //"wispgraveyard",
            //"shipgraveyard",
            //"rootjungle",
            //"habitat",
            //"habitatfall",
            //"lakes",
            //"lakesnight",
            //"foggyswamp"
        };
        public override string[] favoredScenes => new string[] { };

        private Xoroshiro128Plus rng;

        public override void Init()
        {
            base.Init();
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
            if (Run.instance.stageClearCount <= 0)
                return;

            this.rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
            if (this.rng.RangeInt(0, 100) >= bloomChance)
                return;
            Log.Debug("Blooming!");
            
            int eggsToHide = this.rng.RangeInt(bloomCtMin, bloomCtMax+1) - bloomCtPerAdditionalPlayer;
            foreach (CharacterMaster master in CharacterMaster.readOnlyInstancesList)
            {
                eggsToHide += bloomCtPerAdditionalPlayer;
            }

            if (this.rng.RangeInt(0, 100) < bloomDoubleChance)
            {
                eggsToHide += bloomCtDouble;
                if(broadcastDoubleBloom)
                    Chat.ServerAttemptBroadcastChat(UtilityColor("The air smells particularly floral...!"));
            }

            for (int j = 0; j < eggsToHide; j++)
            {
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(FlowerBloom.instance.customInteractable.spawnCard, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random
                }, rng));
            }
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            InteractableDropPickup idi = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idi.dropTable = GenerateWeightedSelection();
            idi.destroyOnUse = true;
            return idi.OnInteractionBegin;
        }
        private ExplicitPickupDropTable GenerateWeightedSelection()
        {
            ExplicitPickupDropTable dropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();

            List<ExplicitPickupDropTable.PickupDefEntry> pickupDefEntries = new List<ExplicitPickupDropTable.PickupDefEntry>();
            pickupDefEntries.Add(
                new ExplicitPickupDropTable.PickupDefEntry
                    {
                        pickupDef = ManaFlower.instance.ItemsDef,
                        pickupWeight = 1f
                    }
                );
            dropTable.pickupEntries = pickupDefEntries.ToArray();

            return dropTable;
        }
    }
}
