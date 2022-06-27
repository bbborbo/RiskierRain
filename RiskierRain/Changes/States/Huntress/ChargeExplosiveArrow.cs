using EntityStates.Huntress.HuntressWeapon;
using EntityStates.LemurianMonster;
using EntityStates.Mage.Weapon;
using EntityStates.Toolbot;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.EntityState.Huntress
{
    class ChargeExplosiveArrow : BaseChargeBombState
    {
        public static GameObject muzzleFlashPerfectTiming = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/Muzzleflash1");
        public static float baseWindUpDuration = 0.9f;
        public static float baseWindDownDuration = 0.1f;
        public static float sweetSpotDuration = 0.2f; // not affected by attack speed
        internal float windUpDuration;
        bool hasFullyCharged = false;
        public override void OnEnter()
        {
            baseDuration = baseWindUpDuration + (sweetSpotDuration * this.attackSpeedStat) + baseWindDownDuration;
            windUpDuration = baseWindUpDuration / this.attackSpeedStat;
            minChargeDuration = (FireExplosiveArrow.minDamage / FireExplosiveArrow.maxDamage) * windUpDuration;

            base.OnEnter();
            //this.animator.SetBool("chargingArrow", true);
            base.PlayCrossfade("Gesture, Override", "FireSeekingShot", "FireSeekingShot.playbackRate", 2 * this.duration, this.duration * 0.2f / this.attackSpeedStat);
            base.PlayCrossfade("Gesture, Additive", "FireSeekingShot", "FireSeekingShot.playbackRate", 2 * this.duration, this.duration * 0.2f / this.attackSpeedStat);
            //base.PlayCrossfade("Body", "ArrowBarrageLoop", windUpDuration);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate(); 
            if(IsPerfectCharge())
            {
                //this.animator.SetBool("chargingArrow", false);
                EffectManager.SimpleMuzzleFlash(muzzleFlashPerfectTiming, base.gameObject, new FireSeekingArrow().muzzleString, false);
                if (!hasFullyCharged)
                {
                    Util.PlaySound(AimStunDrone.enterSoundString, base.gameObject);
                    hasFullyCharged = true;
                }
            }
            else if(hasFullyCharged)
            {
                Util.PlaySound(AimStunDrone.exitSoundString, base.gameObject);
                hasFullyCharged = false;
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            this.animator.SetFloat("FireSeekingShot.fire", 0); 
            if (hasFullyCharged)
            {
                Util.PlaySound(AimStunDrone.exitSoundString, base.gameObject);
                hasFullyCharged = false;
            }
        }

        public override BaseThrowBombState GetNextState()
        {
            FireExplosiveArrow nextState = new FireExplosiveArrow();
            nextState.isCrit = base.RollCrit() ? true : IsPerfectCharge();
            return nextState;
        }

        bool IsPerfectCharge()
        {
            return (this.fixedAge >= windUpDuration && this.fixedAge < windUpDuration + sweetSpotDuration);
        }
    }
}
