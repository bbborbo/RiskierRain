using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EntityStates.NullifierMonster;
using RoR2.Skills;
using EntityStates.BeetleQueenMonster;
using RiskierRain.Components;
using SwanSongExtended.Modules;
using RoR2.ContentManagement;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;

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
            Tools.LoadCharacterBodyAsync(RoR2_Base_Vagrant.VagrantBody_prefab, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseMaxHealth = vagrantBaseHealth;
                body.levelMaxHealth = vagrantBaseHealth * 0.3f;
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
                if (self.characterBody.attackSpeed > 1.5f)
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
        float pestBaseDamage = 6f; // 15
        float pestBaseSpeed = 4f; //6

        float pestSpitVelocity = 70; // 100
        void PestChanges()
        {
            Tools.LoadCharacterBodyAsync(RoR2_DLC1_FlyingVermin.FlyingVerminBody_prefab, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseMaxHealth = pestBaseHealth;
                body.levelMaxHealth = pestBaseHealth * 0.3f;
                body.baseDamage = pestBaseDamage;
                body.levelDamage = pestBaseDamage * 0.2f;
                body.baseMoveSpeed = pestBaseSpeed;
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
            EntityStates.BeetleQueenMonster.FireSpit.damageCoefficient = spitDamageCoefficient;

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

            SummonEggs.maxSummonCount = 2;
        }
        #endregion
        #region gup
        CharacterSpawnCard gupSpawnCard;
        int gupCreditCost = 200;//150

        GameObject gupPrefab;
        float gupBaseHealth = 1000f; // 1000
        float gupBaseArmor = 0f; // 0
        float gupBaseDamage = 12f; // 12
        float gupBaseSpeed = 14f; //12
        float gupBaseRegen = 0f; //0.6f

        GameObject geepPrefab;
        float geepBaseHealth = 500f; // 500
        float geepBaseArmor = 0f; // 0
        float geepBaseDamage = 8f; // 6
        float geepBaseSpeed = 10f; //8
        float geepBaseRegen = 0f; //0.6f

        GameObject gipPrefab;
        float gipBaseHealth = 250f; // 250
        float gipBaseArmor = 0f; // 0
        float gipBaseDamage = 5f; // 3
        float gipBaseSpeed = 6f; //5
        float gipBaseRegen = 0f; //0.6f

        void GupChanges()
        {
            On.EntityStates.Gup.BaseSplitDeath.OnEnter += (orig, self) =>
            {
                self.moneyMultiplier = 0;
                orig(self);
            };

            gupSpawnCard = CoreModules.SpawnCards.Gup;
            gupSpawnCard.directorCreditCost = gupCreditCost;

            Tools.LoadCharacterBodyAsync(RoR2_DLC1_Gup.GupBody_prefab, GupStats);
            void GupStats(CharacterBody body)
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

            Tools.LoadCharacterBodyAsync(RoR2_DLC1_Gup.GeepBody_prefab, GeepStats);
            void GeepStats(CharacterBody body)
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

            Tools.LoadCharacterBodyAsync(RoR2_DLC1_Gup.GipBody_prefab, GipStats);
            void GipStats(CharacterBody body)
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

        float fuckBarnacleRegen = 0;
        void BarnacleChanges()
        {
            Tools.LoadCharacterBodyAsync(RoR2_DLC1_VoidBarnacle.VoidBarnacleBody_png, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseRegen = fuckBarnacleRegen;
                body.levelRegen = fuckBarnacleRegen * 0.2f;
            }
        }
        #endregion
        #region wisp
        GameObject lesserWispPrefab;
        float wispBaseDamage = 1.5f; //3.5f

        void LesserWispCHanges()
        {
            Tools.LoadCharacterBodyAsync(RoR2_Base_Wisp.WispBody_prefab, BodyStats);
            void BodyStats(CharacterBody lesserWispBody)
            {
                lesserWispBody.baseDamage = wispBaseDamage;
                lesserWispBody.levelDamage = wispBaseDamage * 0.2f;
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
        
        #endregion

        #region templar and chwisp

        GameObject templarPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/ClayBruiserBody");
        public static float templarBaseDamage = 9;//16
        public static float templarBaseAttackSpeed = 2;//1
        public static float templarFireInterval = 0.05f;//0.05f
        public static float templarSpinUpDuration = 1.5f;//1
        public static float templarSpinDownDuration = 2;//2

        GameObject chimeraWispPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/LunarWispBody");
        public static float chimeraWispBaseDamage = 5;//15
        public static float chimeraWispBaseAttackSpeed = 2;//1
        public static float chimeraWispFireInterval = 0.1f;//0.1f
        public static float chimeraWispFireDuration = 4f;//4f
        public static float chimeraWispChargeDuration = 3.33f;//3.33f

        void NerfTemplar()
        {
            Tools.LoadCharacterBodyAsync(RoR2_Base_ClayBruiser.ClayBruiserBody_prefab, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseAttackSpeed = templarBaseAttackSpeed;
                body.baseAttackSpeed *= 1 + kitSlowAspdReduction;
                body.baseDamage = templarBaseDamage;
                body.levelDamage = body.baseDamage * 0.2f;
            }

            On.EntityStates.ClayBruiser.Weapon.MinigunFire.OnEnter += (orig, self) =>
            {
                EntityStates.ClayBruiser.Weapon.MinigunFire.baseFireInterval = templarFireInterval * templarBaseAttackSpeed;
                orig(self);
            };
            On.EntityStates.ClayBruiser.Weapon.MinigunSpinUp.OnEnter += (orig, self) =>
            {
                EntityStates.ClayBruiser.Weapon.MinigunSpinUp.baseDuration = templarSpinUpDuration * templarBaseAttackSpeed;
                orig(self);
            };
            On.EntityStates.ClayBruiser.Weapon.MinigunSpinDown.OnEnter += (orig, self) =>
            {
                EntityStates.ClayBruiser.Weapon.MinigunSpinDown.baseDuration = templarSpinDownDuration * templarBaseAttackSpeed;
                orig(self);
            };
        }
        void NerfChimeraWisp()
        {
            Tools.LoadCharacterBodyAsync(RoR2_Base_LunarWisp.LunarWispBody_prefab, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseAttackSpeed = chimeraWispBaseAttackSpeed;
                body.baseAttackSpeed *= 1 + kitSlowAspdReduction;
                body.baseDamage = chimeraWispBaseDamage;
                body.levelDamage = body.baseDamage * 0.2f;
            }

            On.EntityStates.LunarWisp.FireLunarGuns.OnEnter += (orig, self) =>
            {
                EntityStates.LunarWisp.FireLunarGuns.baseFireInterval = 0.1f * chimeraWispBaseAttackSpeed;
                EntityStates.LunarWisp.FireLunarGuns.baseDuration = 4 * chimeraWispBaseAttackSpeed;
                orig(self);
            };
            On.EntityStates.LunarWisp.ChargeLunarGuns.OnEnter += (orig, self) =>
            {
                EntityStates.LunarWisp.ChargeLunarGuns.baseDuration = chimeraWispChargeDuration * chimeraWispBaseAttackSpeed;
                EntityStates.LunarWisp.ChargeLunarGuns.spinUpDuration = chimeraWispChargeDuration * chimeraWispBaseAttackSpeed;
                orig(self);
            };
            On.EntityStates.LunarWisp.SeekingBomb.OnEnter += (orig, self) =>
            {
                EntityStates.LunarWisp.SeekingBomb.spinUpDuration = 2 * chimeraWispBaseAttackSpeed;
                EntityStates.LunarWisp.SeekingBomb.baseDuration = 3 * chimeraWispBaseAttackSpeed;
                orig(self);
            };
        }
        #endregion

        #region alloyed collective
        public static int solusScorcherCreditCost = 18; //18
        public static float solusScorcherBaseHealth = 111; //175
        public static float solusScorcherBaseMovespeed = 9; //15
        void ChangeSolusScorcher()
        {
            SpawnCards.LoadSpawnCardAsync(RoR2_DLC3_Tanker.cscTanker_asset, ScorcherCredits);
            void ScorcherCredits(CharacterSpawnCard spawnCard)
            {
                spawnCard.directorCreditCost = solusScorcherCreditCost;
            }
            Tools.LoadCharacterBodyAsync(RoR2_DLC3_Tanker.TankerBody_prefab, ScorcherStats);
            void ScorcherStats(CharacterBody body)
            {
                body.baseMoveSpeed = solusScorcherBaseMovespeed;
                body.baseMaxHealth = solusScorcherBaseHealth;
                body.levelMaxHealth = solusScorcherBaseHealth * 0.3f;
            }
        }
        public static int solusProspectorCreditCost = 11; //11
        public static float solusProspectorBaseHealth = 110; //160
        public static float solusProspectorBaseMovespeed = 11.5f; //11.5
        void ChangeSolusProspector()
        {
            SpawnCards.LoadSpawnCardAsync(RoR2_DLC3_WorkerUnit.cscWorkerUnit_asset, ProspectorCredits);
            void ProspectorCredits(CharacterSpawnCard spawnCard)
            {
                spawnCard.directorCreditCost = solusProspectorCreditCost;
            }
            Tools.LoadCharacterBodyAsync(RoR2_DLC3_WorkerUnit.WorkerUnitBody_prefab, ProspectorStats);
            void ProspectorStats(CharacterBody body)
            {
                body.baseMoveSpeed = solusProspectorBaseMovespeed;
                body.baseMaxHealth = solusProspectorBaseHealth;
                body.levelMaxHealth = solusProspectorBaseHealth * 0.3f;
            }
        }
        #endregion
    }
}