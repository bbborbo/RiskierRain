using JetBrains.Annotations;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Components
{
    class CopyInventoryFromOwner : MonoBehaviour
    {
        public Func<ItemIndex, bool> inventoryItemCopyFilter = new Func<ItemIndex, bool>(Inventory.defaultItemCopyFilterDelegate);

        public bool copyInventory = true;
        public bool copyEquipment = true;

        public void Start()
        {
            CharacterMaster master = this.gameObject.GetComponent<CharacterMaster>();
            MinionOwnership minionOwnership = this.gameObject.GetComponent<MinionOwnership>();
            if(minionOwnership != null && master != null) 
            {
                Inventory ownerInventory = minionOwnership.ownerMaster?.inventory;
                if (ownerInventory)
                {
                    master.inventory.CopyEquipmentFrom(ownerInventory);
                    master.inventory.CopyItemsFrom(ownerInventory);
                }
            }
        }
    }
}
