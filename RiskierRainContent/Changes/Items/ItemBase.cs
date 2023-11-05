using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;
using RoR2.EntitlementManagement;

namespace RiskierRain.Items
{
    // The directly below is entirely from TILER2 API (by ThinkInvis) specifically the Item module. Utilized to keep instance checking functionality as I migrate off TILER2.
    // TILER2 API can be found at the following places:
    // https://github.com/ThinkInvis/RoR2-TILER2
    // https://thunderstore.io/package/ThinkInvis/TILER2/

    public abstract class ItemBase<T> : ItemBase where T : ItemBase<T>
    {
        public static T instance { get; private set; }

        public ItemBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class ItemBase
    {
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

        public abstract void Init(ConfigFile config);

        protected void CreateLang()
        {
            LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_NAME", ItemName);
            LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_PICKUP", ItemPickupDesc);
            LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_DESCRIPTION", ItemFullDescription);
            LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_LORE", ItemLore);
        }

        public abstract ItemDisplayRuleDict CreateItemDisplayRules();

        protected void CreateItem()
        {
            string tierNameString = Tier.ToString();
            if (!tierNameString.Contains("Tier"))
                tierNameString += "Tier";

            ItemsDef = ScriptableObject.CreateInstance<ItemDef>();//new RoR2.ItemDef()
            {
                ItemsDef.name = "ITEM_" + ItemLangTokenName;
                ItemsDef.nameToken = "ITEM_" + ItemLangTokenName + "_NAME";
                ItemsDef.pickupToken = "ITEM_" + ItemLangTokenName + "_PICKUP";
                ItemsDef.descriptionToken = "ITEM_" + ItemLangTokenName + "_DESCRIPTION";
                ItemsDef.loreToken = "ITEM_" + ItemLangTokenName + "_LORE";
                ItemsDef.pickupModelPrefab = ItemModel;
                ItemsDef.pickupIconSprite = ItemIcon;
                ItemsDef.tier = Tier;
                ItemsDef.deprecatedTier = Tier;
                ItemsDef.requiredExpansion = RequiredExpansion;
            }
            if (ItemTags.Length > 0) { ItemsDef.tags = ItemTags; }

            var itemDisplayRules = CreateItemDisplayRules();
            if (itemDisplayRules == null)
            {
                itemDisplayRules = new ItemDisplayRuleDict();
            }

            ItemAPI.Add(new CustomItem(ItemsDef, itemDisplayRules));
        }

        public abstract void Hooks();

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

        public static GameObject LoadDropPrefab(string prefabName)
        {
            GameObject prefab = Assets.mainAssetBundle.LoadAsset<GameObject>($"Assets/Models/DropPrefabs/Item/{prefabName}.prefab");
            return prefab;
        }

        public static GameObject LoadDisplayPrefab(string prefabName)
        {
            GameObject prefab = Assets.mainAssetBundle.LoadAsset<GameObject>($"Assets/Models/DisplayPrefabs/Item/{prefabName}.prefab");
            if(prefab == null)
            {
                prefab = LoadDisplayPrefab(prefabName);
            }
            return prefab;
        }

        public static Sprite LoadItemIcon(string spriteName)
        {
            Sprite icon = Assets.mainAssetBundle.LoadAsset<Sprite>($"Assets/Textures/Icons/Item/{spriteName}.png");
            return icon;
        }
        public static ExpansionDef SotvExpansionDef()
        {
            return Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
        }
    }
}