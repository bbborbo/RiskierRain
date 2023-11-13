using RiskierRainContent.Skills;
using EntityStates;
using EntityStates.Captain.Weapon;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RiskierRainContent.EntityState.Captain
{
    class VulcanSlug : BaseSkillState
    {
        public float baseChargeDuration = 1.5f;
        public float baseWinddownDuration = 0.25f;

        public float minFuse = 0.1f;
        public float maxFuse = 0.7f;

        public static float damageCoefficient = 5f;
        public static float procCoefficient = 1f;
        public float force = 1250;
        public float selfForce = 750;

        private const float minChargeDuration = 0.2f;
        private float stopwatch;
        private float windDownDuration;
        private float chargeDuration;
        private bool hasFiredBomb;

        private string muzzleString;
        private Transform muzzleTransform;

        private GameObject chargeEffectInstance;

        public static GameObject aoeEffect = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/omnieffect/OmniImpactVFXLightningMage");
        public static GameObject muzzleflashEffect = new FireCaptainShotgun().muzzleFlashPrefab;// muzzleflashEffect;
        private GameObject chargeupVfxGameObject = new ChargeCaptainShotgun().chargeupVfxGameObject;
        private uint enterSoundID;

        public override void OnEnter()
        {
            base.OnEnter();

            this.windDownDuration = this.baseWinddownDuration / this.attackSpeedStat;
            this.chargeDuration = this.baseChargeDuration / this.attackSpeedStat;

            base.PlayCrossfade("Gesture, Override", "ChargeCaptainShotgun", "ChargeCaptainShotgun.playbackRate", this.chargeDuration, 0.1f);
            base.PlayCrossfade("Gesture, Additive", "ChargeCaptainShotgun", "ChargeCaptainShotgun.playbackRate", this.chargeDuration, 0.1f);
            this.muzzleTransform = base.FindModelChild(ChargeCaptainShotgun.muzzleName);
            if (this.muzzleTransform)
            {
                this.chargeupVfxGameObject = UnityEngine.Object.Instantiate<GameObject>(ChargeCaptainShotgun.chargeupVfxPrefab, this.muzzleTransform);
                this.chargeupVfxGameObject.GetComponent<ScaleParticleSystemDuration>().newDuration = this.chargeDuration;
            }
            this.enterSoundID = Util.PlayAttackSpeedSound(ChargeCaptainShotgun.enterSoundString, base.gameObject, this.attackSpeedStat);
            Util.PlaySound(ChargeCaptainShotgun.playChargeSoundString, base.gameObject);
        }

        public override void Update()
        {
            base.Update();
            base.characterBody.SetSpreadBloom(base.age / this.chargeDuration, true);
            return;

            //base.characterBody.SetSpreadBloom(Util.Remap(this.GetChargeProgressSmooth(), 0f, 1f, this.minRadius, this.maxRadius), true);
        }
        private float GetChargeProgressSmooth()
        {
            return Mathf.Clamp01(this.stopwatch / this.chargeDuration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            base.characterBody.SetAimTimer(1f);
            this.stopwatch += Time.fixedDeltaTime;

            if (!this.hasFiredBomb && this.stopwatch >= minChargeDuration && 
                (this.stopwatch >= this.chargeDuration || !IsKeyDownAuthority()))
            {
                this.FireBomb();
            }
            if (this.stopwatch >= this.windDownDuration && this.hasFiredBomb && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        private void FireBomb()
        {
            this.hasFiredBomb = true;
            base.PlayAnimation("Gesture, Additive", "FireCaptainShotgun");
            base.PlayAnimation("Gesture, Override", "FireCaptainShotgun");

            string fireSoundString =
                (base.characterBody.spreadBloomAngle <= FireCaptainShotgun.tightSoundSwitchThreshold) 
                ? FireCaptainShotgun.tightSoundString : FireCaptainShotgun.wideSoundString;
            Util.PlaySound(fireSoundString, base.gameObject);
            if (this.muzzleTransform)
            {
                EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, this.muzzleString, false);
            }

            Ray aimRay = base.GetAimRay();
            if (base.isAuthority)
            {
                FireProjectileInfo fpi = new FireProjectileInfo
                {
                    projectilePrefab = VulcanSlugSkill.vulcanSlugPrefab,
                    position = aimRay.origin,
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    owner = base.gameObject,
                    damage = damageStat * damageCoefficient,
                    force = force,
                    crit = Util.CheckRoll(this.critStat, base.characterBody.master),
                    fuseOverride = Util.Remap(this.GetChargeProgressSmooth(), 0f, 1f, this.minFuse, this.maxFuse),
                    useFuseOverride = true
                };

                ProjectileManager.instance.FireProjectile(fpi);

                if (base.characterMotor)
                {
                    base.characterMotor.ApplyForce(aimRay.direction * -this.selfForce, false, false);
                }
            }
            base.AddRecoil(-1f * FireTazer.recoilAmplitude, -1.5f * FireTazer.recoilAmplitude, -0.25f * FireTazer.recoilAmplitude, 0.25f * FireTazer.recoilAmplitude);

            this.stopwatch = 0f;
        }

        public override void OnExit()
        {
            if (!this.outer.destroying && !this.hasFiredBomb)
            {
                base.PlayAnimation("Gesture, Additive", "Empty");

                GameObject obj = base.outer.gameObject;
            }
            if (this.chargeupVfxGameObject)
            {
                Destroy(this.chargeupVfxGameObject);
            }

            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}
