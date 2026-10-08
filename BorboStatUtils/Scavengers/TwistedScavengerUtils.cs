using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RainrotSharedUtils
{
    public static partial class Extensions
    {
        public static void ReplacePickupsOnStart(this GivePickupsOnStart pickups, Action<GivePickupsOnStart.ItemInfo> callback)
        {
            if (callback == null)
                return;
            foreach (GivePickupsOnStart.ItemInfo itemInfos in pickups.itemInfos)
            {
                callback.Invoke(itemInfos);
            }
        }
        public static void ReplacePickupsOnStart(this GivePickupsOnStart pickups, Action<GivePickupsOnStart.ItemDefInfo> callback)
        {
            if (callback == null)
                return;
            foreach (GivePickupsOnStart.ItemDefInfo itemInfos in pickups.itemDefInfos)
            {
                callback.Invoke(itemInfos);
            }
        }
    }
    public static class TwistedScavengerUtils
    {
        public static ExpansionDef dlc1 => Addressables.LoadAssetAsync<ExpansionDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_Common.DLC1_asset).WaitForCompletion();
        public static ExpansionDef dlc2 => Addressables.LoadAssetAsync<ExpansionDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Common.DLC2_asset).WaitForCompletion();
        public static ExpansionDef dlc3 => Addressables.LoadAssetAsync<ExpansionDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3.DLC3_asset).WaitForCompletion();
        //public static ExpansionDef dlc4 = Addressables.LoadAssetAsync<ExpansionDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_Common.DLC1_asset).WaitForCompletion();

        public static GivePickupsOnStart GetPickupOnStart(CharacterMaster master)
        {
            foreach (GivePickupsOnStart pickups in master.GetComponents<GivePickupsOnStart>())
            {
                if (pickups.enabled == true)
                    return pickups;
            }
            return null;
        }

        /// <summary>
        /// please add to your own content pack
        /// </summary>
        public static CharacterMaster CreateNewScavengerMaster(string scavNameToken, out CharacterBody scavBody)
        {
            GameObject masterObject = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ScavLunar.ScavLunar1Master_prefab).WaitForCompletion()
                .InstantiateClone(scavNameToken + "Master", true);
            GameObject bodyObject = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ScavLunar.ScavLunar1Body_prefab).WaitForCompletion()
                .InstantiateClone(scavNameToken + "Body", true);

            CharacterMaster scavMaster = masterObject.GetComponent<CharacterMaster>();
            scavBody = bodyObject.GetComponent<CharacterBody>();

            scavMaster.bodyPrefab = bodyObject;
            scavBody.baseNameToken = scavNameToken.ToUpper() + "_NAME";


            foreach (GivePickupsOnStart gpos in masterObject.GetComponents<GivePickupsOnStart>())
            {
                gpos.enabled = false;
            }

            //Content.AddMasterPrefab(masterObject);
            //Content.AddCharacterBodyPrefab(bodyObject);

            return scavMaster;
        }
    }
}
