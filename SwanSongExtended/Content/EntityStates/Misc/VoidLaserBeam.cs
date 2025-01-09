using EntityStates;
using EntityStates.Mage.Weapon;
using SwanSongExtended.Skills;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.States
{
    class VoidLaserBeam : BaseSkillState
    {
        public GameObject muzzleflashEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/MuzzleflashMageLightningLarge");

        public float procCoefficient => VoidLaserTurbineSkill.procCoefficient;

        public static float damageCoefficient => VoidLaserTurbineSkill.damageCoefficient;

        public float force => VoidLaserTurbineSkill.force;
        public float selfForce => VoidLaserTurbineSkill.selfForce;

        public float baseDuration => VoidLaserTurbineSkill.baseDuration;

        public string attackSoundString = FireLaserbolt.attackString;
        public string attackSoundString2 = "Play_mage_m2_impact";

        private float duration;

        private bool hasFiredGauntlet = false;

        private string muzzleString = "Head";

        private Transform muzzleTransform;

        private Animator animator;

        private ChildLocator childLocator;

        public static float baseRecoilAmplitude = 10f;
        public static float maxRange = 1000;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            Util.PlaySound(this.attackSoundString, base.gameObject);
            Util.PlaySound(this.attackSoundString2, base.gameObject);
            float recoil = baseRecoilAmplitude / this.attackSpeedStat;
            base.AddRecoil(-recoil, -2f * recoil, -recoil, recoil);
            base.characterBody.SetAimTimer(2f);
            this.animator = base.GetModelAnimator();
            if (this.animator)
            {
                this.childLocator = this.animator.GetComponent<ChildLocator>();
            }
            FireLaser();
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireLaser()
        {
            if (this.hasFiredGauntlet)
            {
                return;
            }
            //SetStep((int)gauntlet + 1);
            //base.characterBody.AddSpreadBloom(FireFireBolt.bloom);
            this.hasFiredGauntlet = true;

            Ray aimRay = base.GetAimRay();

            if (this.childLocator)
            {
                this.muzzleTransform = this.childLocator.FindChild(this.muzzleString);
            }
            if (this.muzzleflashEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, this.muzzleString, false);
            }
            if (base.isAuthority)
            {
                if (base.characterMotor)
                {
                    base.characterMotor.ApplyForce(aimRay.direction * -selfForce, false, false);
                }

                new BulletAttack
                {
                    owner = base.gameObject,
                    weapon = base.gameObject,
                    origin = aimRay.origin,
                    aimVector = aimRay.direction,
                    minSpread = 0f,
                    maxSpread = 0f,
                    damage = damageCoefficient * this.damageStat,
                    procCoefficient = procCoefficient,
                    stopperMask = 0,
                    force = force,
                    tracerEffectPrefab = VoidLaserTurbineSkill.tracerLaser,
                    muzzleName = this.muzzleString,
                    hitEffectPrefab = FireLaserbolt.impactEffectPrefab,
                    isCrit = Util.CheckRoll(this.critStat, base.characterBody.master),
                    radius = 5f,
                    falloffModel = BulletAttack.FalloffModel.None,
                    maxDistance = maxRange,
                    smartCollision = false
                }.Fire();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}
