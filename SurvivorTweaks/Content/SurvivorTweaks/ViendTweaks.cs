using EntityStates;
using EntityStates.VoidSurvivor.Weapon;
using R2API;
using SurvivorTweaks.Modules;
using SurvivorTweaks.States.VoidFiend;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;
using RoR2.Projectile;
using UnityEngine.Events;
using UnityEngine.Networking;
using SurvivorTweaks.Components;

namespace SurvivorTweaks.SurvivorTweaks
{
    class ViendTweaks : SurvivorTweakBase<ViendTweaks>
    {
        public static GameObject viendPrimaryDamagePool;
        public static GameObject viendDelayKnockback;
        [AutoConfig("Void Fiend : Base Max Health", "Scales 30% per level. Vanilla is 110", 130f)]
        public static float baseMaxHealth = 130f;//110
        [AutoConfig("Void Fiend : Base Damage", "Scales 20% per level. Vanilla is 12", 11f)]
        public static float baseDamage = 11f;//12f

        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Corrupt Mode Bonus Armor", "Vanilla is 100", 100f)]
        static float corruptModeArmor = 100; //100
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Corrupt Mode Anti-Healing", "If true, ALL healing will be negated during Corrupt Mode. Vanilla is false", true)]
        static bool corruptModeAntiHeal = true;
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Minimum Corruption Per Void Item", "Vanilla is 2", 2f)]
        static float minimumCorruptionPerVoidItem = 2; //2
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Corruption For 100% Damage Taken", "Vanilla is 50", 50f)]
        static float corruptionForFullDamage = 50; //50
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Corruption For 100% Health Healed", "Vanilla is -100", -50f)]
        static float corruptionForFullHeal = -50; //-100
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Corruption Per Second (While Corrupted)", "Vanilla is -0.06666667", -0.06666667f)]
        static float corruptionFractionPerSecondWhileCorrupted = -0.06666667f; //aka 15s; -0.06666667f aka 15s
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Corruption Per Second (In Combat)", "Vanilla is 3", 2.22222222f)]
        static float corruptionPerSecondInCombat = 2.22222222f; //aka 45s; 3 aka 33.3s
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Corruption Per Second (Out Of Combat)", "Vanilla is 3", 2.22222222f)]
        static float corruptionPerSecondOutOfCombat = 2.22222222f; //3
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Corruption Per Crit", "Vanilla is 2", 0f)]
        static float corruptionPerCrit = 0; //2
        [AutoConfig("Ability Tweaks (Passive) : Void Corruption : Max Corruption", "Vanilla is 100", 100f)]
        static float maxCorruption = 100; //100

        [AutoConfig("Ability Tweaks (Primary) : Drown : Damage Coefficient", "Expressed as a percentage (eg 3.8 is 380%). Vanilla is 3", 3.8f)]
        public static float primaryDamageCoefficientLight = 3.8f;
        [AutoConfig("Ability Tweaks (Primary) : Drown : Damage Coefficient (Final)", "Expressed as a percentage (eg 3.8 is 380%). Vanilla is 3", 3.8f)]
        public static float primaryDamageCoefficientHeavy = 3.8f;
        [AutoConfig("Ability Tweaks (Primary) : Drown : Pool Damage Coefficient Per Second", "Expressed as a percentage (eg 2.5 is 250%). Vanilla is N/A", 2.5f)]
        public static float primaryPoolDamageCoefficientPerSecond = 2.5f;
        [AutoConfig("Ability Tweaks (Primary) : Drown : Base Attack Duration", "Expressed in seconds. Vanilla is 0.6", 0.8f)]
        public static float primaryBaseDurationLight = 0.8f; //0.6f
        [AutoConfig("Ability Tweaks (Primary) : Drown : Base Attack Duration (Final)", "Expressed in seconds. Vanilla is 0.6", 1.1f)]
        public static float primaryBaseDurationHeavy = 1.1f; //0.6f
        [AutoConfig("Ability Tweaks (Primary) : Drown : Recoil Amplitude", "Vanilla is 1", 2f)]
        public static float primaryRecoilAmplitudeLight = 2f; //1
        [AutoConfig("Ability Tweaks (Primary) : Drown : Recoil Amplitude (Final)", "Vanilla is 1", 3.5f)]
        public static float primaryRecoilAmplitudeHeavy = 3.5f; //1
        [AutoConfig("Ability Tweaks (Primary) : Drown : Trajectory Aim Assist", "Vanilla is 0.75", 0.25f)]
        public static float primaryTrajectoryAimAssistMultiplier = 0.25f; //0.75f
        [AutoConfig("Ability Tweaks (Primary) : Drown : Step Count", "Final step in combo fires a lingering void pool. Vanilla is N/A", 3)]
        public static int   primaryStepCount = 3;
        [AutoConfig("Ability Tweaks (Primary) : Drown : Pool Duration", "Expressed in seconds. Vanilla is N/A", 3)]
        public static int   primaryPoolDuration = 3;
        #region not gonna bother configging these unless asked for
        public static float maxSpread = 3; //3
        public static float maxDistance = 1000; //1000
        public static float force = 1000; //1000
        public static int   bulletCount = 1; //1
        public static float bulletRadius = 2; //2
        public static float spreadBloomValue = 0.2f; //0.2f
        #endregion

