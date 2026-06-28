using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using MoreStats;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using RoR2.Projectile;
using EntityStates.BeetleQueenMonster;
using EntityStates.NullifierMonster;
using SwanSongExtended.Modules;

namespace RiskierRain.Changes
{
    public static partial class EnemyChanges
    {
        public static void Initialize()
        {
            //MakeEnemiesuseEquipment();
            ChangeSpawnlists();
            ChangeVagrant();
            ChangePest();
            ChangeQueen();
            ChangeVoidReaver();
            ChangeBarnacle();
            ChangeTemplar();
            ChangeChimeraWisp();
            ChangeGup();
            ChangeSolusScorcher();
            ChangeSolusProspector();
            ChangeLesserWisp();
        }

        #region enemy use equip

        public static void MakeEnemiesuseEquipment()
        {
            Debug.Log("Enemies using equipment needs to be fixed");
            On.RoR2.EquipmentSlot.FixedUpdate += TryUseEquip;
        }

        private static void TryUseEquip(On.RoR2.EquipmentSlot.orig_FixedUpdate orig, EquipmentSlot self)
        {
            orig(self);
            if (!self.characterBody.isPlayerControlled && self.characterBody.teamComponent.teamIndex != TeamIndex.Player)
            {
                if (!self.characterBody.outOfCombat)
                {
                    //self.ExecuteIfReady(EquipmentCatalog.GetEquipmentDef(self.equipmentIndex));
                    bool isEquipmentActivationAllowed = self.characterBody.isEquipmentActivationAllowed;
                    if (isEquipmentActivationAllowed /**&& self.hasEffectiveAuthority*/)
                    {
                        if (NetworkServer.active)
                        {
                            self.ExecuteIfReady();
                            return;
                        }
                        self.CallCmdExecuteIfReady();
                    }
                }

            }
        }
        #endregion

        #region vagrant
        static float vagrantBaseHealth = 1600; //2100
        static GameObject vagrantPrefab;
        static void ChangeVagrant()
        {
            LoadAsync<CharacterBody>(RoR2_Base_Vagrant.VagrantBody_prefab, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseMaxHealth = vagrantBaseHealth;
                body.levelMaxHealth = vagrantBaseHealth * 0.3f;
            }
            On.EntityStates.VagrantMonster.ChargeMegaNova.OnEnter += (orig, self) =>
            {
                orig(self);
                self.duration = EntityStates.VagrantMonster.ChargeMegaNova.baseDuration;
                if (self.characterBody.attackSpeed > 1.5f)
                {
                    self.duration = 2;
                }
            };
        }
        #endregion

        #region pest
        static GameObject pestPrefab;
        static GameObject pestSpit;
        static float pestBaseHealth = 50f; // 80
        static float pestBaseDamage = 6f; // 15
        static float pestBaseSpeed = 4f; //6
        static float pestSpitVelocity = 70; // 100
        static void ChangePest()
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
            LoadAsync<GameObject>(RoR2_DLC1_FlyingVermin.VerminSpitProjectile_prefab, NerfSpit);
            void NerfSpit(GameObject pestSpit)
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
        static float spitDamageCoefficient = 0.4f; //1.3f
        static float acidSize = 2f; //1f
        static float acidDamageCoefficient = 2.5f; //1f
        static float acidDamageFrequency = 4f; //2f
        static void ChangeQueen()
        {
            FireSpit.damageCoefficient = spitDamageCoefficient;

            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BeetleQueen.BeetleQueenAcid_prefab, NerfAcid);
            void NerfAcid(GameObject queenAcidPrefab)
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
        #region void reaver

        static int nulliferBombCount = 10;

        static void ChangeVoidReaver()
        {
            On.EntityStates.NullifierMonster.FirePortalBomb.OnEnter += BuffFirePortalBomb;
        }

