using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace SwanSongExtended.Components
{
    public class InteractableDropPickup : MonoBehaviour
    {
        public PurchaseInteraction purchaseInteraction;
        public bool destroyOnUse = true;
        public ExplicitPickupDropTable dropTable;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
        public bool canActivate = true;
        void Start()
        {
            if(dropletOrigin == null)
            {
                dropletOrigin = this.transform;
            }
            purchaseInteraction.onPurchase.AddListener(new UnityAction<Interactor>(OnInteractionBegin));
        }
        public void OnInteractionBegin(Interactor activator)
        {
            if (dropTable == null || !canActivate)
                return;
            Debug.Log("AAAAAAAAAAAAAHHHHHHHHHHHHHHH");
            
            PickupIndex pickupIndex = PickupIndex.none;
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            dropTable.GenerateWeightedSelection();
            pickupIndex = dropTable.GenerateDrop(rng);
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 3f + dropletOrigin.up * 5f);
            if (destroyOnUse)
            {
                canActivate = false;
                Destroy(this.gameObject);
            }
        }
    }
}
