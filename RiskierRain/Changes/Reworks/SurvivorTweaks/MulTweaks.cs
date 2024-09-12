using RiskierRain.CoreModules;
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

namespace RiskierRain.SurvivorTweaks
{
    class MulTweaks : SurvivorTweakModule
    {
        float nailSpreadCoefficient = 1.2f;

        GameObject scrapProjectile = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/ToolbotGrenadeLauncherProjectile");
        public static bool useScrapGravity = true;
        public static float scrapSpeed = 150; //100
        public static float scrapCooldown = 3; //1.5f
        public static float scrapDamage = 3.6f; //3.6f
        public static float scrapDuration = 0.2f; //0.3f

        public static float retoolDuration = 0.5f;

        public override string survivorName => "MULT";

        public override string bodyName => "TOOLBOTBODY";

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            body.baseMoveSpeed = 8;
            body.acceleration = 25;

            On.EntityStates.Toolbot.BaseNailgunState.FireBullet += FuckTheCorkscrewPattern;
            On.EntityStates.Toolbot.FireNailgun.OnExit += NewNailgunBurst;
            On.EntityStates.Toolbot.NailgunSpinDown.GetBaseDuration += FixWinddownDuration;
            On.EntityStates.Toolbot.NailgunSpinDown.FixedUpdate += RemoveNailgunBurst;
            ToolbotWeaponSkillDef nailGun = (ToolbotWeaponSkillDef)primary.variants[0].skillDef;
            AnimationCurve curve = nailGun.crosshairSpreadCurve;
            nailGun.beginSkillCooldownOnSkillEnd = true;

            ToolbotWeaponSkillDef rebar = (ToolbotWeaponSkillDef)primary.variants[1].skillDef;
            rebar.crosshairSpreadCurve = curve;

            On.EntityStates.Toolbot.FireGrenadeLauncher.OnEnter += ScrapBuff;
            ToolbotWeaponSkillDef scrapGun = (ToolbotWeaponSkillDef)primary.variants[2].skillDef;
            scrapGun.resetCooldownTimerOnUse = false;
            scrapGun.baseRechargeInterval = scrapCooldown;

            //On.EntityStates.Toolbot.FireBuzzsaw.FixedUpdate += SawFixedUpdate;
            ToolbotWeaponSkillDef saw = (ToolbotWeaponSkillDef)primary.variants[3].skillDef;
            saw.crosshairSpreadCurve = curve;
            saw.crosshairPrefab.GetComponent<CrosshairController>().maxSpreadAngle *= 4;
            saw.canceledFromSprinting = true;
            saw.beginSkillCooldownOnSkillEnd = true;

            if (useScrapGravity)
            {
                ProjectileSimple scrapPs = scrapProjectile.GetComponent<ProjectileSimple>();
                scrapPs.desiredForwardSpeed = scrapSpeed;
                Rigidbody scrapRb = scrapProjectile.GetComponent<Rigidbody>();
                scrapRb.useGravity = true;
                AntiGravityForce scrapAntiGravity = scrapProjectile.AddComponent<AntiGravityForce>();
                scrapAntiGravity.rb = scrapRb;
                scrapAntiGravity.antiGravityCoefficient = 0.3f;
            }

            secondary.variants[0].skillDef.canceledFromSprinting = false;

            On.EntityStates.Toolbot.ToolbotStanceSwap.OnEnter += RetoolBuff;
            SkillDef retool = special.variants[0].skillDef;
            retool.baseRechargeInterval = retoolDuration * 4;

            On.EntityStates.Toolbot.ToolbotDualWieldBase.OnEnter += PowerModeNerf;
            On.EntityStates.Toolbot.ToolbotDualWieldBase.OnExit += UndoPowerMode;
        }


        private void SawFixedUpdate(On.EntityStates.Toolbot.FireBuzzsaw.orig_FixedUpdate orig, FireBuzzsaw self)
        {
            orig(self);
            if (self.characterBody.isSprinting && self.skillDef == self.activatorSkillSlot.skillDef)
            {
                self.activatorSkillSlot.rechargeStopwatch = 0.5f;
                self.outer.SetNextStateToMain();
            }
        }

