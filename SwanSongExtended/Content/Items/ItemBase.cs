﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;
using RoR2.EntitlementManagement;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    public abstract class ItemBase<T> : ItemBase where T : ItemBase<T>
    {
        public static T instance { get; private set; }

        public ItemBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class ItemBase : SharedBase
    {
        public override AssetBundle assetBundle => SwanSongPlugin.orangeAssetBundle;
        public override string ConfigName => "Items : " + ItemName;


        public static Dictionary<string, ItemDef> DefDictionary = new Dictionary<string, ItemDef>();

        public abstract string ItemName { get; }
        public abstract string ItemLangTokenName { get; }
        public abstract string ItemPickupDesc { get; }
        public abstract string ItemFullDescription { get; }
        public abstract string ItemLore { get; }

        public abstract ItemTier Tier { get; }
        public abstract ItemTag[] ItemTags { get; }

        public abstract GameObject ItemModel { get; }
        public abstract Sprite ItemIcon { get; }
        public ItemDef ItemsDef;

        public virtual bool CanRemove { get; } = false;
        public virtual bool IsHidden { get; } = false;
        public virtual ExpansionDef RequiredExpansion { get; } = null;

        public override void Init()
        {
            CreateItem();
            base.Init();
        }

        internal static bool CheckDLC1Entitlement()
        {
            EntitlementDef dlc1 = Addressables.LoadAssetAsync<EntitlementDef>("RoR2/DLC1/Common/entitlementDLC1.asset").WaitForCompletion();
            return CheckDLCEntitlement(dlc1);
        }

        internal static bool CheckDLCEntitlement(EntitlementDef expansion)
        {
            //LocalUser thisUser = PlayerCharacterMasterController.instances[0].networkUser.localUser;
            //LocalUserEntitlementTracker localEntitlement = EntitlementManager.localUserEntitlementTracker;
            //if (localEntitlement.UserHasEntitlement(thisUser, expansion))
            //{
            //    return true;
            //}
            if (EntitlementAbstractions.VerifyLocalSteamUser(expansion))
            {
                return true;
            }
            return false;
        }

        internal static void CloneVanillaDisplayRules(UnityEngine.Object newDef, UnityEngine.Object vanillaDef)
        {
            Debug.LogError("Unable to clone vanilla display rules!");
            return;
            if (newDef != null)
            {
                foreach (GameObject bodyPrefab in BodyCatalog.bodyPrefabs)
                {
                    CharacterModel model = bodyPrefab.GetComponentInChildren<CharacterModel>();
                    if (model)
                    {
                        ItemDisplayRuleSet idrs = model.itemDisplayRuleSet;
                        if (idrs)
                        {
                            // clone the original item display rule

                            Array.Resize(ref idrs.keyAssetRuleGroups, idrs.keyAssetRuleGroups.Length + 1);
                            idrs.keyAssetRuleGroups[idrs.keyAssetRuleGroups.Length - 1].displayRuleGroup = idrs.FindDisplayRuleGroup(vanillaDef);
                            idrs.keyAssetRuleGroups[idrs.keyAssetRuleGroups.Length - 1].keyAsset = newDef;

                            idrs.GenerateRuntimeValues();
                        }
                    }
                }
            }
        }

        public override void Lang()
        {
            DoLangForItem(ItemsDef, ItemName, ItemPickupDesc, ItemFullDescription, ItemLore);
            //LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_NAME", ItemName);
            //LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_PICKUP", ItemPickupDesc);
            //LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_DESCRIPTION", ItemFullDescription);
            //LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_LORE", ItemLore);
        }

        public abstract ItemDisplayRuleDict CreateItemDisplayRules();

        protected void CreateItem()
        {
            string tierNameString = Tier.ToString();
            if (!tierNameString.Contains("Tier"))
                tierNameString += "Tier";

            ItemsDef = CreateNewItem(ItemLangTokenName, ItemModel, ItemIcon, Tier, ItemTags, RequiredExpansion, IsHidden);

            var itemDisplayRules = CreateItemDisplayRules();
            if (itemDisplayRules == null)
            {
                itemDisplayRules = new ItemDisplayRuleDict();
            }

            //ItemAPI.Add(new CustomItem(ItemsDef, itemDisplayRules));
        }

        public static ItemDef CreateNewItem(string langTokenName, GameObject modelPrefab, Sprite iconSprite, ItemTier tier, 
            ItemTag[] itemTags = null, ExpansionDef requiredExpansion = null, bool isHidden = false)
        {
            ItemDef itemDef = CreateNewUntieredItem(langTokenName, iconSprite, tier, itemTags, isHidden);
            itemDef.pickupModelPrefab = modelPrefab;
            itemDef.requiredExpansion = requiredExpansion;
            return itemDef;
        }

        public static ItemDef CreateNewUntieredItem(string langTokenName, Sprite iconSprite, ItemTier tier = ItemTier.NoTier, 
            ItemTag[] itemTags = null, bool isHidden = false)
        {
            ItemDef itemDef = ScriptableObject.CreateInstance<ItemDef>();

            itemDef.name = "ITEM_" + langTokenName;
            itemDef.nameToken = "ITEM_" + langTokenName + "_NAME";
            itemDef.pickupToken = "ITEM_" + langTokenName + "_PICKUP";
            itemDef.descriptionToken = "ITEM_" + langTokenName + "_DESCRIPTION";
            itemDef.loreToken = "ITEM_" + langTokenName + "_LORE";
            itemDef.pickupIconSprite = iconSprite;
            itemDef.tier = tier;
            itemDef.deprecatedTier = tier;
            itemDef.hidden = isHidden;

            if(itemTags != null && itemTags.Length > 0)
            {
                itemDef.tags = itemTags;
            }

            Content.AddItemDef(itemDef);
            return itemDef;
        }
        public static void DoLangForItem(ItemDef itemDef, string name, string pickupDesc, string fullDesc, string lore = "")
        {
            LanguageAPI.Add(itemDef.nameToken, name);
            LanguageAPI.Add(itemDef.pickupToken, pickupDesc);
            LanguageAPI.Add(itemDef.descriptionToken, fullDesc);
            LanguageAPI.Add(itemDef.loreToken, lore);
        }

        public int GetCount(CharacterBody body)
        {
            if (!body || !body.inventory) { return 0; }

            return body.inventory.GetItemCount(ItemsDef);
        }
        public int GetCount(Inventory inventory)
        {
            if (!inventory) { return 0; }

            return inventory.GetItemCount(ItemsDef);
        }

        public int GetCount(CharacterMaster master)
        {
            if (!master || !master.inventory) { return 0; }

            return master.inventory.GetItemCount(ItemsDef);
        }

        public int GetCountSpecific(CharacterBody body, ItemDef itemIndex)
        {
            if (!body || !body.inventory) { return 0; }

            return body.inventory.GetItemCount(itemIndex);
        }

        public static float GetStackValue(float baseValue, float stackValue, int itemCount)
        {
            return baseValue + stackValue * (itemCount - 1);
        }


        public static GameObject LoadDropPrefab(string prefabName = "")
        {
            GameObject prefab = null;
            if (SwanSongPlugin.mainAssetBundle && prefabName != "")
            {
                prefab = SwanSongPlugin.mainAssetBundle.LoadAsset<GameObject>($"Assets/Models/DropPrefabs/Item/{prefabName}.prefab");
            }
            
            if(prefab == null)
                prefab = Resources.Load<GameObject>("prefabs/NullModel");
            return prefab;
        }

        public static GameObject LoadDisplayPrefab(string prefabName = "")
        {
            GameObject prefab = null;
            if(SwanSongPlugin.mainAssetBundle && prefabName != "")
            {
                prefab = SwanSongPlugin.mainAssetBundle.LoadAsset<GameObject>($"Assets/Models/DisplayPrefabs/Item/{prefabName}.prefab"); ;
            }
            return prefab;
        }

        public static Sprite LoadItemIcon(string spriteName = "")
        {
            Sprite icon = null;
            if (SwanSongPlugin.mainAssetBundle && spriteName != "")
            {
                icon = SwanSongPlugin.mainAssetBundle.LoadAsset<Sprite>($"Assets/Textures/Icons/Item/{spriteName}.png");
            }

            if (icon == null)
                icon = Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
            return icon;
        }
        public static ExpansionDef SotvExpansionDef()
        {
            return Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
        }
    }
}