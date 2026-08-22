using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Changes
{
    public static partial class AllyChanges
    {
        public static void Initialize()
        {
            ChangeCleanupDrone();
            ChangeJunkDrone();
            ChangeMegaDrone();
        }

        public static float megaDroneBaseMaxHealth = 450f;//1200
        public static float megaDroneBaseDamage = 10f;//14
        private static void ChangeMegaDrone()
        {
            RiskierRainPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Drones.MegaDroneBody_prefab, (prefab) =>
            {
                if(prefab.TryGetComponent(out CharacterBody body))
                {
                    body.baseMaxHealth = megaDroneBaseMaxHealth;
                    body.levelMaxHealth = body.baseMaxHealth * 0.3f;
                    body.baseDamage = megaDroneBaseDamage;
                    body.levelDamage = body.baseDamage * 0.2f;
                }
            });
        }

        private static void ChangeJunkDrone()
        {
            EntityStates.Drone.DroneJunk.Surprise.itemsToDropCoefficient = 1000;
            EntityStates.Drone.DroneJunk.Surprise.maxItemCount = 3;
            EntityStates.Drone.DroneJunk.Surprise.extraItemsPerTier = 2;
        }

        private static void ChangeCleanupDrone()
        {
            EntityStates.Drone.Cleanup.goldPackValue = 8;
            EntityStates.Drone.Cleanup.healthOrbFractionalHealing = 0.10f;
            EntityStates.Drone.Cleanup.healthOrbFlatHealing = 0;
        }
    }
}
