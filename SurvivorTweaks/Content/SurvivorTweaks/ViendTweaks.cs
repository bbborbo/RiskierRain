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

namespace SurvivorTweaks.SurvivorTweaks
{
    class ViendTweaks : SurvivorTweakBase<ViendTweaks>
    {
        public static GameObject viendPrimaryDamagePool;
        public static GameObject viendDelayKnockback;
        static float corruptModeArmor = 100; //100

        static float corruptionPerCleanse = 3; //0
        static float minimumCorruptionPerVoidItem = 2; //2
        static float corruptionForFullDamage = 50; //50
        static float corruptionForFullHeal = -50; //-100
        static float corruptionFractionPerSecondWhileCorrupted = -0.04f; //aka 25s; -0.06666667f aka 15s
        static float corruptionPerSecondInCombat = 1.5f; //aka 66.6s; 3 aka 33.3s
        static float corruptionPerSecondOutOfCombat = 1.5f; //3
        static float corruptionPerCrit = 0; //2
        static float maxCorruption = 100; //100

        public static float primaryUnchargedDamage = 0.9f;
        public static float primaryChargedDamage = 4.8f;
        public static int primaryStepCount = 3;

        public static float primaryCorruptDps = 20; //20
        public static float primaryCorruptTickRate = 8; //8

        public static float secondaryUncorruptCooldown = 5f; //4f
        public static float secondaryCorruptCooldown = 7f; //4f
        public static int secondaryCorruptStock = 2; //1
        public static int secondaryCorruptRechargeStock = 2; //1
        public static float secondaryUncorruptBlastRadius = 10f;//5f
        public static float secondaryCorruptBlastRadius = 10f;//10f

        public override string survivorName => "Void Fiend";
        public override string bodyName => "VoidSurvivorBody";


        public override void Init()
        {
            //GetBodyObject();
            bodyObject = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorBody_prefab).WaitForCompletion();
            GetSkillsFromBodyObject(bodyObject);
            //CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            //body.
            On.RoR2.HealthComponent.Heal += ViendNoHealing;
            GetStatCoefficients += ViendStatCoefficients;

            #region passive
            On.RoR2.VoidSurvivorController.OnEnable += VoidSurvivorController_OnEnable;
            //On.RoR2.Skills.VoidSurvivorSkillDef.HasRequiredCorruption += VoidSurvivorSkillDef_HasRequiredCorruption;
            #endregion

            DoViendPrimary();

            #region secondary
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorMegaBlasterBigProjectile_prefab).Completed += (ctx) =>
            {
                viendDelayKnockback = ctx.Result.InstantiateClone("ViendDelayKnockback", true);
                Content.AddNetworkedObjectPrefab(viendDelayKnockback);
                if (viendDelayKnockback.TryGetComponent(out ProjectileDamage pd))
                {
                    pd.force = 100;
                }
                if (viendDelayKnockback.TryGetComponent(out ProjectileImpactExplosion explode))
                {
                    explode.blastAttackerFiltering = AttackerFiltering.AlwaysHitSelf;
                    explode.explosionEffect = null;
                    explode.bonusBlastForce = Vector3.up * 200;
                    explode.canRejectForce = false;
                    explode.lifetime = 0.01f;
                    explode.explodeOnLifeTimeExpiration = true;
                }

                Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorMegaBlasterBigProjectile_prefab).Completed += (ctx) =>
                {
                    GameObject viendUncorruptBomb = ctx.Result;

                    if (viendUncorruptBomb.TryGetComponent(out ProjectileImpactExplosion pie))
                    {
                        pie.childrenCount = 1;
                        pie.childrenDamageCoefficient = 0;
                        pie.childrenInheritDamageType = true;
                        pie.childrenProjectilePrefab = viendDelayKnockback;
                        pie.fireChildren = true;
                    }
                };
                Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorMegaBlasterBigProjectileCorrupted_prefab).Completed += (ctx) =>
                {
                    GameObject viendCorruptBomb = ctx.Result;

                    if (viendCorruptBomb.TryGetComponent(out ProjectileImpactExplosion pie))
                    {
                        pie.childrenCount = 1;
                        pie.childrenDamageCoefficient = 0;
                        pie.childrenInheritDamageType = true;
                        pie.childrenProjectilePrefab = viendDelayKnockback;
                        pie.fireChildren = true;
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
            #endregion

            #region utility
            On.EntityStates.VoidSurvivor.VoidBlinkBase.OnEnter += VoidBlinkBase_OnEnter;
            LanguageAPI.Add("VOIDSURVIVOR_UTILITY_DESCRIPTION",
                $"<style=cIsUtility>Disappear</style> into the Void, <style=cIsUtility>cleansing all debuffs</style> " +
                $"while moving in an <style=cIsUtility>upward arc</style>. " +
                $"Gain <style=cIsVoid>{corruptionPerCleanse}% Corruption</style> per debuff cleansed.");
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

        private void VoidBlinkBase_OnEnter(On.EntityStates.VoidSurvivor.VoidBlinkBase.orig_OnEnter orig, EntityStates.VoidSurvivor.VoidBlinkBase self)
        {
            if (NetworkServer.active)
            {
                if(self.outer.TryGetComponent(out VoidSurvivorController voidSurvivorController))
                {
                    int debuffCount = 0;
                    foreach (BuffIndex buffType in BuffCatalog.debuffBuffIndices)
                    {
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
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidRaidCrab.VoidRaidCrabMultiBeamDotZone_prefab).Completed += (ctx) => CreateSloshProjectile(ctx.Result);

            SkillDef viendPrimary = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC1/VoidSurvivor/FireHandBeam.asset").WaitForCompletion();
            SteppedSkillDef viendComboPrimary = ScriptableObject.CreateInstance<SteppedSkillDef>();

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
                $"<style=cIsDamage>{Tools.ConvertDecimal(FireHandBeamLight.damageCoefficientLight)}-{Tools.ConvertDecimal(FireHandBeamLight.damageCoefficientHeavy)} damage</style>.");
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
                pdz.lifetime = 3;
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
            if (self.body.HasBuff(DLC1Content.Buffs.VoidSurvivorCorruptMode))
                amount = 0;
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