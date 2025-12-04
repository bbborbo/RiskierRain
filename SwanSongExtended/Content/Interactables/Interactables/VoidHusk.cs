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

namespace SwanSongExtended.Interactables
{
    class VoidHusk : InteractableBase<VoidHusk>
    {
        #region abstract
        public override string InteractableName => "Fractured Husk";

        public override string InteractableContext => "Break open";

        public override string InteractableLangToken => "VOID_HUSK";

        public override GameObject InteractableModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlVoidHusk.prefab");

        public override string modelName => "mdlVoidHusk";

        public override string prefabName => "VoidHusk";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0.4f;

        public override int normalWeight => 5;

        public override int favoredWeight => 20;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Rare;

        public override int spawnCost => 20;


        public override CostTypeIndex costTypeIndex => CostTypeIndex.VoidCoin;

        //public override int costAmount => 1;
        //
        //public override int interactableMinimumStageCompletions => 1;
        //
        //public override bool automaticallyScaleCostWithDifficulty => false;
        //
        //public override bool setUnavailableOnTeleporterActivated => false;
        //
        //public override bool isShrine => false;
        //
        //public override bool orientToFloor => true;
        //
        //public override bool skipSpawnWhenSacrificeArtifactEnabled => true;//idk
        //
        //public override float weightScalarWhenSacrificeArtifactEnabled => 1;
        //
        //public override int maxSpawnsPerStage => 1;

        public override int interactionCost => 1;

        public override string[] validScenes => new string[]
        {
            "foggyswamp",
            "shipgraveyard",
            "ancientloft",            
			//modded stages
			"slumberingsatellite",
            "FBLScene"
        };

        public override string[] favoredScenes => new string[]
        {
            "dampcavesimple",
            "sulfurpools",
            //modded stages
            "forgottenhaven",
            "drybasin"
        };

        public override SimpleInteractableData InteractableData => new SimpleInteractableData
            (
                minimumStageCompletions: 1,
                unavailableDuringTeleporter: false,
                isShrine: false,
                orientToFloor: true,
                maxSpawnsPerStage: 1
            );
        #endregion

        public override void Init()
        {
            base.Init();
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
        private ExplicitPickupDropTable GenerateWeightedSelection()
        {
            ExplicitPickupDropTable dropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();

            List<ExplicitPickupDropTable.PickupDefEntry> pickupDefEntries = new List<ExplicitPickupDropTable.PickupDefEntry>();
            pickupDefEntries.Add(
                new ExplicitPickupDropTable.PickupDefEntry
                {
                    pickupDef = VoidIchorRed.instance.ItemsDef,
                    pickupWeight = 1f
                }
                );
            pickupDefEntries.Add(
                new ExplicitPickupDropTable.PickupDefEntry
                {
                    pickupDef = VoidIchorYellow.instance.ItemsDef,
                    pickupWeight = 1f
                }
                );
            pickupDefEntries.Add(
                 new ExplicitPickupDropTable.PickupDefEntry
                 {
                     pickupDef = VoidIchorViolet.instance.ItemsDef,
                     pickupWeight = 1f
                 }
                 );
            dropTable.pickupEntries = pickupDefEntries.ToArray();

            return dropTable;
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            InteractableDropPickup idp = interaction.gameObject.AddComponent<InteractableDropPickup>();
            idp.dropTable = GenerateWeightedSelection();
            idp.destroyOnUse = true;
            return idp.OnInteractionBegin;

        }

        public override void Hooks()
        {
            
        }

        WeightedSelection<PickupIndex> weightedSelection;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
    }
}
