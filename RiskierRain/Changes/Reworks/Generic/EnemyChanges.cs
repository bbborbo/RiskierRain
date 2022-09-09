using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain
{
    partial class RiskierRainPlugin : BaseUnityPlugin
    {
        #region vagrant
        float genesisLoopBlastDamageCoefficient = 30; //60
        float vagrantBaseHealth = 1600; //2100
        GameObject vagrantPrefab;
        void VagrantChanges()
        {
            vagrantPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Vagrant/VagrantBody.prefab").WaitForCompletion();
            if (vagrantPrefab)
            {
                CharacterBody vagrantBody = vagrantPrefab.GetComponent<CharacterBody>();
                if (vagrantBody) 
                {
                    vagrantBody.baseMaxHealth = vagrantBaseHealth;
                    vagrantBody.levelMaxHealth = vagrantBaseHealth * 0.3f;
                }
            }
        }

        private void FixJellyNuke()
        {
            EntityStates.VagrantNovaItem.DetonateState.blastProcCoefficient = 0.3f;
            EntityStates.VagrantNovaItem.DetonateState.blastDamageCoefficient = genesisLoopBlastDamageCoefficient;
            LanguageAPI.Add("ITEM_NOVAONLOWHEALTH_DESC",
                $"Falling below <style=cIsHealth>25% health</style> causes you to explode, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(genesisLoopBlastDamageCoefficient)} base damage</style>. " +
                $"Recharges every <style=cIsUtility>30 / (2 <style=cStack>+1 per stack</style>) seconds</style>.");

            On.EntityStates.VagrantMonster.ChargeMegaNova.OnEnter += (orig, self) =>
            {
                orig(self);
                self./*private*/duration = EntityStates.VagrantMonster.ChargeMegaNova.baseDuration;
            };
            On.EntityStates.VagrantNovaItem.ChargeState.OnEnter += (orig, self) =>
            {
                orig(self);
                self./*private*/duration = 3;
            };
        }
        #endregion
        #region pest

        GameObject pestPrefab;
        GameObject pestSpit;

        float pestBaseHealth = 50f; // 80
        float pestBaseDamage = 8f; // 15
        float pestBaseSpeed = 4f; //6

        float pestSpitVelocity = 45; // 100
        void PestChanges()
        {
            pestPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/FlyingVermin/FlyingVerminBody.prefab").WaitForCompletion();
            if (pestPrefab)
            {
                CharacterBody pestBody = pestPrefab.GetComponent<CharacterBody>();
                if (pestBody) 
                {
                    pestBody.baseMaxHealth = pestBaseHealth;
                    pestBody.levelMaxHealth = pestBaseHealth * 0.3f;
                    pestBody.baseDamage = pestBaseDamage;
                    pestBody.levelDamage = pestBaseDamage * 0.2f;
                    pestBody.baseMoveSpeed = pestBaseSpeed;
                }
            }

            pestSpit = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/FlyingVermin/VerminSpitProjectile.prefab").WaitForCompletion();
            if (pestSpit)
            {
                ProjectileSimple pestSpitController = pestSpit.GetComponent<ProjectileSimple>();
                if (pestSpitController)
                {
                    pestSpitController.desiredForwardSpeed = pestSpitVelocity;
                }
            }
        }
        #endregion
    }
}
