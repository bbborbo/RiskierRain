using SurvivorTweaks.Modules;
using EntityStates.Toolbot;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using static RainrotSharedUtils.Styling;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace SurvivorTweaks.SurvivorTweaks
{
    class MulTweaks : SurvivorTweakBase<MulTweaks>
    {
        [AutoConfig(": Base Damage Stat", "Scales 20% per level. Vanilla is 12", 12)]
        public static float toolbotBaseDamage = 12f; //12f
        [AutoConfig(": Acceleration Stat", "Vanilla is 30", 50)]
        public static float toolbotAcceleration = 50f; //30f

        [AutoConfig("Primary (Rebar Puncher) : Base Duration", "Expressed in seconds. Vanilla is 1.1", 0.9f)]
        public static float spearBaseDuration = 0.9f; //1.1f
        [AutoConfig("Primary (Rebar Puncher) : Base Recharge Duration", "Expressed in seconds. Vanilla is 0.7", 0.45f)]
        public static float spearBaseRechargeDuration = 0.45f; //0.7f

        [AutoConfig("Primary (Scrap Launcher) : Projectile Velocity", "Expressed in meters per second. Vanilla is 100", 150f)]
        public static float scrapSpeed = 150f; //100
        [AutoConfig("Primary (Scrap Launcher) : Base Cooldown", "Expressed in seconds. Vanilla is 1.5", 3f)]
        public static float scrapCooldown = 3f; //1.5f
        [AutoConfig("Primary (Scrap Launcher) : Damage Coefficient", "Expressed as a percentage (eg 3.6 is 360%). Vanilla is 3.6", 3.6f)]
        public static float scrapDamage = 3.6f; //3.6f
        [AutoConfig("Primary (Scrap Launcher) : Base Attack Duration", "Expressed in seconds. Vanilla is 0.3", 0.2f)]
        public static float scrapDuration = 0.2f; //0.3f
        [AutoConfig("Primary (Scrap Launcher) : Reset Cooldown On Activation", "Set to true to reset its cooldown when activated. Vanilla is true", false)]
        public static bool scrapResetCooldown = false;
        [AutoConfig("Primary (Scrap Launcher) : Remove Blast Falloff", "Set to true to remove falloff from its impact explosion. Vanilla is false", true)]
        public static bool removeScrapFalloff = true;
        [AutoConfig("Primary (Scrap Launcher) : Projectile Anti-Gravity", "How much to reduce Scrap Launcher gravity by. A value of 1 means it is unaffected by gravity. Vanilla is 1", 0.3f)]
        public static float scrapGravity = 0.3f;

        [AutoConfig("Special (Power Mode) : Remove Accuracy Exploit", "Set to TRUE to fix the exploit that allows Nailgun to be used with perfect accuracy.", true)]
        public static bool removeAccuracyExploit = true;
        [AutoConfig("Special (Power Mode) : Remove Luminous Shot Exploit", "Set to TRUE to fix the exploit that allows off hand primaries to gain Luminous Shot charges.", true)]
        public static bool removeLuminousExploit = true;
        [AutoConfig("Special (Power Mode) : Jump Power Penalty", "Works like movement speed penalties. Vanilla is 0", 0.5f)]
        public static float powerModeJumpPenalty = 0.5f;

        public static float retoolDuration = 0.5f;

        public override string survivorName => "MULT";

        public override string bodyName => "TOOLBOTBODY";

        public override void Init()
        {
            base.Init();
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Toolbot.ToolbotBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);
                CharacterBody body = bodyObject.GetComponent<CharacterBody>();
                body.baseMoveSpeed = 8;
                body.acceleration = toolbotAcceleration;
                body.baseDamage = toolbotBaseDamage;
                body.levelDamage = toolbotBaseDamage * 0.2f;

                Transform modelTransform = body.GetComponent<ModelLocator>()?.modelTransform;
                if (modelTransform == null)
                    return;
                List<HitBoxGroup> hitBoxGroups = GetComponentsCache<HitBoxGroup>.GetGameObjectComponents(modelTransform.gameObject);
                for (int i = 0; i < hitBoxGroups.Count; i++)
                {
                    if (hitBoxGroups[i].groupName == "Buzzsaw")
                    {
                        //21,21,12
                        hitBoxGroups[i].hitBoxes[0].transform.localScale = new Vector3(35, 35, 27);
                    }
                    if (hitBoxGroups[i].groupName == "BuzzsawL")
                    {
                        //12,21,21
                        hitBoxGroups[i].hitBoxes[0].transform.localScale = new Vector3(27, 35, 35);
                    }
                    if (hitBoxGroups[i].groupName == "BuzzsawR")
                    {
                        //12,21,21
                        hitBoxGroups[i].hitBoxes[0].transform.localScale = new Vector3(27, 35, 35);
                    }
                }
                GetComponentsCache<HitBoxGroup>.ReturnBuffer(hitBoxGroups);
            });

            ChangePrimaries();
            ChangeSpecials();
        }

        #region primary
        public static void ChangePrimaries()
        {
            #region nailgun
            On.EntityStates.Toolbot.BaseNailgunState.FireBullet += FuckTheCorkscrewPattern;
            On.EntityStates.Toolbot.FireNailgun.OnExit += NewNailgunBurst;
            On.EntityStates.Toolbot.NailgunSpinDown.GetBaseDuration += FixWinddownDuration;
            On.EntityStates.Toolbot.NailgunSpinDown.FixedUpdate += RemoveNailgunBurst;
            IL.EntityStates.Toolbot.FireNailgun.FixedUpdate += FixNailgunSpreadBloom;
            IL.EntityStates.Toolbot.BaseNailgunState.FireBullet += FixNailgunSpreadBloom;

            SurvivorTweaksPlugin.LoadAsync<ToolbotWeaponSkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Toolbot.ToolbotBodyFireNailgun_asset, (nailgun) =>
            {
                nailgun.beginSkillCooldownOnSkillEnd = true;
                nailgun.autoHandleLuminousShot = !removeLuminousExploit;
            });
            #endregion

            #region rebar
            On.EntityStates.Toolbot.CooldownSpear.OnEnter += RechargeRebar;
            On.EntityStates.Toolbot.FireSpear.OnEnter += FireRebar;

            SurvivorTweaksPlugin.LoadAsync<ToolbotWeaponSkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Toolbot.ToolbotBodyFireSpear_asset, (rebar) =>
            {
                rebar.autoHandleLuminousShot = !removeLuminousExploit;
            });
            #endregion

            #region scrap launcher scrapgun
            On.EntityStates.Toolbot.FireGrenadeLauncher.OnEnter += ScrapBuff;

            SurvivorTweaksPlugin.LoadAsync<ToolbotWeaponSkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Toolbot.ToolbotBodyFireGrenadeLauncher_asset, (scrapGun) =>
            {
                scrapGun.autoHandleLuminousShot = !removeLuminousExploit;
                scrapGun.resetCooldownTimerOnUse = scrapResetCooldown;
                scrapGun.attackSpeedBuffsRestockSpeed = true;
                scrapGun.baseRechargeInterval = scrapCooldown;
            });

            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Toolbot.ToolbotGrenadeLauncherProjectile_prefab, (scrapGrenade) =>
            {
                if (removeScrapFalloff && scrapGrenade.TryGetComponent(out ProjectileImpactExplosion pie))
                {
                    if (removeScrapFalloff == true)
                        pie.falloffModel = BlastAttack.FalloffModel.None;
                }
                if (scrapSpeed != 100 && scrapGrenade.TryGetComponent(out ProjectileSimple ps))
                {
                    ps.desiredForwardSpeed = scrapSpeed;
                }
                if (scrapGravity != 1 && scrapGrenade.TryGetComponent(out Rigidbody rb))
                {
                    rb.useGravity = true;

                    AntiGravityForce scrapAntiGravity = scrapGrenade.GetComponent<AntiGravityForce>();
                    if(scrapAntiGravity == null)
                        scrapAntiGravity = scrapGrenade.AddComponent<AntiGravityForce>();
                    scrapAntiGravity.rb = rb;
                    scrapAntiGravity.antiGravityCoefficient = scrapGravity;
                }
            });
            #endregion

            #region saw
            SurvivorTweaksPlugin.LoadAsync<ToolbotWeaponSkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Toolbot.ToolbotBodyFireBuzzsaw_asset, (saw) =>
            {
                saw.autoHandleLuminousShot = !removeLuminousExploit;
                saw.cancelSprintingOnActivation = false;
                saw.beginSkillCooldownOnSkillEnd = true;
                saw.keywordTokens = new string[] { "KEYWORD_AGILE" };
                LanguageAPI.Add("TOOLBOT_PRIMARY_ALT3_DESCRIPTION", $"{UtilityColor("Agile")}. Saw nearby enemies for <style=cIsDamage>1000% damage per second</style>.");
            });
            #endregion
        }

        private static void FixNailgunSpreadBloom(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_spreadBloomAngle")
                );
            if (b1 == false)
            {
                RainrotSharedUtils.SharedUtilsPlugin.DebugBreakpoint(nameof(FixNailgunSpreadBloom));
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, FireNailgun, float>>((angle, self) =>
            {
                if (removeAccuracyExploit == false)
                    return angle;
                if (self.activatorSkillSlot != null)
                {
                    ToolbotWeaponSkillDef skillDef = self.activatorSkillSlot.skillDef as ToolbotWeaponSkillDef;
                    if(skillDef != null)
                    {
                        return skillDef.crosshairSpreadCurve.Evaluate(self.characterBody.spreadBloomInternal);
                    }
                }
                return angle;
            });
        }

        private static void RechargeRebar(On.EntityStates.Toolbot.CooldownSpear.orig_OnEnter orig, CooldownSpear self)
        {
            CooldownSpear.baseDuration = spearBaseRechargeDuration;
            orig(self);
        }

        private static void FireRebar(On.EntityStates.Toolbot.FireSpear.orig_OnEnter orig, FireSpear self)
        {
            self.baseDuration = spearBaseDuration;
            orig(self);
        }

        private static float FixWinddownDuration(On.EntityStates.Toolbot.NailgunSpinDown.orig_GetBaseDuration orig, NailgunSpinDown self)
        {
            return NailgunSpinDown.baseDuration +
                ((float)NailgunFinalBurst.finalBurstBulletCount * FireNailgun.baseRefireInterval * NailgunFinalBurst.burstTimeCostCoefficient);
        }

        private static void NewNailgunBurst(On.EntityStates.Toolbot.FireNailgun.orig_OnExit orig, FireNailgun self)
        {
            orig(self);

            if (self.characterBody)
            {
                self.characterBody.SetSpreadBloom(1f, false);
            }
            if (self.characterBody.HasBuff(DLC2Content.Buffs.DisableAllSkills.buffIndex))
            {
                return;
            }
            Ray aimRay = self.GetAimRay();
            TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, BaseNailgunState.maxDistance, self.gameObject, 1f);
            self.FireBullet(self.GetAimRay(), NailgunFinalBurst.finalBurstBulletCount, BaseNailgunState.spreadPitchScale, BaseNailgunState.spreadYawScale);
            if (!self.isInDualWield)
            {
                self.PlayAnimation("Gesture, Additive", NailgunFinalBurst.FireGrenadeLauncherStateHash, NailgunFinalBurst.FireGrenadeLauncherParamHash, 0.45f / self.attackSpeedStat);
            }
            else
            {
                BaseToolbotPrimarySkillStateMethods.PlayGenericFireAnim<FireNailgun>(self, self.gameObject, self.skillLocator, 0.45f / self.attackSpeedStat);
            }
            Util.PlaySound(NailgunFinalBurst.burstSound, self.gameObject);
            if (self.isAuthority)
            {
                float num = NailgunFinalBurst.selfForce * (self.characterMotor.isGrounded ? 0.5f : 1f) * self.characterMotor.mass;
                self.characterMotor.ApplyForce(aimRay.direction * -num, false, false);
            }
            Util.PlaySound(BaseNailgunState.fireSoundString, self.gameObject);
            Util.PlaySound(BaseNailgunState.fireSoundString, self.gameObject);
            Util.PlaySound(BaseNailgunState.fireSoundString, self.gameObject);
        }

        private static void RemoveNailgunBurst(On.EntityStates.Toolbot.NailgunSpinDown.orig_FixedUpdate orig, NailgunSpinDown self)
        {
            self.fixedAge += Time.fixedDeltaTime;
            if (self.fixedAge >= self.duration && self.isAuthority)
            {
                self.outer.SetNextStateToMain();
            }
        }

        private static void FuckTheCorkscrewPattern(On.EntityStates.Toolbot.BaseNailgunState.orig_FireBullet orig, EntityStates.Toolbot.BaseNailgunState self,
            Ray aimRay, int bulletCount, float spreadPitchScale, float spreadYawScale)
        {
            Ray ray = self.GetAimRay();
            TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, BaseNailgunState.maxDistance, self.gameObject, 1f);
            orig(self, ray, bulletCount, 1f, 1f);
        }

        private static void ScrapBuff(On.EntityStates.Toolbot.FireGrenadeLauncher.orig_OnEnter orig, FireGrenadeLauncher self)
        {
            self.damageCoefficient = scrapDamage;
            //Debug.Log(self.baseDuration);
            self.baseDuration = scrapDuration;
            orig(self);
        }
        #endregion

        private void ChangeSpecials()
        {
            On.EntityStates.Toolbot.ToolbotStanceSwap.OnEnter += RetoolBuff;

            SurvivorTweaksPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Toolbot.ToolbotBodySwap_asset, (retool) =>
            {
                retool.baseRechargeInterval = retoolDuration;
            });
            SkillDef retool = special.variants[0].skillDef;

            On.EntityStates.Toolbot.ToolbotDualWieldBase.OnEnter += PowerModeJumpPowerSet;
            On.EntityStates.Toolbot.ToolbotDualWieldBase.OnExit += PowerModeJumpPowerUnset;
        }

        private void PowerModeJumpPowerUnset(On.EntityStates.Toolbot.ToolbotDualWieldBase.orig_OnExit orig, ToolbotDualWieldBase self)
        {
            orig(self);
            if (self.characterMotor)
            {
                self.characterMotor.jumpPowerPenaltyCoefficient = 1f;
            }
        }

        private void PowerModeJumpPowerSet(On.EntityStates.Toolbot.ToolbotDualWieldBase.orig_OnEnter orig, ToolbotDualWieldBase self)
        {
            orig(self);
            if (self.characterMotor)
            {
                self.characterMotor.jumpPowerPenaltyCoefficient = 1f / (1 + powerModeJumpPenalty);
            }
        }

        private static void RetoolBuff(On.EntityStates.Toolbot.ToolbotStanceSwap.orig_OnEnter orig, EntityStates.Toolbot.ToolbotStanceSwap self)
        {
            self.baseDuration = retoolDuration;
            self.characterBody.AddTimedBuffAuthority(ToolbotDualWieldBase.bonusBuff.buffIndex, self.baseDuration / self.attackSpeedStat);

            orig(self);
        }
    }
}
