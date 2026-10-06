using BepInEx.Configuration;
using R2API;
using RainrotSharedUtils;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SwanSongExtended.Scavengers
{
    public static class TwistedScavengersCore
    {
        public static ConfigEntry<bool> enableScavs { get; private set; }
        public static bool ChangeVanillaScavs = true;
        public static void Init()
        {
            enableScavs = Modules.Config.SectionEnableConfig("Twisted Scavengers");
            if (enableScavs.Value == false)
                return;
            CustomScavengers.UseCustomScavengers = true;
            if (ChangeVanillaScavs == true)
            {
                CustomScavengers.RepopulateVanillaScavengers = false;

                //twisted kipkip the gentle
                PopulateScavenger(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ScavLunar.ScavLunar1Master_prefab, TwistedScavengerUtils.dlc2, weight: 2f, callback: (master, body) =>
                {
                    GivePickupsOnStart pickups = TwistedScavengerUtils.GetPickupOnStart(master);
                    pickups.equipmentString = "EliteBeadEquipment";
                    pickups.ReplacePickupsOnStart((info) =>
                    {
                        if (info.itemString == nameof(RoR2Content.Items.RepeatHeal))
                        {
                            info.itemString = nameof(RoR2Content.Items.RandomDamageZone);
                        }
                        //if (info.itemString == nameof(RoR2Content.Items.Infusion))
                        //{
                        //    info.itemString = nameof(DLC2Content.Items.TeleportOnLowHealth);
                        //}
                    });
                    pickups.itemInfos.Append(new GivePickupsOnStart.ItemInfo() { itemString = nameof(DLC2Content.Items.AttackSpeedPerNearbyAllyOrEnemy), count = 3});
                });
                //wipwip the wild (explosions and glass)
                PopulateScavenger(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ScavLunar.ScavLunar2Master_prefab, null, weight: 0.5f);
                //twiptwip the devotee (meteorite and trans)
                PopulateScavenger(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ScavLunar.ScavLunar3Master_prefab, null);
                //guragura the lucky (tonic, visions, clover)
                PopulateScavenger(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ScavLunar.ScavLunar4Master_prefab, null, callback: (master, body) =>
                {
                    GivePickupsOnStart pickups = TwistedScavengerUtils.GetPickupOnStart(master);
                    pickups.itemInfos.Append(new GivePickupsOnStart.ItemInfo() { itemString = nameof(RoR2Content.Items.FireballsOnHit), count = 1 });
                });
            }
        }

        private static void PopulateScavenger(string guid, ExpansionDef requiredExpansion, float weight = 1, Action<CharacterMaster, CharacterBody> callback = null)
        {
            SwanSongPlugin.LoadAsync<GameObject>(guid, Idk);
            void Idk(GameObject masterObject)
            {
                if(callback != null)
                {
                    if (masterObject.TryGetComponent(out CharacterMaster master))
                    {
                        if (master.bodyPrefab.TryGetComponent(out CharacterBody body))
                        {
                            callback.Invoke(master, body);
                            CustomScavengers.AddScavengerMaster(masterObject, 1, requiredExpansion);
                            return;
                        }
                    }
                }

                CustomScavengers.AddScavengerMaster(masterObject, weight, null);
            }
        }


        private static void FinalizeSpawnCard()
        {
            throw new NotImplementedException();
        }
    }
}
