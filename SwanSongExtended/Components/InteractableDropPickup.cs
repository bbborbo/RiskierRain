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
        }
        public void OnInteractionBegin(Interactor activator)
        {
            if (dropTable == null || !canActivate)
                return;
            Debug.Log("AAAAAAAAAAAAAHHHHHHHHHHHHHHH");
            
            UniquePickup pickup = UniquePickup.none;
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            dropTable.GenerateWeightedSelection();
            pickup = dropTable.GeneratePickup(rng);
            PickupDropletController.CreatePickupDroplet(pickup, 
                dropletOrigin.position + (dropletOrigin.forward * 3f) + (Vector3.up * 3f), 
                dropletOrigin.forward * 3f + Vector3.up * 5f, 
                false);
            if (destroyOnUse)
            {
                canActivate = false;
                Destroy(this.gameObject);
            }
        }
    }
}