        [AutoConfig("Ability Tweaks (Primary) : Drown (Corrupted) : Damage Coefficient Per Second", "Expressed as a percentage (eg 20 is 2000%). Vanilla is 20", 20f)]
        public static float primaryCorruptDps = 20; //20
        [AutoConfig("Ability Tweaks (Primary) : Drown (Corrupted) : Tick Frequency", "Expressed in ticks per second. Vanilla is 8", 8f)]
        public static float primaryCorruptTickRate = 8; //8

        [AutoConfig("Ability Tweaks (Secondary) : Flood : Rocket Jump", "If true, the blast from Flood projectiles will inflict self-knockback aka Rocket Jump. Vanilla is false", true)]
        public static bool secondaryRocketJump = true;
        [AutoConfig("Ability Tweaks (Secondary) : Flood : Base Cooldown", "Expressed in seconds. Vanilla is 4", 7f)]
        public static float secondaryUncorruptCooldown = 7f; //4f
        [AutoConfig("Ability Tweaks (Secondary) : Flood : Blast Radius", "Expressed in meters. Vanilla is 5", 6f)]
        public static float secondaryUncorruptBlastRadius = 6f;//5f
        [AutoConfig("Ability Tweaks (Secondary) : Flood (Corrupted) : Base Cooldown", "Expressed in seconds. Vanilla is 4", 7f)]
        public static float secondaryCorruptCooldown = 7f; //4f
        [AutoConfig("Ability Tweaks (Secondary) : Flood (Corrupted) : Base Max Stock", "Vanilla is 1", 1)]
        public static int secondaryCorruptStock = 1; //1
        [AutoConfig("Ability Tweaks (Secondary) : Flood (Corrupted) : Recharge Stock", "Vanilla is 1", 1)]
        public static int secondaryCorruptRechargeStock = 1; //1
        [AutoConfig("Ability Tweaks (Secondary) : Flood (Corrupted) : Blast Radius", "Expressed in meters. Vanilla is 5", 12f)]
        public static float secondaryCorruptBlastRadius = 12f;//10f

        [AutoConfig("Ability Tweaks (Utility) : Corruption Per Cleanse", "Vanilla is 0", 3f)]
        static float corruptionPerCleanse = 3f; //0
        [AutoConfig("Ability Tweaks (Utility) : Cleanse Debuffs", "If true, both forms of Trespass (Utility) will cleanse debuffs. Vanilla is false", true)]
        static bool utilityCleanse = true; //false

        public override string survivorName => "Void Fiend";
        public override string bodyName => "VoidSurvivorBody";


        public override void Init()
        {
            base.Init();
            //GetBodyObject();
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);
                CharacterBody body = bodyObject.GetComponent<CharacterBody>();
                body.baseMaxHealth = baseMaxHealth;
                body.levelMaxHealth = baseMaxHealth * 0.3f;
                body.baseDamage = baseDamage;
                body.levelDamage = baseDamage * 0.2f;

