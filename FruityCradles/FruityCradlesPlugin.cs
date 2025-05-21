using BepInEx;
using BepInEx.Configuration;
using BetterSoulCost;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace FruityCradles
{
    //[BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    //[BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    //[BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    //[BepInDependency(MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(SoulCostPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(guid, modName, version)]
    //[R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DamageAPI))]
    public class FruityCradlesPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }

        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "FruityCradles";
        public const string version = "1.0.3";
        #endregion

        #region config
        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> DoCradleSoulCost { get; set; }
        public static ConfigEntry<float> CradleSoulPayCost { get; set; }
        public static ConfigEntry<bool> DoCradlePotential { get; set; }
        #endregion

        GameObject voidPotentialPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();
        GameObject voidCradlePrefab;
        public static float cradleHealthCost = 0.2f; //50
        void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + $"\\{modName}.cfg", true);

            DoCradleSoulCost = CustomConfigFile.Bind<bool>(modName + ": Reworks", "Change Cradle To Soul Cost", true,
                "");
            DoCradlePotential = CustomConfigFile.Bind<bool>(modName + ": Reworks", "Change Cradle To Drop Potentials", true,
                "");
            CradleSoulPayCost = CustomConfigFile.Bind<float>(modName + ": Reworks", "Cradle Pay Cost", cradleHealthCost,
                "Expressed as a decimal, i.e 0.2 is 20%. Rounded to the nearest 10%");

            VoidCradleRework();
        }
        void VoidCradleRework()
        {
            voidCradlePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidChest/VoidChest.prefab").WaitForCompletion();
            if (voidCradlePrefab)
            {
                PurchaseInteraction cradleInteraction = voidCradlePrefab.GetComponent<PurchaseInteraction>();
                if (cradleInteraction)
                {
                    cradleInteraction.cost = (int)(Mathf.RoundToInt(cradleHealthCost * 10) * 10);
                    if (DoCradleSoulCost.Value)
                    {
                        cradleInteraction.costType = CostTypeIndex.SoulCost;
                        cradleInteraction.setUnavailableOnTeleporterActivated = true;
                    }
                }

                if (DoCradlePotential.Value)
                {
                    ChestBehavior chestBehavior = voidCradlePrefab.GetComponent<ChestBehavior>();
                    FruityCradleBehavior optionChestBehavior = voidCradlePrefab.AddComponent<FruityCradleBehavior>();
                    On.RoR2.OptionChestBehavior.Roll += DoOptionCradleBehavior;

                    if (chestBehavior)
                    {
                        optionChestBehavior.purchaseInteraction = cradleInteraction;
                        optionChestBehavior.dropTable = chestBehavior.dropTable;
                        optionChestBehavior.displayTier = ItemTier.VoidTier1;
                        optionChestBehavior.dropUpVelocityStrength = 20;
                        optionChestBehavior.dropForwardVelocityStrength = 0;
                        optionChestBehavior.openState = chestBehavior.openState;
                        optionChestBehavior.pickupPrefab = voidPotentialPrefab;
                        optionChestBehavior.numOptions = 1;
                        UnityEngine.Object.Destroy(chestBehavior);
                    }
                }
                //voidCradlePrefab.AddComponent<InteractableCurseController>();
            }
        }

        private void DoOptionCradleBehavior(On.RoR2.OptionChestBehavior.orig_Roll orig, OptionChestBehavior self)
        {
            if (self is FruityCradleBehavior)
            {
                if (!NetworkServer.active)
                {
                    Debug.LogWarning("[Server] function 'System.Void RoR2.OptionChestBehavior::Roll()' called on client");
                    return;
                }

                List<PickupIndex> drops = new List<PickupIndex>();

                PickupIndex initialDrop = self.dropTable.GenerateDrop(self.rng);
                drops.Add(initialDrop);

                ItemDef.Pair[] voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
                foreach (ItemDef.Pair pair in voidPairs)
                {
                    PickupIndex voidPair = PickupCatalog.FindPickupIndex(pair.itemDef2.itemIndex);
                    if (voidPair != initialDrop)
                        continue;
                    if (pair.itemDef2.tier == ItemTier.VoidTier3)
                        break;
                    //if (pair.itemDef2 == DLC1Content.Items.TreasureCacheVoid)
                    //    break;
                    PickupIndex other = PickupCatalog.FindPickupIndex(pair.itemDef1.itemIndex);
                    drops.Add(other);
                }

                self.generatedDrops = drops.ToArray();
                return;
            }
            orig(self);
        }
    }
}
