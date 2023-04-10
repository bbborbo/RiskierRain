using BepInEx;
using R2API;
using R2API.Utils;
using RiskierRain.Equipment;
using RoR2;
using RoR2.Projectile;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EntityStates.NullifierMonster;

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
                self.duration = EntityStates.VagrantMonster.ChargeMegaNova.baseDuration;
                if (self.characterBody.HasBuff(FrenziedAspect.instance.EliteBuffDef))
                {
                    self.duration = 2;
                }
            };
            On.EntityStates.VagrantNovaItem.ChargeState.OnEnter += (orig, self) =>
            {
                orig(self);
                self.duration = 3;
            };
        }
        #endregion
        #region pest
        GameObject pestPrefab;
        GameObject pestSpit;

        float pestBaseHealth = 50f; // 80
        float pestBaseDamage = 8f; // 15
        float pestBaseSpeed = 4f; //6

        float pestSpitVelocity = 70; // 100
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
        #region beetle queen
        GameObject queenSpitPrefab;
        GameObject queenAcidPrefab;

        float spitDamageCoefficient = 0.4f; //1.3f
        float acidSize = 2f; //1f
        float acidDamageCoefficient = 2.5f; //1f
        float acidDamageFrequency = 4f; //2f
        void QueenChanges()
        {
            //queenSpitPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Beetle/BeetleQueenSpit.prefab").WaitForCompletion();
            if (queenSpitPrefab)
            {

            }
            Debug.LogError(EntityStates.BeetleQueenMonster.FireSpit.damageCoefficient);
            EntityStates.BeetleQueenMonster.FireSpit.damageCoefficient = spikeDamageCoefficient;

            queenAcidPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Beetle/BeetleQueenAcid.prefab").WaitForCompletion();
            if (queenAcidPrefab)
            {
                queenAcidPrefab.transform.localScale = Vector3.one * acidSize;
                ProjectileDotZone acidDotZone = queenAcidPrefab.GetComponent<ProjectileDotZone>();
                if (acidDotZone)
                {
                    acidDotZone.damageCoefficient = acidDamageCoefficient;
                    acidDotZone.resetFrequency = acidDamageFrequency;
                }
            }
        }
        #endregion
        #region gup
        CharacterSpawnCard gupSpawnCard;
        int gupCreditCost = 200;//150

        GameObject gupPrefab;
        float gupBaseHealth = 800f; // 1000
        float gupBaseArmor = 25f; // 0
        float gupBaseDamage = 12f; // 12
        float gupBaseSpeed = 14f; //12
        float gupBaseRegen = 0f; //0.6f

        GameObject geepPrefab;
        float geepBaseHealth = 400f; // 500
        float geepBaseArmor = 25f; // 0
        float geepBaseDamage = 8f; // 6
        float geepBaseSpeed = 10f; //8
        float geepBaseRegen = 0f; //0.6f

        GameObject gipPrefab;
        float gipBaseHealth = 200f; // 250
        float gipBaseArmor = 25f; // 0
        float gipBaseDamage = 5f; // 3
        float gipBaseSpeed = 6f; //5
        float gipBaseRegen = 0f; //0.6f

        void GupChanges()
        {
            gupSpawnCard = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/Gup/cscGupBody.asset").WaitForCompletion();
            gupSpawnCard.directorCreditCost = gupCreditCost;

            gupPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Gup/GupBody.prefab").WaitForCompletion();
            if (gupPrefab)
            {
                CharacterBody body = gupPrefab.GetComponent<CharacterBody>();
                if (body)
                {
                    body.baseMaxHealth = gupBaseHealth;
                    body.levelMaxHealth = body.baseMaxHealth * 0.3f;
                    body.baseArmor = gupBaseArmor;
                    body.baseDamage = gupBaseDamage;
                    body.levelDamage = body.baseDamage * 0.2f;
                    body.baseMoveSpeed = gupBaseSpeed;
                    body.baseRegen = gupBaseRegen;
                    body.levelRegen = body.baseRegen * 0.2f;
                }
            }

            geepPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Gup/GeepBody.prefab").WaitForCompletion();
            if (geepPrefab)
            {
                CharacterBody body = geepPrefab.GetComponent<CharacterBody>();
                if (body)
                {
                    body.baseMaxHealth = geepBaseHealth;
                    body.levelMaxHealth = body.baseMaxHealth * 0.3f;
                    body.baseArmor = geepBaseArmor;
                    body.baseDamage = geepBaseDamage;
                    body.levelDamage = body.baseDamage * 0.2f;
                    body.baseMoveSpeed = geepBaseSpeed;
                    body.baseRegen = geepBaseRegen;
                    body.levelRegen = body.baseRegen * 0.2f;
                }
            }

            gipPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Gup/GipBody.prefab").WaitForCompletion();
            if (gipPrefab)
            {
                CharacterBody body = gipPrefab.GetComponent<CharacterBody>();
                if (body)
                {
                    body.baseMaxHealth = gipBaseHealth;
                    body.levelMaxHealth = body.baseMaxHealth * 0.3f;
                    body.baseArmor = gipBaseArmor;
                    body.baseDamage = gipBaseDamage;
                    body.levelDamage = body.baseDamage * 0.2f;
                    body.baseMoveSpeed = gipBaseSpeed;
                    body.baseRegen = gipBaseRegen;
                    body.levelRegen = body.baseRegen * 0.2f;
                }
            }
        }
        #endregion
        #region void reaver

        int nulliferBombCount = 10;

        void VoidReaverChanges()
        {
            On.EntityStates.NullifierMonster.FirePortalBomb.OnEnter += BuffFirePortalBomb;
        }

        private void BuffFirePortalBomb(On.EntityStates.NullifierMonster.FirePortalBomb.orig_OnEnter orig, EntityStates.NullifierMonster.FirePortalBomb self)
        {
            FirePortalBomb.portalBombCount = nulliferBombCount;
            orig(self);
        }
        #endregion
        #region void barnacle
        GameObject barnaclePrefab;

        float fuckRegen = 0;
        void BarnacleChanges()
        {
            barnaclePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidBarnacle/VoidBarnacleBody.prefab").WaitForCompletion();
            if (barnaclePrefab)
            {
                CharacterBody barnacleBody = barnaclePrefab.GetComponent<CharacterBody>();
                if (barnacleBody)
                {
                    barnacleBody.baseRegen = fuckRegen;
                    barnacleBody.levelRegen = fuckRegen;
                }
            }
        }
        #endregion

        #region xi construct related
        void MakeSpawnSlotSpawnsInheritEliteAffix()
        {
            On.RoR2.NetworkedBodySpawnSlot.OnSpawnedServer += SpawnSlotMinionsInheritEliteAffix;
        }

        private void SpawnSlotMinionsInheritEliteAffix(On.RoR2.NetworkedBodySpawnSlot.orig_OnSpawnedServer orig, NetworkedBodySpawnSlot self, GameObject ownerBodyObject, SpawnCard.SpawnResult spawnResult, Action<MasterSpawnSlotController.ISlot, SpawnCard.SpawnResult> callback)
        {
            orig(self, ownerBodyObject, spawnResult, callback);

            CharacterBody ownerBody = ownerBodyObject.GetComponent<CharacterBody>();
            if (spawnResult.success && spawnResult.spawnedInstance && ownerBody)
            {
                Inventory component = spawnResult.spawnedInstance.GetComponent<Inventory>();
                if (component)
                {
                    component.CopyEquipmentFrom(ownerBody.inventory);
                }
            }
        }

        //ai stuff?? RoR2/DLC1/MajorAndMinorConstruct/MegaConstructMaster.prefab
        GameObject xiMaster = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MajorAndMinorConstruct/MegaConstructMaster.prefab").WaitForCompletion();
        AISkillDriver[] xiAI;
        void XiAIFix()
        {
            if (xiMaster)
            {
                xiAI = xiMaster.GetComponents<AISkillDriver>();
                if (xiAI != null)
                {
                    //shield
                    xiAI[1].selectionRequiresTargetLoS = true; //false
                    xiAI[1].maxDistance = 60; //infinite
                    //followfast
                    xiAI[2].minDistance = 150; //200
                    //followstep
                    xiAI[4].minDistance = 40; //100
                    //strafestep
                    xiAI[5].minDistance = 10; //30
                    xiAI[5].maxDistance = 40; //100
                    //fleestep
                    xiAI[6].maxDistance = 10; //30
                }
            }
        }
        #endregion
    }
}