                DoViendPrimary();
            });

            On.RoR2.HealthComponent.Heal += ViendNoHealing;
            GetStatCoefficients += ViendStatCoefficients;

            #region passive
            On.RoR2.VoidSurvivorController.OnEnable += VoidSurvivorController_OnEnable;
            //On.RoR2.Skills.VoidSurvivorSkillDef.HasRequiredCorruption += VoidSurvivorSkillDef_HasRequiredCorruption;
            #endregion


            #region secondary
            DoViendSecondary();
            #endregion

            #region utility
            if(utilityCleanse == true)
            {
                On.EntityStates.VoidSurvivor.VoidBlinkBase.OnEnter += VoidBlinkBase_OnEnter;
                LanguageAPI.Add("VOIDSURVIVOR_UTILITY_DESCRIPTION",
                    $"<style=cIsUtility>Disappear</style> into the Void, <style=cIsUtility>cleansing all debuffs</style> " +
                    $"while moving in an <style=cIsUtility>upward arc</style>. " +
                    $"Gain <style=cIsVoid>{corruptionPerCleanse}% Corruption</style> per debuff cleansed.");
            }
            #endregion

            #region special
            On.EntityStates.VoidSurvivor.Weapon.ChargeCrushBase.OnEnter += ChargeCrushBase_OnEnter;

            Addressables.LoadAssetAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.CrushCorruption_asset).Completed += (ctx) =>
            {
                SkillDef viendSpecialHeal = ctx.Result;

                viendSpecialHeal.baseMaxStock = 2;
                viendSpecialHeal.rechargeStock = 0;
                viendSpecialHeal.baseRechargeInterval = 0;
            };

            Addressables.LoadAssetAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.CrushHealth_asset).Completed += (ctx) =>
            {
                SkillDef viendSpecialHurt = ctx.Result;

                viendSpecialHurt.baseMaxStock = 1;
                viendSpecialHurt.rechargeStock = 1;
                viendSpecialHurt.stockToConsume = 0;
                viendSpecialHurt.baseRechargeInterval = 15;
            };
            #endregion
        }

        private static void DoViendSecondary()
        {
                Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorMegaBlasterBigProjectile_prefab).Completed += (ctx) =>
                {
                    viendDelayKnockback = ctx.Result.InstantiateClone("ViendDelayKnockback", true);
                    Content.AddNetworkedObjectPrefab(viendDelayKnockback);
                    ProjectileSetForceOnStart pd = viendDelayKnockback.AddComponent<ProjectileSetForceOnStart>();
                    pd.force = 1000;

                    if (viendDelayKnockback.TryGetComponent(out ProjectileImpactExplosion explode))
                    {
                        explode.blastRadius = secondaryCorruptBlastRadius;
                        explode.blastAttackerFiltering = AttackerFiltering.AlwaysHitSelf;
                        explode.explosionEffect = null;
                        explode.bonusBlastForce = Vector3.up * 500;
                        explode.canRejectForce = false;
                        explode.lifetime = 0.01f;
                        explode.explodeOnLifeTimeExpiration = true;
                        explode.blastProcCoefficient = 0;
                    }

                    Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorMegaBlasterBigProjectile_prefab).Completed += (ctx) =>
                    {
                        GameObject viendUncorruptBomb = ctx.Result;

                        if (viendUncorruptBomb.TryGetComponent(out ProjectileImpactExplosion pie))
                        {
                            pie.blastRadius = secondaryUncorruptBlastRadius;
                            if(secondaryRocketJump == true)
                            {
                                pie.childrenCount = 1;
                                pie.childrenDamageCoefficient = 0;
                                pie.childrenInheritDamageType = false;
                                pie.childrenProjectilePrefab = viendDelayKnockback;
                                pie.fireChildren = true;
                            }
                        }
                    };
                    Addressables.LoadAssetAsync<GameObject>(
                        //"RoR2/DLC1/VoidSurvivor/VoidSurvivorMegaBlasterBigProjectileCorrupted.prefab"
                        RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorMegaBlasterBigProjectileCorrupted_prefab
                    ).Completed += (ctx) =>
                    {
                        GameObject viendCorruptBomb = ctx.Result;

                        if (viendCorruptBomb.TryGetComponent(out ProjectileImpactExplosion pie2))
                        {
                            pie2.blastRadius = secondaryCorruptBlastRadius;
                            if (secondaryRocketJump == true)
                            {
                                pie2.childrenCount = 1;
                                pie2.childrenDamageCoefficient = 0;
                                pie2.childrenInheritDamageType = false;
                                pie2.childrenProjectilePrefab = viendDelayKnockback;
                                pie2.fireChildren = true;
                            }
                        }
                    };
                };

            LanguageAPI.Add("VOIDSURVIVOR_SECONDARY_DESCRIPTION",
                "<style=cIsUtility>Agile.</style> " +
                "Fire a plasma bolt for <style=cIsDamage>600% damage</style>. " +
                "Fully charge it for an explosive plasma ball instead, " +
                "dealing <style=cIsDamage>1100% damage</style>.");

            Addressables.LoadAssetAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.ChargeMegaBlaster_asset).Completed += (ctx) =>
            {
                SkillDef viendSecondary = ctx.Result;

                viendSecondary.cancelSprintingOnActivation = false;
                viendSecondary.beginSkillCooldownOnSkillEnd = true;
                viendSecondary.baseRechargeInterval = secondaryUncorruptCooldown;
                viendSecondary.keywordTokens = new string[] { "VOIDSURVIVOR_SECONDARY_UPRADE_TOOLTIP", "KEYWORD_AGILE" };
            };

            Addressables.LoadAssetAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.FireCorruptDisk_asset).Completed += (ctx) =>
            {
                SkillDef viendSecondaryCorrupt = ctx.Result;

                viendSecondaryCorrupt.cancelSprintingOnActivation = false;
                viendSecondaryCorrupt.beginSkillCooldownOnSkillEnd = true;
                viendSecondaryCorrupt.baseRechargeInterval = secondaryCorruptCooldown;
                viendSecondaryCorrupt.baseMaxStock = secondaryCorruptStock;
                viendSecondaryCorrupt.rechargeStock = secondaryCorruptRechargeStock;
            };
        }

        private void VoidBlinkBase_OnEnter(On.EntityStates.VoidSurvivor.VoidBlinkBase.orig_OnEnter orig, EntityStates.VoidSurvivor.VoidBlinkBase self)
        {
            if (NetworkServer.active)
            {
                if(self.outer.TryGetComponent(out VoidSurvivorController voidSurvivorController))
                {
                    int debuffCount = 0;
                    foreach (BuffIndex buffType in BuffCatalog.debuffBuffIndices)
                    {
                        BuffDef buffDef = BuffCatalog.GetBuffDef(buffType);
                        if (buffDef.isCooldown || buffDef.isHidden)
                            continue;
                        debuffCount += self.characterBody.GetBuffCount(buffType);
                    }
                    DotController dotController = DotController.FindDotController(self.characterBody.gameObject);
                    if (dotController)
                    {
                        for (DotController.DotIndex dotIndex = DotController.DotIndex.Bleed; dotIndex < DotController.DotIndex.Count; dotIndex++)
                        {
                            if (dotController.HasDotActive(dotIndex))
                            {
                                BuffDef buffType = DotController.GetDotDef(dotIndex).associatedBuff;
                                debuffCount += self.characterBody.GetBuffCount(buffType);
                            }
                        }
                    }

                    voidSurvivorController.AddCorruption(corruptionPerCleanse * debuffCount);
                }
            }
            orig(self);
        }

        private void DoViendPrimary()
        {
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMultiBeamDotZone_prefab, 
                CreateSloshProjectile);

            SteppedSkillDef viendComboPrimary = ScriptableObject.CreateInstance<SteppedSkillDef>();
            SurvivorTweaksPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_VoidSurvivor.FireHandBeam_asset, (viendPrimary) =>
            {
                viendComboPrimary.stepCount = primaryStepCount;
                viendComboPrimary.stepGraceDuration = 0.15f;

                viendComboPrimary.keywordTokens = viendPrimary.keywordTokens;
                viendComboPrimary.icon = viendPrimary.icon;
                viendComboPrimary.skillName = "FSTViendPrimary";
                viendComboPrimary.skillNameToken = viendPrimary.skillNameToken;
                viendComboPrimary.skillDescriptionToken = viendPrimary.skillDescriptionToken;
                viendComboPrimary.activationStateMachineName = viendPrimary.activationStateMachineName;
                viendComboPrimary.baseRechargeInterval = viendPrimary.baseRechargeInterval;
                viendComboPrimary.baseMaxStock = viendPrimary.baseMaxStock;
                viendComboPrimary.rechargeStock = viendPrimary.rechargeStock;
                viendComboPrimary.interruptPriority = viendPrimary.interruptPriority;
                viendComboPrimary.beginSkillCooldownOnSkillEnd = viendPrimary.beginSkillCooldownOnSkillEnd;
                viendComboPrimary.dontAllowPastMaxStocks = viendPrimary.dontAllowPastMaxStocks;
                viendComboPrimary.fullRestockOnAssign = viendPrimary.fullRestockOnAssign;
                viendComboPrimary.isCombatSkill = viendPrimary.isCombatSkill;
                viendComboPrimary.mustKeyPress = viendPrimary.mustKeyPress;
                viendComboPrimary.requiredStock = viendPrimary.requiredStock;
                viendComboPrimary.resetCooldownTimerOnUse = viendPrimary.resetCooldownTimerOnUse;
                viendComboPrimary.stockToConsume = viendPrimary.stockToConsume;
                viendComboPrimary.cancelSprintingOnActivation = viendPrimary.cancelSprintingOnActivation;
                viendComboPrimary.forceSprintDuringState = viendPrimary.forceSprintDuringState;
                viendComboPrimary.canceledFromSprinting = viendPrimary.canceledFromSprinting;
            });

            primary.variants[0] = new SkillFamily.Variant
            {
                skillDef = viendComboPrimary,
                unlockableDef = null,
                viewableNode = new ViewablesCatalog.Node(viendComboPrimary.skillNameToken, false, null)
            };
            Content.AddSkillDef(viendComboPrimary);
            Content.AddEntityState(typeof(FireHandBeamLight));

            SerializableEntityStateType newViendPrimaryCharge = new SerializableEntityStateType(typeof(FireHandBeamLight));
            viendComboPrimary.activationState = newViendPrimaryCharge;

            LanguageAPI.Add("VOIDSURVIVOR_PRIMARY_DESCRIPTION",
                $"Fire a <style=cIsUtility>slowing</style> long-range beam for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(primaryDamageCoefficientLight)} damage</style>. " +
                $"Every third shot leaves a lingering pool for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(primaryPoolDamageCoefficientPerSecond * primaryPoolDuration)} damage over time</style>.");
            //On.EntityStates.VoidSurvivor.Weapon.FireHandBeam.OnEnter += Idk;

            On.EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam.OnEnter += FireCorruptHandBeam_OnEnter;
            LanguageAPI.Add("VOIDSURVIVOR_PRIMARY_UPRADE_TOOLTIP", //uprade is intentional
                $"<style=cKeywordName>【Corruption Upgrade】</style><style=cSub>Transform into a " +
                $"{Tools.ConvertDecimal(primaryCorruptDps)} damage short-range beam.</style>");
        }

        private void CreateSloshProjectile(GameObject result)
        {
            viendPrimaryDamagePool = result.InstantiateClone("ViendDamagePoolProjectile", true);

            if (viendPrimaryDamagePool.TryGetComponent(out ProjectileDamage pd))
            {
                pd.damageType = new DamageTypeCombo(DamageType.SlowOnHit, DamageTypeExtended.Generic, DamageSource.Primary);
            }
            if (viendPrimaryDamagePool.TryGetComponent(out ProjectileDotZone pdz))
            {
                pdz.lifetime = primaryPoolDuration;
            }

            viendPrimaryDamagePool.transform.localScale *= 0.5f;

            Transform particles = viendPrimaryDamagePool.transform.Find("Fire, Stretched");
            if (particles)
            {
                GameObject.Destroy(particles);
            }

            Content.AddProjectilePrefab(viendPrimaryDamagePool);
        }

        private void Idk(On.EntityStates.VoidSurvivor.Weapon.FireHandBeam.orig_OnEnter orig, FireHandBeam self)
        {
            Debug.Log($"maxdistance {self.maxDistance}, force {self.force}, bulletcount {self.bulletCount}, bulletradius {self.bulletRadius}," +
                $"baseduration {self.baseDuration}, attacksoundstring {self.attackSoundString}, recoilamplitude {self.recoilAmplitude}," +
                $"spreadbloomvalue {self.spreadBloomValue}, maxspread {self.maxSpread}, muzzlename {self.muzzle}, animationlayername {self.animationLayerName}," +
                $"animationstatename {self.animationStateName}, animationplaybackrateparam {self.animationPlaybackRateParam}, trajectoryaimassistmultiplier {self.trajectoryAimAssistMultiplier}");
            orig(self);
        }

        private float ViendNoHealing(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
        {
            if (self.body.HasBuff(DLC1Content.Buffs.VoidSurvivorCorruptMode) && corruptModeAntiHeal)
            {
                amount = 0;
            }
            return orig(self, amount, procChainMask, nonRegen);
        }

        private void ChargeCrushBase_OnEnter(On.EntityStates.VoidSurvivor.Weapon.ChargeCrushBase.orig_OnEnter orig, EntityStates.VoidSurvivor.Weapon.ChargeCrushBase self)
        {
            Debug.Log(self.baseDuration);
            if(self is ChargeCrushCorruption)
            {
                self.baseDuration = 1.5f;
            }
            if(self is ChargeCrushHealth)
            {
                self.baseDuration = 0.6f;
            }
            orig(self);
        }

        private void ViendStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(DLC1Content.Buffs.VoidSurvivorCorruptMode))
            {
                args.armorAdd -= (100 - corruptModeArmor);
            }
        }

        private void FireCorruptHandBeam_OnEnter(On.EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam.orig_OnEnter orig, EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam self)
        {
            self.tickRate = primaryCorruptTickRate;
            //self.damageCoefficientPerSecond = primaryCorruptDps / (1 / (1 + RiskierRainPlugin.kitSlowAspdReduction));
            orig(self);
        }

        private bool VoidSurvivorSkillDef_HasRequiredCorruption(On.RoR2.Skills.VoidSurvivorSkillDef.orig_HasRequiredCorruption orig, RoR2.Skills.VoidSurvivorSkillDef self, GenericSkill skillSlot)
        {
            VoidSurvivorSkillDef.InstanceData instanceData = (VoidSurvivorSkillDef.InstanceData)skillSlot.skillInstanceData;
            VoidSurvivorController vsc = instanceData.voidSurvivorController;
            if (vsc)
            {
                float guh = ViendTweaks.maxCorruption - Mathf.Min(self.maximumCorruption, ViendTweaks.maxCorruption);
                float a = vsc.maxCorruption - vsc.minimumCorruption;
                float b = self.minimumCorruption - guh;
                if (a > b)
                    return true;
                return vsc.corruption >= self.minimumCorruption && vsc.corruption < self.maximumCorruption;
            }
            return false;
            //return orig(self, skillSlot);
        }

        private void VoidSurvivorController_OnEnable(On.RoR2.VoidSurvivorController.orig_OnEnable orig, RoR2.VoidSurvivorController self)
        {
            self.minimumCorruptionPerVoidItem = ViendTweaks.minimumCorruptionPerVoidItem;
            self.corruptionForFullDamage = ViendTweaks.corruptionForFullDamage;
            self.corruptionForFullHeal = ViendTweaks.corruptionForFullHeal;
            self.corruptionFractionPerSecondWhileCorrupted = ViendTweaks.corruptionFractionPerSecondWhileCorrupted;
            self.corruptionPerSecondInCombat = ViendTweaks.corruptionPerSecondInCombat;
            self.corruptionPerSecondOutOfCombat = ViendTweaks.corruptionPerSecondOutOfCombat;
            self.corruptionPerCrit = ViendTweaks.corruptionPerCrit;
            self.maxCorruption = ViendTweaks.maxCorruption;
            orig(self);
        }
    }
}