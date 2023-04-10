using EntityStates;
using EntityStates.Captain.Weapon;
using EntityStates.CaptainDefenseMatrixItem;
using EntityStates.CaptainSupplyDrop;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.Stats;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RiskierRain.SurvivorTweaks
{
    class CaptainTweaks : SurvivorTweakModule
    {
        public static float microbotRechargeRate = 1.5f; //0.5
        public static float microbotRadius = 20f; //20

        public static float shotgunCooldown = 1.5f;
        public static int shotgunStock = 2;
        public static float shotgunWindDown = 0.35f;
        public static float shotgunPelletDamageCoeff = 1f; //1.2
        public static float shotgunPelletProcCoeff = 0.5f; //0.75

        public static GameObject tazerPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CaptainTazer");
        public static float tazerAoeRadius = 6; //2
        public static float tazerDamage = 2f; //1
        public static float tazerDamageBonus = 2f; 
        public static float tazerCooldown = 5; //6

        public static GameObject diabloPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/ExplosionDroneDeath");
        float diabloMaxDuration = 20;


        public static bool refreshSupplyDrops = true;
        public static GameObject beaconExplosion = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/ExplosionDroneDeath");

        public static GameObject healZone = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainHealingWard");
        public static float healRadius = 12; //9

        public static GameObject shockBeacon = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Shocking");
        public static float shockRadius = 12;
        public static float shockDamageCoefficient = 1f; //0
        public static float shockRate = 2f; //3
        public static float shockForce = 500f; //0

        public static GameObject hackBeacon = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Hacking");
        public static float hackRadius = 9;
        public static float hackBaseDuration = 30; //15

        public static GameObject supplyBeacon = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, EquipmentRestock");
        public static GameObject supplyRadiusIndicator = healZone;
        public static float supplyRadius = 9;

        public override string survivorName => "Captain";

        public override string bodyName => "CaptainBody";

        public override void Init()
        {
            refreshSupplyDrops = RiskierRainPlugin.CustomConfigFile.Bind<bool>("Captain", "Captain Beacon Refresh", true, 
                "Set to TRUE to refresh Captain's beacons at the beginning of every teleporter event. Only works if Captain changes are enabled!").Value;

            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            //passive
            On.RoR2.CaptainDefenseMatrixController.TryGrantItem += MicrobotGuh;
            On.RoR2.CaptainDefenseMatrixController.OnServerMasterSummonGlobal += MicrobotGah;

            On.EntityStates.CaptainDefenseMatrixItem.DefenseMatrixOn.OnEnter += NerfMicrobots;
            LanguageAPI.Add("ITEM_CAPTAINDEFENSEMATRIX_DESC", 
                $"Shoot down <style=cIsDamage>1</style> <style=cStack>(+1 per stack)</style> projectiles " +
                $"within <style=cIsDamage>{microbotRadius}m</style> every <style=cIsDamage>{microbotRechargeRate} seconds</style>. " +
                $"<style=cIsUtility>Recharge rate scales with attack speed</style>.");

            //primary
            ChangeVanillaPrimaries(primary);
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.OnEnter += CaptainShotgunFixes;
            LanguageAPI.Add("CAPTAIN_PRIMARY_DESCRIPTION", 
                $"Fire a blast of pellets that deal <style=cIsDamage>8x{Tools.ConvertDecimal(shotgunPelletDamageCoeff)} damage</style>. " +
                $"Charging the attack narrows the <style=cIsUtility>spread</style>. Hold up to {shotgunStock} charges.");

            //secondary
            ChangeVanillaSecondaries(secondary);
            On.EntityStates.Captain.Weapon.FireTazer.OnEnter += CaptainTazerBuff;
            #region taser
            ProjectileImpactExplosion taserPie = tazerPrefab.GetComponent<ProjectileImpactExplosion>();
            taserPie.blastRadius = tazerAoeRadius;
            tazerPrefab.AddComponent<ProjectileIncreaseDamageOnStick>().damageMultiplier = tazerDamageBonus;

            On.RoR2.Projectile.ProjectileStickOnImpact.TrySticking += StickDamageBonus;
            LanguageAPI.Add("CAPTAIN_SECONDARY_DESCRIPTION",
                $"<style=cIsDamage>Shocking</style>. " +
                $"Fire a fast tazer that deals <style=cIsDamage>{Tools.ConvertDecimal(tazerDamage)} damage</style>. " +
                $"If bounced, it can travel further and gains up to <style=cIsDamage>{tazerDamageBonus}x damage</style>.");
            #endregion

            //utility
            On.EntityStates.AimThrowableBase.ModifyProjectile += ModifyDiabloDuration;
            On.RoR2.Projectile.ProjectileManager.InitializeProjectile += ModifyDiabloFriendlyFire;

            GameObject diabloProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeAltProjectile.prefab").WaitForCompletion();
            if (diabloProjectile)
            {
                ProjectileController diabloController = diabloProjectile.GetComponent<ProjectileController>();
                diabloController.cannotBeDeleted = true;

                ProjectileImpactExplosion diabloExplosion = diabloProjectile.GetComponent<ProjectileImpactExplosion>();
                diabloExplosion.blastAttackerFiltering = AttackerFiltering.AlwaysHit;
            }

            ChangeVanillaUtilities(utility);


            //special
            #region heal
            HealingWard healWard = healZone.GetComponent<HealingWard>();
            if(healWard != null)
            {
                healWard.radius = healRadius;
            }
            #endregion
            #region shock
            On.EntityStates.CaptainSupplyDrop.ShockZoneMainState.OnEnter += ShockZoneChanges;
            On.EntityStates.CaptainSupplyDrop.ShockZoneMainState.Shock += ShockAttackChanges;
            #endregion
            #region hack
            On.EntityStates.CaptainSupplyDrop.HackingMainState.OnEnter += HackZoneChanges;
            On.EntityStates.CaptainSupplyDrop.HackingInProgressState.OnEnter += HackProgressChanges;
            #endregion
            #region supply
            Transform[] hackChildren = hackBeacon.GetComponentsInChildren<Transform>();
            foreach(Transform t in hackChildren)
            {
                GameObject o = t.gameObject;
                if(o.name == "Indicator")
                {
                    supplyRadiusIndicator = o.InstantiateClone("CaptainSupplyCdrRangeIndicator", false);
                    break;
                }
            }
            if(supplyRadiusIndicator != null)
            {
                supplyRadiusIndicator = healWard.gameObject.InstantiateClone("CaptainSupplyCdrRangeIndicator", false);
                HealingWard w = supplyRadiusIndicator.GetComponent<HealingWard>();
                GameObject.Destroy(w);
            }

            BuffWard supplyWard = supplyBeacon.AddComponent<BuffWard>();
            supplyWard.buffDef = CoreModules.Assets.captainCdrBuff;
            supplyWard.interval = 0.25f;
            supplyWard.buffDuration = 0.5f;
            supplyWard.radius = supplyRadius;
            On.EntityStates.CaptainSupplyDrop.BaseCaptainSupplyDropState.OnEnter += SupplyDropOnEnter;
            #endregion

            if (refreshSupplyDrops)
            {
                On.RoR2.TeleporterInteraction.IdleToChargingState.OnEnter += CaptainBeaconRefresh;
                LanguageAPI.Add("CAPTAIN_SPECIAL_DESCRIPTION",
                    $"Request <style=cIsUtility>up to 2</style> Supply Beacons. " +
                    $"Beacons are <style=cIsUtility>refreshed at the teleporter event</style>.");
            }
            LanguageAPI.Add("CAPTAIN_SUPPLY_EQUIPMENT_RESTOCK_DESCRIPTION", 
                $"<style=cIsUtility>Recharge Equipment</style> on use. " +
                $"<style=cIsUtility>Reduces the cooldowns</style> of nearby allies " +
                $"by <style=cIsUtility>{Tools.ConvertDecimal(CoreModules.Assets.captainCdrPercent)}.</style>");
            LanguageAPI.Add("CAPTAIN_SUPPLY_SHOCKING_DESCRIPTION", 
                $"Periodically <style=cIsDamage>Shock</style> all nearby enemies, immobilizing them. " +
                $"Deals <style=cIsDamage>{Tools.ConvertDecimal(shockDamageCoefficient)} damage</style> per hit.");
        }

        private void MicrobotGah(On.RoR2.CaptainDefenseMatrixController.orig_OnServerMasterSummonGlobal orig, CaptainDefenseMatrixController self, MasterSummon.MasterSummonReport summonReport)
        {
            if (self.characterBody.master && self.characterBody.master == summonReport.leaderMasterInstance)
            {
                CharacterMaster summonMasterInstance = summonReport.summonMasterInstance;
                if (summonMasterInstance)
                {
                    CharacterBody body = summonMasterInstance.GetBody();
                    if (body && (body.bodyFlags & CharacterBody.BodyFlags.Mechanical) > CharacterBody.BodyFlags.None)
                    {
                        summonMasterInstance.inventory.GiveItem(RoR2Content.Items.ScrapRed, self.defenseMatrixToGrantMechanicalAllies);
                    }
                }
            }
        }

        private void MicrobotGuh(On.RoR2.CaptainDefenseMatrixController.orig_TryGrantItem orig, CaptainDefenseMatrixController self)
        {
            if (self.characterBody.master)
            {
                bool flag = false;
                if (self.characterBody.master.playerStatsComponent)
                {
                    flag = (self.characterBody.master.playerStatsComponent.currentStats.GetStatValueDouble(PerBodyStatDef.totalTimeAlive, BodyCatalog.GetBodyName(self.characterBody.bodyIndex)) > 0.0);
                }
                if (!flag && self.characterBody.master.inventory.GetItemCount(RoR2Content.Items.ScrapRed) <= 0)
                {
                    self.characterBody.master.inventory.GiveItem(RoR2Content.Items.ScrapRed, self.defenseMatrixToGrantPlayer);
                }
            }
        }

        private void ChangeVanillaUtilities(SkillFamily family)
        {
            //diablo
            utility.variants[1].skillDef.baseRechargeInterval = diabloMaxDuration + 20;

            ObjectScaleCurve[] diabloIndicators = diabloPrefab.GetComponentsInChildren<ObjectScaleCurve>();
            foreach (ObjectScaleCurve osc in diabloIndicators)
            {
                //Debug.Log(osc.name);
                if (osc.name == "IndicatorRing")
                {
                    osc.timeMax = diabloMaxDuration;
                }
            }
        }

        private void ModifyDiabloFriendlyFire(On.RoR2.Projectile.ProjectileManager.orig_InitializeProjectile orig, ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
        {
            orig(projectileController, fireProjectileInfo);
            //return;
            GameObject proj = projectileController.gameObject;
            ProjectileImpactExplosion pie = proj.GetComponent<ProjectileImpactExplosion>();
            if(pie != null && pie.blastAttackerFiltering == AttackerFiltering.AlwaysHit)
            {
                projectileController.teamFilter.teamIndex = TeamIndex.None;
            }
        }

        private void ModifyDiabloDuration(On.EntityStates.AimThrowableBase.orig_ModifyProjectile orig, AimThrowableBase self, ref FireProjectileInfo fireProjectileInfo)
        {
            orig(self, ref fireProjectileInfo);
            if (self is CallAirstrikeAlt)
            {
                fireProjectileInfo.damageTypeOverride = DamageType.BypassOneShotProtection;
                fireProjectileInfo.useFuseOverride = true;
                fireProjectileInfo.fuseOverride = 10;
            }
        }

        public static List<GameObject> activeBeacons = new List<GameObject>();
        private void CaptainBeaconRefresh(On.RoR2.TeleporterInteraction.IdleToChargingState.orig_OnEnter orig, BaseState self)
        {
            orig(self);

            foreach(PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
            {
                CharacterBody currentBody = player.body;
                if(currentBody != null)
                {
                    CaptainSupplyDropController supplyController = currentBody.GetComponent<CaptainSupplyDropController>();
                    if(supplyController != null)
                    {
                        supplyController.supplyDrop1Skill.stock = 1;
                        supplyController.supplyDrop2Skill.stock = 1;
                    }
                }
            }

            for(int i = 0; i < activeBeacons.Count; i++)
            {
                GameObject beacon = activeBeacons[i];
                if (beacon != null)
                {
                    GameObject.Destroy(beacon);

                    EffectManager.SpawnEffect(beaconExplosion, new EffectData
                    {
                        origin = beacon.transform.position,
                        scale = 10
                    }, false);
                }
                activeBeacons.Remove(beacon);
            }
        }

        private void SupplyDropOnEnter(On.EntityStates.CaptainSupplyDrop.BaseCaptainSupplyDropState.orig_OnEnter orig, BaseCaptainSupplyDropState self)
        {
            if (refreshSupplyDrops)
            {
                activeBeacons.Add(self.gameObject);
            }
            orig(self);
            BuffWard ward = self.gameObject.GetComponent<BuffWard>();
            if(ward != null)
            {
                if(supplyRadiusIndicator != null && ward.rangeIndicator == null)
                {
                    if (NetworkServer.active)
                    {
                        GameObject indicator = UnityEngine.Object.Instantiate(supplyRadiusIndicator, self.transform.position, self.transform.rotation);
                        NetworkServer.Spawn(indicator);
                        ward.rangeIndicator = indicator.transform;
                    }
                }
                ward.teamFilter = self.teamFilter;
            }
        }

        private void NerfMicrobots(On.EntityStates.CaptainDefenseMatrixItem.DefenseMatrixOn.orig_OnEnter orig, EntityStates.CaptainDefenseMatrixItem.DefenseMatrixOn self)
        {
            DefenseMatrixOn.baseRechargeFrequency = 1 / microbotRechargeRate;
            DefenseMatrixOn.projectileEraserRadius = microbotRadius;
            orig(self);
        }

        #region primary
        private void ChangeVanillaPrimaries(SkillFamily family)
        {
            family.variants[0].skillDef.baseRechargeInterval = shotgunCooldown;
            family.variants[0].skillDef.beginSkillCooldownOnSkillEnd = true;
            family.variants[0].skillDef.baseMaxStock = shotgunStock;
            family.variants[0].skillDef.rechargeStock = shotgunStock;
            family.variants[0].skillDef.stockToConsume = 1;
            family.variants[0].skillDef.resetCooldownTimerOnUse = true;
            family.variants[0].skillDef.mustKeyPress = false;
        }

        private void CaptainShotgunFixes(On.EntityStates.Captain.Weapon.FireCaptainShotgun.orig_OnEnter orig, FireCaptainShotgun self)
        {
            self.damageCoefficient = shotgunPelletDamageCoeff;
            self.procCoefficient = shotgunPelletProcCoeff;
            self.baseDuration = shotgunWindDown;
            orig(self);
        }
        #endregion

        #region secondary
        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            family.variants[0].skillDef.baseRechargeInterval = tazerCooldown;
        }

        private void CaptainTazerBuff(On.EntityStates.Captain.Weapon.FireTazer.orig_OnEnter orig, FireTazer self)
        {
            FireTazer.damageCoefficient = tazerDamage;
            orig(self);
        }

        private bool StickDamageBonus(On.RoR2.Projectile.ProjectileStickOnImpact.orig_TrySticking orig, ProjectileStickOnImpact self, Collider hitCollider, Vector3 impactNormal)
        {
            bool ret = orig(self, hitCollider, impactNormal);
            if (!ret && hitCollider.GetComponent<HurtBox>() == null)
            {
                ProjectileIncreaseDamageOnStick pidos = self.gameObject.GetComponent<ProjectileIncreaseDamageOnStick>();
                if (pidos != null)
                {
                    if (pidos.currentApplications < pidos.maxApplications)
                    {
                        ProjectileDamage damage = self.gameObject.GetComponent<ProjectileDamage>();
                        if (damage != null)
                        {
                            pidos.currentApplications++;
                            damage.damage *= pidos.damageMultiplier;
                        }
                    }
                }
            }
            return ret;
        }
        #endregion

        #region special
        private void ShockZoneChanges(On.EntityStates.CaptainSupplyDrop.ShockZoneMainState.orig_OnEnter orig, EntityStates.CaptainSupplyDrop.ShockZoneMainState self)
        {
            ShockZoneMainState.shockRadius = shockRadius;
            ShockZoneMainState.shockFrequency = 1 / shockRate;

            ProjectileDamage pd = self.gameObject.GetComponent<ProjectileDamage>();
            if (pd != null)
            {
                self.damageStat = pd.damage / 20;
                //Debug.Log(self.damageStat);
            }

            orig(self);
        }

        private void ShockAttackChanges(On.EntityStates.CaptainSupplyDrop.ShockZoneMainState.orig_Shock orig, ShockZoneMainState self)
        {
            GameObject owner = self.gameObject.GetComponent<GenericOwnership>().ownerObject;

            new BlastAttack
            {
                radius = ShockZoneMainState.shockRadius,
                baseDamage = self.damageStat * shockDamageCoefficient,
                damageType = DamageType.Shock5s,
                falloffModel = BlastAttack.FalloffModel.None,
                attacker = owner,
                teamIndex = self.teamFilter.teamIndex,
                position = self.transform.position,
                //baseForce = shockForce,
                bonusForce = Vector3.up * shockForce,
                procCoefficient = (shockRate / 5)
            }.Fire();
            if (ShockZoneMainState.shockEffectPrefab)
            {
                EffectManager.SpawnEffect(ShockZoneMainState.shockEffectPrefab, new EffectData
                {
                    origin = self.transform.position,
                    scale = ShockZoneMainState.shockRadius
                }, false);
            }
        }

        private void HackZoneChanges(On.EntityStates.CaptainSupplyDrop.HackingMainState.orig_OnEnter orig, EntityStates.CaptainSupplyDrop.HackingMainState self)
        {
            HackingMainState.baseRadius = hackRadius;
            orig(self);
        }

        private void HackProgressChanges(On.EntityStates.CaptainSupplyDrop.HackingInProgressState.orig_OnEnter orig, HackingInProgressState self)
        {
            HackingInProgressState.baseDuration = hackBaseDuration;
            orig(self);
        }
        #endregion
    }

    class ProjectileIncreaseDamageOnStick : MonoBehaviour
    {
        public float damageMultiplier = 2;
        public int maxApplications = 1;
        public int currentApplications;

        void Start()
        {
            currentApplications = 0;
        }
    }
}