        private void UndoPowerMode(On.EntityStates.Toolbot.ToolbotDualWieldBase.orig_OnExit orig, ToolbotDualWieldBase self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody && Tools.isLoaded("com.Borbo.BORBO"))
            {
                if (ToolbotDualWieldBase.penaltyBuff && self.applyPenaltyBuff)
                {
                    self.characterBody.RemoveBuff(CoreModules.Assets.aspdPenaltyDebuff);
                }
            }
        }

        private void PowerModeNerf(On.EntityStates.Toolbot.ToolbotDualWieldBase.orig_OnEnter orig, ToolbotDualWieldBase self)
        {
            if (NetworkServer.active && self.characterBody && Tools.isLoaded("com.Borbo.BORBO"))
            {
                if (ToolbotDualWieldBase.penaltyBuff && self.applyPenaltyBuff)
                {
                    self.characterBody.AddBuff(CoreModules.Assets.aspdPenaltyDebuff);
                }
            }
            orig(self);
        }

        private float FixWinddownDuration(On.EntityStates.Toolbot.NailgunSpinDown.orig_GetBaseDuration orig, NailgunSpinDown self)
        {
            return NailgunSpinDown.baseDuration +
                ((float)NailgunFinalBurst.finalBurstBulletCount * FireNailgun.baseRefireInterval * NailgunFinalBurst.burstTimeCostCoefficient);
        }

        private void NewNailgunBurst(On.EntityStates.Toolbot.FireNailgun.orig_OnExit orig, FireNailgun self)
        {
            orig(self);

            if (self.characterBody)
            {
                self.characterBody.SetSpreadBloom(1f, false);
            }
            Ray aimRay = self.GetAimRay();
            self.FireBullet(self.GetAimRay(), NailgunFinalBurst.finalBurstBulletCount, BaseNailgunState.spreadPitchScale, BaseNailgunState.spreadYawScale);
            if (!self.isInDualWield)
            {
                self.PlayAnimation("Gesture, Additive", "FireGrenadeLauncher", "FireGrenadeLauncher.playbackRate", 0.45f / self.attackSpeedStat);
            }
            else
            {
                BaseToolbotPrimarySkillStateMethods.PlayGenericFireAnim<FireNailgun>(self, self.gameObject, base.skillLocator, 0.45f / self.attackSpeedStat);
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

        private void RemoveNailgunBurst(On.EntityStates.Toolbot.NailgunSpinDown.orig_FixedUpdate orig, NailgunSpinDown self)
        {
            self.fixedAge += Time.fixedDeltaTime;
            if (self.fixedAge >= self.duration && self.isAuthority)
            {
                self.outer.SetNextStateToMain();
            }
        }

        private void ScrapBuff(On.EntityStates.Toolbot.FireGrenadeLauncher.orig_OnEnter orig, FireGrenadeLauncher self)
        {
            self.damageCoefficient = scrapDamage;
            //Debug.Log(self.baseDuration);
            self.baseDuration = scrapDuration;
            orig(self);
        }

        private void RetoolBuff(On.EntityStates.Toolbot.ToolbotStanceSwap.orig_OnEnter orig, EntityStates.Toolbot.ToolbotStanceSwap self)
        {
            self.baseDuration = 0.75f;
            self.characterBody.AddTimedBuffAuthority(ToolbotDualWieldBase.bonusBuff.buffIndex, self.baseDuration / self.attackSpeedStat);

            orig(self);
        }

        private void FuckTheCorkscrewPattern(On.EntityStates.Toolbot.BaseNailgunState.orig_FireBullet orig, EntityStates.Toolbot.BaseNailgunState self, 
            Ray aimRay, int bulletCount, float spreadPitchScale, float spreadYawScale)
        {
            orig(self, self.GetAimRay(), bulletCount, 1f, 1f);
        }
    }
}
