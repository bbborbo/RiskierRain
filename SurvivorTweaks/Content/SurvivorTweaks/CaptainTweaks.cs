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
using static SurvivorTweaks.SurvivorTweaksPlugin;
using RainrotSharedUtils;

namespace SurvivorTweaks.SurvivorTweaks
{
    class CaptainTweaks : SurvivorTweakBase<CaptainTweaks>
    {
        public static GameObject supplyRadiusIndicator;
        public static GameObject beaconExplosion => Addressables.LoadAssetAsync<GameObject>(
            RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.OmniExplosionVFXMegaDrone_prefab
            ).WaitForCompletion();// LegacyResourcesAPI.Load<GameObject>("prefabs/effects/ExplosionDroneDeath");

        [AutoConfig("Ability Tweaks (Passive) : Defensive Microbots : Retier", "If true, the Microbots item will be retiered to Uncommon/Tier2/Green. Vanilla is false", true)]
        public static bool microbotRetier = true;
        [AutoConfig("Ability Tweaks (Passive) : Defensive Microbots : Chance To Appear In Printers", "Expressed as a chance out of 100. Vanilla is 0", 2)]
        public static int microbotInPrinterChance = 2;
        [AutoConfig("Ability Tweaks (Passive) : Defensive Microbots : Eraser Recharge Interval", "Expressed in seconds. Vanilla is 0.5", 1.5f)]
        public static float microbotRechargeInterval = 1.5f; //0.5
        [AutoConfig("Ability Tweaks (Passive) : Defensive Microbots : Eraser Range", "Expressed in meters. Vanilla is 20", 20)]
        public static float microbotEraserRange = 20f; //20

        [AutoConfig("Ability Tweaks (Primary) : Vulcan Shotgun : Exacting Keyword", "Uses Exacting keyword if true. Vanilla is false", true)]
        public bool shotgunUsesExacting = true;
        [AutoConfig("Ability Tweaks (Primary) : Vulcan Shotgun : Exacting Behavior", "Attack Speed is additive if true, multiplicative if false. Vanilla is N/A", false)]
        public bool shotgunExactingAdditive = false;
        [AutoConfig("Ability Tweaks (Primary) : Vulcan Shotgun : Base Cooldown", "Expressed in seconds. Vanilla is 0", 2f)]
        public static float shotgunCooldown = 2f;
        [AutoConfig("Ability Tweaks (Primary) : Vulcan Shotgun : Base Max Stock", "Vanilla is 1", 2)]
        public static int shotgunStock = 2;
        [AutoConfig("Ability Tweaks (Primary) : Vulcan Shotgun : Charge Duration Max", "Expressed in seconds. Vanilla is 1.2", 0.8f)]
        public static float shotgunChargeDuration = 0.8f; //1.2
        [AutoConfig("Ability Tweaks (Primary) : Vulcan Shotgun : Wind Down Duration", "Expressed in seconds. Vanilla is 1.0", 0.2f)]
        public static float shotgunWindDownDuration = 0.2f; //1.0
        [AutoConfig("Ability Tweaks (Primary) : Vulcan Shotgun : Damage Coefficient Per Pellet", "Expressed as a percentage (eg 1.0 is 100%). Vanilla is 1.2", 1.0f)]
        public static float shotgunPelletDamageCoeff = 1f; //1.2
        [AutoConfig("Ability Tweaks (Primary) : Vulcan Shotgun : Proc Coefficient Per Pellet", "Vanilla is 0.75", 0.5f)]
        public static float shotgunPelletProcCoeff = 0.5f; //0.75

        [AutoConfig("Ability Tweaks (Secondary) : Power Tazer : Projectile Blast Radius", "Expressed in meters. Vanilla is 2", 2f)]
        public static float tazerAoeRadius = 2; //2
        [AutoConfig("Ability Tweaks (Secondary) : Power Tazer : Projectile Damage Coefficient", "Expressed as a percentage (eg 2.0 is 200%). Vanilla is 1", 2.0f)]
        public static float tazerDamage = 2f; //1
        [AutoConfig("Ability Tweaks (Secondary) : Power Tazer : Base Cooldown", "Expressed in seconds. Vanilla is 6", 5f)]
        public static float tazerCooldown = 5; //6
        [AutoConfig("Ability Tweaks (Secondary) : Power Tazer : Max Bounces", "Vanilla is 1", 3)]
        public static int tazerTotalTargets = 3; //1

        [AutoConfig("Ability Tweaks (Utility) : OGM-72 Diablo Strike : Strike Delay", "Expressed in seconds. Vanilla is 20", 10)]
        public static float diabloMaxDuration = 10; //20


