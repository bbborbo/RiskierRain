using EntityStates;
using EntityStates.Drone.DroneWeapon;
using MonoMod.Cil;
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
            ChangeHealingDrones();
        }

        private static void ChangeHealingDrones()
        {
            //healing drone
            IL.EntityStates.Drone.DroneWeapon.HealBeam.OnEnter += NerfHealingDroneScaling;
            //emergency drone
            IL.EntityStates.Drone.DroneWeapon.StartHealBeam.OnEnter += NerfHealingDroneScaling;
        }


        private static void NerfHealingDroneScaling(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdfld<BaseState>(nameof(BaseState.damageStat))
                );
            if(b == false)
            {
                RiskierRainPlugin.DebugBreakpoint(nameof(NerfHealingDroneScaling));
                return;
            }
            c.Remove();
            c.EmitDelegate<Func<BaseState, float>>((self) =>
            {
                if (self.characterBody == null || self.characterBody.teamComponent == null || self.characterBody.teamComponent.teamIndex != TeamIndex.Player)
                    return self.damageStat;
                float factor = (float)(5 + TeamManager.instance.GetTeamLevel(TeamIndex.Player)) / (float)(5 + Run.instance.ambientLevelFloor);
                return self.damageStat * factor;
            });
        }

        #region tc-280 prototype drone mega drone
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
                    body.PerformAutoCalculateLevelStats();
                }
            });
        }
        #endregion

        #region junk drone
        public static float junkDroneDropCoefficient = 1000;//1?
        public static int junkDroneDropsMax = 3;//5
        public static int junkDroneDropsPerTier = 1;//1
        private static void ChangeJunkDrone()
        {
            On.EntityStates.Drone.DroneJunk.Surprise.OnEnter += (orig, self) =>
            {
                EntityStates.Drone.DroneJunk.Surprise.itemsToDropCoefficient = junkDroneDropCoefficient;
                EntityStates.Drone.DroneJunk.Surprise.maxItemCount = junkDroneDropsMax;
                EntityStates.Drone.DroneJunk.Surprise.extraItemsPerTier = junkDroneDropsPerTier;
                orig(self);
            };
        }
        #endregion

        #region cleanup drone
        public static int cleanupValueGold = 8; //12
        public static float cleanupValueHealFraction = 0.10f;//0.15f
        public static float cleanupValueHealFlat = 0f;//0f
        private static void ChangeCleanupDrone()
        {
            On.EntityStates.Drone.Cleanup.OnEnter += (orig, self) =>
            {
                EntityStates.Drone.Cleanup.goldPackValue = cleanupValueGold;
                EntityStates.Drone.Cleanup.healthOrbFractionalHealing = cleanupValueHealFraction;
                EntityStates.Drone.Cleanup.healthOrbFlatHealing = cleanupValueHealFlat;
                orig(self);
            };
        }
        #endregion
    }
}
