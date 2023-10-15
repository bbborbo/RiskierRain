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
        public static GameObject muzzleFlashPerfectTiming = FireFlurrySeekingArrow.critMuzzleflashEffectPrefab;//LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/Muzzleflash1");
        public static GameObject muzzleFlashFullCharge = ChargeArrow.muzzleflashEffectPrefab;//LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/Muzzleflash1");
        
        public static float baseWindUpDuration = 0.6f;
        public static float baseWindDownDuration = 0.25f;
        public static float sweetSpotDuration = 0.2f; // not affected by attack speed
        internal float windUpDuration;
        bool hasFullyCharged = false;
        bool isCrit = false;
        public override void OnEnter()
        {
            isCrit = Util.CheckRoll(characterBody.crit, characterBody.master);
            if (isCrit)
            {
                EffectManager.SimpleMuzzleFlash(muzzleFlashPerfectTiming, base.gameObject, new FireSeekingArrow().muzzleString, false);
            }
            baseDuration = baseWindUpDuration;// + (sweetSpotDuration * this.attackSpeedStat) + baseWindDownDuration;
            windUpDuration = baseWindUpDuration / this.attackSpeedStat;
            minChargeDuration = 0;// (FireExplosiveArrow.minDamage / FireExplosiveArrow.maxDamage) * windUpDuration;

            base.OnEnter();
            //this.animator.SetBool("chargingArrow", true);
            //this.PlayAnimation("Gesture, Override", "FireSeekingArrow");
            //this.PlayAnimation("Gesture, Additive", "FireSeekingArrow");
            //base.PlayCrossfade("Gesture, Override", "ArrowBarrageLoop", 0.1f);
            //base.PlayCrossfade("Gesture, Additive", "ArrowBarrageLoop", 0.1f);
            //base.PlayCrossfade("Gesture, Override", "FireSeekingShot", "FireSeekingShot.playbackRate", 2 * this.duration, this.duration * 0.2f / this.attackSpeedStat);
            base.PlayCrossfade("Gesture, Additive", "FireSeekingShot", "FireSeekingShot.playbackRate", 2 * this.duration, this.duration * 0.2f);
            //base.PlayCrossfade("Body", "ArrowBarrageLoop", windUpDuration);
        }
       public override void FixedUpdate()
        {
            fixedAge += Time.fixedDeltaTime;
            if(fixedAge >= this.duration)
            {
                if (!hasFullyCharged)
                {
                    this.animator.SetFloat("FireSeekingShot.fire", 0);
                    hasFullyCharged = true;
                    Util.PlaySound(AimStunDrone.exitSoundString, base.gameObject);
                    EffectManager.SimpleMuzzleFlash(isCrit ? muzzleFlashPerfectTiming : muzzleFlashFullCharge, base.gameObject, new FireSeekingArrow().muzzleString, false);
                }
                base.PlayCrossfade("Gesture", "ArrowBarrageLoop", 0.1f);
            }

            float charge = this.CalcCharge();
            if (base.isAuthority && ((!base.IsKeyDownAuthority() && base.fixedAge >= this.minChargeDuration)))
            {
                BaseThrowBombState nextState = this.GetNextState();
                nextState.charge = charge;
                this.outer.SetNextState(nextState);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            //this.animator.SetFloat("FireSeekingShot.fire", 0);
            this.PlayAnimation("Gesture", "BufferEmpty");
            this.PlayAnimation("Gesture", "BufferEmpty");
            if (hasFullyCharged)
            {
                hasFullyCharged = false;
            }
        }

        public override BaseThrowBombState GetNextState()
        {
            FireExplosiveArrow nextState = new FireExplosiveArrow();
            nextState.baseDuration = baseWindDownDuration;
            nextState.isCrit = this.isCrit;
            return nextState;
        }

        bool IsPerfectCharge()
        {
            return (this.fixedAge >= windUpDuration && this.fixedAge < windUpDuration + sweetSpotDuration);
        }
    }
}