        [AutoConfig("Ability Tweaks (Special) : Refresh Supply Drops", "Beacons refresh during boss fights if true. Vanilla is false", true)]
        public static bool refreshSupplyDrops = true;

        [AutoConfig("Ability Tweaks (Special) : Healing Beacon : Effect Radius", "Expressed in meters. Vanilla is 10", 12f)]
        public static float healRadius = 12; //10
        [AutoConfig("Ability Tweaks (Special) : Healing Beacon : Heal Fraction Per Second", "Expressed as a percentage (eg 0.07 is 7%). Vanilla is 0.1", 0.08)]
        public static float healFractionPerSecond = 0.07f;//0.1f

        [AutoConfig("Ability Tweaks (Special) : Shock Beacon : Effect Radius", "Expressed in meters. Vanilla is idk", 12f)]
        public static float shockRadius = 12; //12
        [AutoConfig("Ability Tweaks (Special) : Shock Beacon : Blast Damage Coefficient", "Expressed as a percentage (eg 3.0 is 300%). Vanilla is 0", 3.0f)]
        public static float shockDamageCoefficient = 3f; //0
        [AutoConfig("Ability Tweaks (Special) : Shock Beacon : Blast Interval", "Expressed in seconds. Vanilla is 3", 6.0f)]
        public static float shockInterval = 6f; //3
        [AutoConfig("Ability Tweaks (Special) : Shock Beacon : Blast Proc Coefficient", "Affects Shock effect duration. Vanilla is N/A", 1.0f)]
        public static float shockProcCoefficient = 1.0f;
        [AutoConfig("Ability Tweaks (Special) : Shock Beacon : Blast Force", "Vanilla is 0", 500f)]
        public static float shockForce = 500f; //0

        [AutoConfig("Ability Tweaks (Special) : Hacking Beacon : Effect Radius", "Expressed in meters. Vanilla is idk", 9f)]
        public static float hackRadius = 9;
        [AutoConfig("Ability Tweaks (Special) : Hacking Beacon : Hack Duration", "Expressed in seconds. Vanilla is 15", 15f)]
        public static float hackBaseDuration = 15; //15

        [AutoConfig("Ability Tweaks (Special) : Resupply Beacon : Effect Radius", "Expressed in meters. Vanilla is N/A", 9f)]
        public static float supplyRadius = 9;
        [AutoConfig("Ability Tweaks (Special) : Resupply Beacon : Cooldown Reduction", "Expressed in meters. Vanilla is N/A", 0.25f)]
        public static float supplyCdrPercent = 0.25f;

        public override string survivorName => "Captain";

        public override string bodyName => "CaptainBody";

        public override void Init()
        {
            //refreshSupplyDrops = 
            //    SurvivorTweaksPlugin.CustomConfigFile.Bind<bool>("Captain", "Captain Beacon Refresh", true, 
            //    "Set to TRUE to refresh Captain's beacons at the beginning of every teleporter event. Only works if Captain changes are enabled!").Value;

            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Captain.CaptainBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                ChangeVanillaPrimaries(primary);
                ChangeVanillaSecondaries(secondary);
                ChangeVanillaUtilities(utility);
            });

