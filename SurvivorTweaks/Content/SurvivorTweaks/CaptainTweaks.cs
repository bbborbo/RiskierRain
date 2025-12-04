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
using SurvivorTweaks.Modules;

namespace SurvivorTweaks.SurvivorTweaks
{
    class CaptainTweaks : SurvivorTweakBase<CaptainTweaks>
    {
        public static float microbotRechargeRate = 1.5f; //0.5
        public static float microbotRadius = 20f; //20

        public bool attackSpeedDamageAdditive = false;
        public static float shotgunCooldown = 2f;
        public static int shotgunStock = 2;
        public static float shotgunChargeDuration = 0.8f; //1.2
        public static float shotgunWindDownDuration = 0.2f; //1.0
        public static float shotgunPelletDamageCoeff = 1f; //1.2
        public static float shotgunPelletProcCoeff = 0.5f; //0.75

        public static GameObject tazerPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CaptainTazer");
        public static float tazerAoeRadius = 2; //2
        public static float tazerDamage = 2f; //1
        public static float tazerDamageBonus = 3f; 
        public static float tazerCooldown = 5; //6

        public static int tazerTotalTargets = 3; //1

        public static GameObject diabloPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/ExplosionDroneDeath");
        float diabloMaxDuration = 40; //40


        public static bool refreshSupplyDrops = true;
        public static GameObject beaconExplosion = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/ExplosionDroneDeath");

        public static GameObject healZone = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainHealingWard");
        public static float healRadius = 12; //9

        public static GameObject shockBeacon = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Shocking");
        public static float shockRadius = 12;
        public static float shockDamageCoefficient = 3f; //0
        public static float shockTimeInSeconds = 6f; //3
        public static float shockProcCoefficient = 1.0f;
        public static float shockForce = 500f; //0

        public static GameObject hackBeacon = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Hacking");
        public static float hackRadius = 9;
        public static float hackBaseDuration = 15; //15

        public static GameObject supplyBeacon = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, EquipmentRestock");
        public static GameObject supplyRadiusIndicator = healZone;
        public static float supplyRadius = 9;

        public override string survivorName => "Captain";

        public override string bodyName => "CaptainBody";

        public override void Init()
        {
            //refreshSupplyDrops = 
            //    SurvivorTweaksPlugin.CustomConfigFile.Bind<bool>("Captain", "Captain Beacon Refresh", true, 
            //    "Set to TRUE to refresh Captain's beacons at the beginning of every teleporter event. Only works if Captain changes are enabled!").Value;

            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            //passive
            ItemDef microbot = Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/CaptainDefenseMatrix/CaptainDefenseMatrix.asset").WaitForCompletion();
            SurvivorTweaksPlugin.RetierItem(microbot, ItemTier.Tier2);
            Sprite sprite = assetBundle.LoadAsset<Sprite>("Assets/Icons/Defensive_Microbots.png");
            if(sprite)
                microbot.pickupIconSprite = sprite;
            //microbot.tags |= ItemTag.
            //On.RoR2.CaptainDefenseMatrixController.TryGrantItem += MicrobotGuh;
            //On.RoR2.CaptainDefenseMatrixController.OnServerMasterSummonGlobal += MicrobotGah;

            On.EntityStates.CaptainDefenseMatrixItem.DefenseMatrixOn.OnEnter += NerfMicrobots;
            LanguageAPI.Add("ITEM_CAPTAINDEFENSEMATRIX_DESC", 
                $"Shoot down <style=cIsDamage>1</style> <style=cStack>(+1 per stack)</style> projectiles " +
                $"within <style=cIsDamage>{microbotRadius}m</style> every <style=cIsDamage>{microbotRechargeRate} seconds</style>. " +
                $"<style=cIsUtility>Recharge rate scales with attack speed</style>.");

            //primary
            ChangeVanillaPrimaries(primary);

            //secondary
            ChangeVanillaSecondaries(secondary);
            On.EntityStates.Captain.Weapon.FireTazer.OnEnter += CaptainTazerBuff;

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
            supplyWard.buffDef = CommonAssets.captainCdrBuff;
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
                $"by <style=cIsUtility>{Tools.ConvertDecimal(CommonAssets.captainCdrPercent)}.</style>");
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
        private void CaptainBeaconRefresh(On.RoR2.TeleporterInteraction.IdleToChargingState.orig_OnEnter orig, RoR2.TeleporterInteraction.IdleToChargingState self)
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
            SkillDef shotgun = family.variants[0].skillDef;
            shotgun.baseRechargeInterval = shotgunCooldown;
            shotgun.beginSkillCooldownOnSkillEnd = true;
            shotgun.baseMaxStock = shotgunStock;
            shotgun.rechargeStock = shotgunStock;
            shotgun.stockToConsume = 1;
            shotgun.resetCooldownTimerOnUse = true;
            shotgun.mustKeyPress = false;
            shotgun.attackSpeedBuffsRestockSpeed = true;
            shotgun.keywordTokens = new string[] { RainrotSharedUtils.SharedUtilsPlugin.noAttackSpeedKeywordToken };

            On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.OnEnter += CaptainShotgunCharge;
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.OnEnter += CaptainShotgunFixes;
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ModifyBullet += CaptainShotgunModifyBullet;
            LanguageAPI.Add("CAPTAIN_PRIMARY_DESCRIPTION",
                $"<style=cIsUtility>Exacting</style>. Fire a blast of pellets that deal <style=cIsDamage>8x{Tools.ConvertDecimal(shotgunPelletDamageCoeff)} damage</style>. " +
                $"Charging the attack narrows the <style=cIsUtility>spread</style>. Hold up to {shotgunStock} charges.");
        }