        private static void BuffFirePortalBomb(On.EntityStates.NullifierMonster.FirePortalBomb.orig_OnEnter orig, EntityStates.NullifierMonster.FirePortalBomb self)
        {
            FirePortalBomb.portalBombCount = nulliferBombCount;
            orig(self);
        }
        #endregion
        #region void barnacle
        static float fuckBarnacleRegen = 0;
        static void ChangeBarnacle()
        {
            Tools.LoadCharacterBodyAsync(RoR2_DLC1_VoidBarnacle.VoidBarnacleBody_png, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseRegen = fuckBarnacleRegen;
                body.levelRegen = fuckBarnacleRegen * 0.2f;
            }
        }
        #endregion

        #region templar
        public static float templarBaseDamage = 9;//16
        public static float templarBaseAttackSpeed = 1;//1
        public static float templarFireInterval = 0.05f;//0.05f
        public static float templarSpinUpDuration = 1.5f;//1
        public static float templarSpinDownDuration = 2;//2

        static void ChangeTemplar()
        {
            Tools.LoadCharacterBodyAsync(RoR2_Base_ClayBruiser.ClayBruiserBody_prefab, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseAttackSpeed = templarBaseAttackSpeed;
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
        #endregion
        #region chimera wisp
        public static float chimeraWispBaseDamage = 5;//15
        public static float chimeraWispBaseAttackSpeed = 1;//1
        public static float chimeraWispFireInterval = 0.1f;//0.1f
        public static float chimeraWispFireDuration = 4f;//4f
        public static float chimeraWispChargeDuration = 3.33f;//3.33f
        static void ChangeChimeraWisp()
        {
            Tools.LoadCharacterBodyAsync(RoR2_Base_LunarWisp.LunarWispBody_prefab, BodyStats);
            void BodyStats(CharacterBody body)
            {
                body.baseAttackSpeed = chimeraWispBaseAttackSpeed;
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
        #region gup
        static int gupCreditCost = 200;//150

        static float gupBaseHealth = 1000f; // 1000
        static float gupBaseArmor = 0f; // 0
        static float gupBaseDamage = 12f; // 12
        static float gupBaseSpeed = 14f; //12
        static float gupBaseRegen = 0f; //0.6f

        static float geepBaseHealth = 500f; // 500
        static float geepBaseArmor = 0f; // 0
        static float geepBaseDamage = 8f; // 6
        static float geepBaseSpeed = 10f; //8
        static float geepBaseRegen = 0f; //0.6f

        static float gipBaseHealth = 250f; // 250
        static float gipBaseArmor = 0f; // 0
        static float gipBaseDamage = 5f; // 3
        static float gipBaseSpeed = 6f; //5
        static float gipBaseRegen = 0f; //0.6f

        static void ChangeGup()
        {
            On.EntityStates.Gup.BaseSplitDeath.OnEnter += (orig, self) =>
            {
                self.moneyMultiplier = 0;
                orig(self);
            };

            SpawnCards.LoadSpawnCardAsync(RoR2_DLC1_Gup.cscGupBody_asset, GupCredits);
            void GupCredits(CharacterSpawnCard spawnCard)
            {
                spawnCard.directorCreditCost = gupCreditCost;
            }

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
        #region solus scorcher
        public static int solusScorcherCreditCost = 18; //18
        public static float solusScorcherBaseHealth = 111; //175
        public static float solusScorcherBaseMovespeed = 9; //15
        static void ChangeSolusScorcher()
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
        #endregion

        #region solus prospector
        public static int solusProspectorCreditCost = 11; //11
        public static float solusProspectorBaseHealth = 110; //160
        public static float solusProspectorBaseMovespeed = 11.5f; //11.5
        static void ChangeSolusProspector()
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

        #region wisp
        static float wispBaseDamage = 2.0f; //3.5f
        static void ChangeLesserWisp()
        {
            Tools.LoadCharacterBodyAsync(RoR2_Base_Wisp.WispBody_prefab, BodyStats);
            void BodyStats(CharacterBody lesserWispBody)
            {
                lesserWispBody.baseDamage = wispBaseDamage;
                lesserWispBody.levelDamage = wispBaseDamage * 0.2f;
            }
        }
        #endregion
    }
}
