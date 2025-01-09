using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Components
{
    public class InteractableDropPickup : MonoBehaviour
    {
        public bool destroyOnUse = true;
        public WeightedSelection<PickupIndex> dropTable;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
        public bool canActivate = true;
        void Start()
        {
            if(dropletOrigin == null)
            {
                dropletOrigin = this.transform;
            }
        }
        public void OnInteractionBegin(Interactor activator)
        {
            if (dropTable == null || !canActivate)
                return;
            PickupIndex pickupIndex = PickupIndex.none;
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            pickupIndex = PickupDropTable.GenerateDropFromWeightedSelection(rng, dropTable);
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 3f + dropletOrigin.up * 5f);
            if (destroyOnUse)
            {
                canActivate = false;
                Destroy(this.gameObject);
            }
        }
    }
}
