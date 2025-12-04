using R2API;
using RoR2;
using SwanSongExtended.Components;
using SwanSongExtended.Items;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

namespace SwanSongExtended.Interactables
{
    class WishboneCarcass : InteractableBase<WishboneCarcass>
    {
        public override string InteractableName => "Omen";

        public override string InteractableContext => "Break Omen";

        public override string InteractableLangToken => "WISHBONECARCASS";

        public override GameObject InteractableModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlEggPile.prefab");

        public override string modelName => "mdlEggPile";

        public override string prefabName => "omen";

        public override bool ShouldCloneModel => true;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Barrels;

        public override CostTypeIndex costTypeIndex => CostTypeIndex.None;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 0;

        public override int favoredWeight => 0;

        public override int spawnCost => 0;

        public override int interactionCost => 0;

        public override string[] validScenes => new string[] { };

        public override string[] favoredScenes => new string[] { };
        public override SimpleInteractableData InteractableData => new SimpleInteractableData
            (
                unavailableDuringTeleporter: false,
                sacrificeWeightScalar: 1,
                maxSpawnsPerStage: 0
            );

        private static Xoroshiro128Plus rng;
        public static void ScatterWishbones()
        {
            rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
            int eggsToHide = Run.instance.participatingPlayerCount + 2;
            Log.Warning($"Hiding {eggsToHide} wishbones");

            for (int j = 0; j < eggsToHide; j++)
            {
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(WishboneCarcass.instance.customInteractable.spawnCard, new DirectorPlacementRule
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
                        pickupDef = Wishbone.instance.ItemsDef,
                        pickupWeight = 1f
                    }
                );
            dropTable.pickupEntries = pickupDefEntries.ToArray();

            return dropTable;
        }
        public override void Init()
        {
            base.Init();
            interactablePrefab.AddComponent<WishboneCarcassComponent>();

            GameObject lockbox = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_TreasureCache.Lockbox_prefab).WaitForCompletion();
            if (lockbox)
            {
                ParticleSystem[] particleSystems = lockbox.GetComponentsInChildren<ParticleSystem>();
                foreach(ParticleSystem ps in particleSystems)
                {
                    if (ps.name != "ActiveGlow")
                        continue;

                    GameObject glow = ps.gameObject.InstantiateClone("CarcassGlow");
                    glow.transform.parent = interactablePrefab.transform;
                    glow.transform.localPosition = Vector3.zero;
                    glow.transform.localScale *= 0.22f;
                    break;
                }
            }
        }

        public override void Hooks()
        {
        }
    }
}