        private void CaptainShotgunCharge(On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.orig_OnEnter orig, ChargeCaptainShotgun self)
        {
            orig(self);
            self.chargeDuration = shotgunChargeDuration;
            self.minChargeDuration = 0.05f;
        }

        private void CaptainShotgunFixes(On.EntityStates.Captain.Weapon.FireCaptainShotgun.orig_OnEnter orig, FireCaptainShotgun self)
        {
            self.damageCoefficient = shotgunPelletDamageCoeff;
            self.procCoefficient = shotgunPelletProcCoeff;
            self.baseDuration = shotgunWindDownDuration;
            orig(self);
        }

        private void CaptainShotgunModifyBullet(On.EntityStates.Captain.Weapon.FireCaptainShotgun.orig_ModifyBullet orig, FireCaptainShotgun self, BulletAttack bulletAttack)
        {
            orig(self, bulletAttack);
            if (attackSpeedDamageAdditive)
            {
                bulletAttack.damage += self.characterBody.baseDamage * self.attackSpeedStat;
            }
            else
            {
                bulletAttack.damage *= self.attackSpeedStat;
            }
        }
        #endregion

        #region secondary
        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            SkillDef tazer = family.variants[0].skillDef;
            tazer.baseRechargeInterval = tazerCooldown;
            tazer.keywordTokens = new string[] { "KEYWORD_SHOCKING", RainrotSharedUtils.SharedUtilsPlugin.sparkPickupKeywordToken };

            #region taser
            GameObject tazerPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Captain.CaptainTazer_prefab).WaitForCompletion();
            if(tazerPrefab.TryGetComponent<ProjectileStickOnImpact>(out ProjectileStickOnImpact sticky))
            {
                UnityEngine.Object.Destroy(sticky);
            }

            ProjectileLightningOnImpact beam = tazerPrefab.AddComponent<ProjectileLightningOnImpact>();
            beam.attackFireCount = 1;
            beam.attackInterval = 99;
            beam.attackRange = 21;
            beam.lightningType = RoR2.Orbs.LightningOrb.LightningType.MageLightning;
            beam.inheritDamageType = true;
            beam.damageCoefficient = 1;
            beam.procCoefficient = 1;
            beam.bounces = tazerTotalTargets - 1;
            beam.enabled = true;

            if (tazerPrefab.TryGetComponent<ProjectileImpactExplosion>(out ProjectileImpactExplosion pie))
            {
                pie.blastRadius = 3;// tazerAoeRadius;
                pie.blastDamageCoefficient = 0.25f;
                pie.blastProcCoefficient = 0;
                pie.timerAfterImpact = true;
                pie.lifetimeAfterImpact = 1f;
                pie.impactOnWorld = true;
                pie.destroyOnWorld = true;
                //UnityEngine.Object.Destroy(pie);
            }

            On.RoR2.Projectile.ProjectileStickOnImpact.TrySticking += StickDamageBonus;
            LanguageAPI.Add("CAPTAIN_SECONDARY_DESCRIPTION",
                $"<style=cIsDamage>Shocking</style>. " +
                $"Fire a fast tazer that deals <style=cIsDamage>{tazerTotalTargets}x{Tools.ConvertDecimal(tazerDamage)} damage</style>.");
            #endregion
        }

        private void CaptainTazerBuff(On.EntityStates.Captain.Weapon.FireTazer.orig_OnEnter orig, FireTazer self)
        {
            FireTazer.damageCoefficient = tazerDamage;
            orig(self);
        }

        private bool StickDamageBonus(On.RoR2.Projectile.ProjectileStickOnImpact.orig_TrySticking orig, ProjectileStickOnImpact self, Collider hitCollider, Vector3 impactNormal)
        {
            bool ret = orig(self, hitCollider, impactNormal);
            //bool hitColliderHasHurtbox = hitCollider.GetComponent<HurtBox>() != null;
            //if (!ret)
            //{
            //    if (!hitColliderHasHurtbox)
            //    {
            //        ProjectileIncreaseDamageOnStick pidos = self.gameObject.GetComponent<ProjectileIncreaseDamageOnStick>();
            //        if (pidos != null)
            //        {
            //            pidos.IncreaseDamage(self);
            //        }
            //    }
            //}
            return ret;
        }
        #endregion

        #region special
        private void ShockZoneChanges(On.EntityStates.CaptainSupplyDrop.ShockZoneMainState.orig_OnEnter orig, EntityStates.CaptainSupplyDrop.ShockZoneMainState self)
        {
            ShockZoneMainState.shockRadius = shockRadius;
            ShockZoneMainState.shockFrequency = 1 / shockTimeInSeconds;

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
                procCoefficient = shockProcCoefficient
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
    class ProjectileLightningOnImpact : ProjectileProximityBeamController, IProjectileImpactBehavior
    {
        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            DoLightning();
        }
        public void DoLightning()
        {
            Log.Warning("B");
            if (!NetworkServer.active)
                return;
            this.attackTimer = 0;
        }
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
        public void IncreaseDamage(ProjectileStickOnImpact sticky)
        {
            //if NOT STUCK then skip
            if (sticky.stuck || sticky.stuckTransform != null || sticky.stuckBody != null)
            {
                return;
            }

            if (this.currentApplications < this.maxApplications)
            {
                ProjectileDamage damage = gameObject.GetComponent<ProjectileDamage>();
                if (damage != null)
                {
                    this.currentApplications++;
                    damage.damage *= this.damageMultiplier;
                }
            }
        }
    }
}