            //passive
            if(microbotRetier == true)
            {
                LoadAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_CaptainDefenseMatrix.CaptainDefenseMatrix_asset, RetierMicrobot);
                void RetierMicrobot(ItemDef itemDef)
                {
                    itemDef.tier = ItemTier.Tier2;
                    itemDef.deprecatedTier = ItemTier.Tier2;
                    if (assetBundle && assetBundle.Contains("Assets/Icons/Defensive_Microbots.png"))
                    {
                        Sprite sprite = assetBundle.LoadAsset<Sprite>("Assets/Icons/Defensive_Microbots.png");
                        if (sprite)
                            itemDef.pickupIconSprite = sprite;
                    }
                }
            }
            On.RoR2.ShopTerminalBehavior.SetPickup += (orig, self, newPickup, newHidden) =>
            {
                ItemTier microbotTier = microbotRetier == true ? ItemTier.Tier2 : ItemTier.Tier3;
                if(microbotInPrinterChance > 0 && self.itemTier == microbotTier)
                {
                    if (Util.CheckRoll(microbotInPrinterChance))
                    {
                        newPickup = new UniquePickup(PickupCatalog.itemIndexToPickupIndex[(int)RoR2Content.Items.CaptainDefenseMatrix.itemIndex]);
                    }
                }
                orig(self, newPickup, newHidden);
            };
            //On.RoR2.CaptainDefenseMatrixController.TryGrantItem += MicrobotGuh;
            //On.RoR2.CaptainDefenseMatrixController.OnServerMasterSummonGlobal += MicrobotGah;

            On.EntityStates.CaptainDefenseMatrixItem.DefenseMatrixOn.OnEnter += NerfMicrobots;
            LanguageAPI.Add("ITEM_CAPTAINDEFENSEMATRIX_DESC", 
                $"Shoot down <style=cIsDamage>1</style> <style=cStack>(+1 per stack)</style> projectiles " +
                $"within <style=cIsDamage>{microbotEraserRange}m</style> every <style=cIsDamage>{microbotRechargeInterval} seconds</style>. " +
                $"<style=cIsUtility>Recharge rate scales with attack speed</style>.");

            //primary

            //secondary

            //utility
            On.EntityStates.AimThrowableBase.ModifyProjectile += ModifyDiabloDuration;
            //On.RoR2.Projectile.ProjectileManager.InitializeProjectile += ModifyDiabloFriendlyFire;

            GameObject diabloProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Captain/CaptainAirstrikeAltProjectile.prefab").WaitForCompletion();
            if (diabloProjectile)
            {
                ProjectileController diabloController = diabloProjectile.GetComponent<ProjectileController>();
                diabloController.cannotBeDeleted = true;

                ProjectileImpactExplosion diabloExplosion = diabloProjectile.GetComponent<ProjectileImpactExplosion>();
                diabloExplosion.blastAttackerFiltering = AttackerFiltering.AlwaysHit;
            }



            //special
            #region heal
            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Captain.CaptainHealingWard_prefab, TweakHealZone);
            void TweakHealZone(GameObject healZonePrefab)
            {
                HealingWard healWard = healZonePrefab.GetComponent<HealingWard>();
                if (healWard != null)
                {
                    healWard.radius = healRadius;
                    healWard.healFraction = healWard.interval * healFractionPerSecond;
                }

                LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Captain.CaptainSupplyDrop__Hacking_prefab, GetHackBeaconIndicator);
                void GetHackBeaconIndicator(GameObject hackBeaconPrefab)
                {
                    Transform[] hackChildren = hackBeaconPrefab.GetComponentsInChildren<Transform>();
                    if(hackChildren.Length > 0)
                    {
                        foreach (Transform t in hackChildren)
                        {
                            GameObject o = t.gameObject;
                            if (o.name == "Indicator")
                            {
                                supplyRadiusIndicator = o.InstantiateClone("CaptainSupplyCdrRangeIndicator", false);
                                break;
                            }
                        }
                    }

                    if (supplyRadiusIndicator == null && healWard != null)
                    {
                        supplyRadiusIndicator = healWard.gameObject.InstantiateClone("CaptainSupplyCdrRangeIndicator", false);
                        HealingWard w = supplyRadiusIndicator.GetComponent<HealingWard>();
                        GameObject.Destroy(w);
                    }

                    if(supplyRadiusIndicator == null)
                    {
                        Debug.LogError("Captain Restock beacon couldn't get indicator!");
                    }
                }
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
            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Captain.CaptainSupplyDrop__EquipmentRestock_prefab, TweakSupplyBeacon);
            void TweakSupplyBeacon(GameObject supplyBeaconPrefab)
            {
                //supplyRadiusIndicator.transform.parent = supplyBeaconPrefab.transform;
                //supplyRadiusIndicator.transform.localPosition = Vector3.zero;

                BuffWard supplyWard = supplyBeaconPrefab.AddComponent<BuffWard>();
                supplyWard.buffDef = CommonAssets.captainCdrBuff;
                supplyWard.interval = 0.25f;
                supplyWard.buffDuration = 0.5f;
                supplyWard.radius = supplyRadius;
            }
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
                $"by <style=cIsUtility>{Tools.ConvertDecimal(supplyCdrPercent)}.</style>");
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
                if (!flag && self.characterBody.master.inventory.GetItemCountEffective(RoR2Content.Items.ScrapRed) <= 0)
                {
                    self.characterBody.master.inventory.GiveItem(RoR2Content.Items.ScrapRed, self.defenseMatrixToGrantPlayer);
                }
            }
        }

        private void ChangeVanillaUtilities(SkillFamily family)
        {
            //diablo
            utility.variants[1].skillDef.baseRechargeInterval = diabloMaxDuration + 20;

            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Captain.CaptainAirstrikeAltGhost_prefab, FixDiabloIndicator);
            void FixDiabloIndicator(GameObject prefab)
            {
                ObjectScaleCurve[] diabloIndicators = prefab.GetComponentsInChildren<ObjectScaleCurve>();
                foreach (ObjectScaleCurve osc in diabloIndicators)
                {
                    //Debug.Log(osc.name);
                    if (osc.name == "IndicatorRing")
                    {
                        osc.timeMax = diabloMaxDuration;
                    }
                    if (osc.name == "Sphere, Inner Expanding")
                    {
                        osc.timeMax = diabloMaxDuration;
                    }
                }
                ObjectTransformCurve[] diabloIndicators2 = prefab.GetComponentsInChildren<ObjectTransformCurve>();
                foreach (ObjectTransformCurve osc in diabloIndicators2)
                {
                    //Debug.Log(osc.name);
                    if (osc.name == "Laser")
                    {
                        osc.timeMax = diabloMaxDuration;
                    }
                }
            }
            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Captain.CaptainAirstrikeAltProjectile_prefab, (prefab) =>
            {
                if(prefab.TryGetComponent(out ProjectileImpactExplosion pie))
                {
                    pie.lifetime = diabloMaxDuration;
                }
            });
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
                fireProjectileInfo.fuseOverride = diabloMaxDuration;
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
                        supplyController.supplyDrop1Skill.stock = Mathf.Max(supplyController.supplyDrop1Skill.maxStock, 1);
                        supplyController.supplyDrop2Skill.stock = Mathf.Max(supplyController.supplyDrop2Skill.maxStock, 1);
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
            DefenseMatrixOn.baseRechargeFrequency = 1 / microbotRechargeInterval;
            DefenseMatrixOn.projectileEraserRadius = microbotEraserRange;
            orig(self);
        }

        #region primary
        private void ChangeVanillaPrimaries(SkillFamily family)
        {
            SkillDef shotgun = family.variants[0].skillDef;
            if(shotgunCooldown > 0)
            {
                shotgun.baseRechargeInterval = shotgunCooldown;
                shotgun.beginSkillCooldownOnSkillEnd = true;
                shotgun.baseMaxStock = shotgunStock;
                shotgun.rechargeStock = shotgunStock;
                shotgun.stockToConsume = 1;
                shotgun.resetCooldownTimerOnUse = true;
                shotgun.attackSpeedBuffsRestockSpeed = true;
            }
            shotgun.mustKeyPress = false;
            if (shotgunUsesExacting)
            {
                shotgun.keywordTokens = new string[]
                {
                    shotgunExactingAdditive ? SharedUtilsPlugin.noAttackSpeedAdditiveKeywordToken : SharedUtilsPlugin.noAttackSpeedMultiplicativeKeywordToken
                };
            }

            On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.OnEnter += CaptainShotgunCharge;
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.OnEnter += CaptainShotgunFixes;
            On.EntityStates.Captain.Weapon.FireCaptainShotgun.ModifyBullet += CaptainShotgunModifyBullet;
            LanguageAPI.Add("CAPTAIN_PRIMARY_DESCRIPTION",
                (shotgunUsesExacting == true ? $"<style=cIsUtility>Exacting</style>. " : "") +
                $"Fire a blast of pellets that deal <style=cIsDamage>8x{Tools.ConvertDecimal(shotgunPelletDamageCoeff)} damage</style>. " +
                $"Charging the attack narrows the <style=cIsUtility>spread</style>. Hold up to {shotgunStock} charges.");
        }

        private void CaptainShotgunCharge(On.EntityStates.Captain.Weapon.ChargeCaptainShotgun.orig_OnEnter orig, ChargeCaptainShotgun self)
        {
            orig(self);
            if (shotgunUsesExacting)
            {
                self.chargeDuration = shotgunChargeDuration;
                self.minChargeDuration = 0.05f;
            }
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
            if (shotgunUsesExacting)
            {
                if (shotgunExactingAdditive)
                {
                    bulletAttack.damage += self.characterBody.baseDamage * self.attackSpeedStat;
                }
                else
                {
                    bulletAttack.damage *= self.attackSpeedStat;
                }
            }
        }
        #endregion

        #region secondary
        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            SkillDef tazer = family.variants[0].skillDef;
            tazer.baseRechargeInterval = tazerCooldown;

            #region taser
            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Captain.CaptainTazer_prefab, TweakTazer);
            void TweakTazer(GameObject tazerPrefab)
            { 
                if (tazerPrefab.TryGetComponent<ProjectileStickOnImpact>(out ProjectileStickOnImpact sticky))
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
            }

            On.RoR2.Projectile.ProjectileStickOnImpact.TrySticking += StickDamageBonus;
            LanguageAPI.Add("CAPTAIN_SECONDARY_DESCRIPTION",
                $"<style=cIsDamage>Shocking</style>. " +
                $"Fire a fast tazer that deals <style=cIsDamage>{tazerTotalTargets}x{Tools.ConvertDecimal(tazerDamage)} damage</style>.");
            #endregion

            On.EntityStates.Captain.Weapon.FireTazer.OnEnter += CaptainTazerBuff;
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
            ShockZoneMainState.shockFrequency = 1 / shockInterval;

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
