using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Changes
{
    public abstract class ReworkBase<T> : ReworkBase where T : ReworkBase<T>
    {
        public static T instance { get; private set; }

        public ReworkBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class ReworkBase : SharedBase
    {
        public EquipmentDef equipDef;
        public ItemDef itemDef;
        public override string ConfigName => "Reworks : " + this.ItemName; 
        public override AssetBundle assetBundle => SwanSongPlugin.retierAssetBundle;
        public virtual bool IsEquipment { get; } = false;
        public abstract string ItemPath { get; }
        public abstract string ItemName { get; }
        public abstract string ItemPickupDesc { get; }
        public abstract string ItemFullDesc { get; }

        public override void Init()
        {
            base.Init();

            if (IsEquipment)
                SwanSongPlugin.LoadAsync<EquipmentDef>(ItemPath, LoadItem);
            else
                SwanSongPlugin.LoadAsync<ItemDef>(ItemPath, LoadItem);
        }
        public override void Lang() { }

        private void LoadItem(UnityEngine.Object item)
        {
            if (this.IsEquipment)
                OnEquipmentLoaded(item as EquipmentDef);
            else
                OnItemLoaded(item as ItemDef);
        }

        public virtual void OnItemLoaded(ItemDef item) 
        {
            itemDef = item;

                LanguageAPI.Add(itemDef.nameToken, ItemName);
            if(ItemPickupDesc != null)
                LanguageAPI.Add(itemDef.pickupToken, ItemPickupDesc);
            if (ItemFullDesc != null)
                LanguageAPI.Add(itemDef.descriptionToken, ItemFullDesc);
        }
        public virtual void OnEquipmentLoaded(EquipmentDef item)
        {
            equipDef = item;

                LanguageAPI.Add(equipDef.nameToken, ItemName);
            if (ItemPickupDesc != null)
                LanguageAPI.Add(equipDef.pickupToken, ItemPickupDesc);
            if (ItemFullDesc != null)
                LanguageAPI.Add(equipDef.descriptionToken, ItemFullDesc);
        }
        public static void DoLangForItem(ItemDef itemDef, string name, string pickupDesc, string fullDesc)
        {
            LanguageAPI.Add(itemDef.nameToken, name);
            LanguageAPI.Add(itemDef.pickupToken, pickupDesc);
            LanguageAPI.Add(itemDef.descriptionToken, fullDesc);
        }

        public int GetCount(CharacterBody body)
        {
            if (!body || !body.inventory) { return 0; }

            return body.inventory.GetItemCountEffective(itemDef);
        }
        public int GetCount(Inventory inventory)
        {
            if (!inventory) { return 0; }

            return inventory.GetItemCountEffective(itemDef);
        }

        public int GetCount(CharacterMaster master)
        {
            if (!master || !master.inventory) { return 0; }

            return master.inventory.GetItemCountEffective(itemDef);
        }

        public int GetCountSpecific(CharacterBody body, ItemDef itemIndex)
        {
            if (!body || !body.inventory) { return 0; }

            return body.inventory.GetItemCountEffective(itemIndex);
        }

        public static float GetStackValue(float baseValue, float stackValue, int itemCount)
        {
            return baseValue + stackValue * (itemCount - 1);
        }
    }
}
