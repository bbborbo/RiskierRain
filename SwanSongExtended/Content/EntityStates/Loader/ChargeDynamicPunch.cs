using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using EntityStates.Loader;
using RoR2.UI;

namespace SwanSongExtended.States.Loader
{
    class ChargeDynamicPunch : BaseSkillState
    {
        public float baseChargeDuration = .5f;
        public float baseWinddownDuration = .2f;

        public static float damageCoefficient = 3f;
        public static float procCoefficient = 1f;
        public static float force = 500;

        private const float baseMinChargeDuration = 0.15f;
        private float stopwatch;
        private float charge;
        private float windDownDuration;
        private float chargeDuration;
        private float minChargeDuration;
        private bool hasPunched;

        private uint soundID;
        private Transform chargeVfxInstanceTransform;
        private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

        public override void OnEnter()
        {
            base.OnEnter();
            this.windDownDuration = this.baseWinddownDuration / this.attackSpeedStat;
            this.chargeDuration = this.baseChargeDuration / this.attackSpeedStat;
            this.minChargeDuration = baseMinChargeDuration / this.attackSpeedStat;

            //this.soundID = Util.PlayAttackSpeedSound(this.chargeSoundString, base.gameObject, this.attackSpeedStat);

            //base.PlayAnimation("Gesture, Additive", "PrepFlamethrower", "Flamethrower.playbackRate", this.chargeDuration);
            Util.PlaySound(BaseChargeFist.enterSFXString, base.gameObject);
            this.soundID = Util.PlaySound(BaseChargeFist.startChargeLoopSFXString, base.gameObject);
        }
        public override void OnExit()
        {
            base.OnExit();
            if (this.chargeVfxInstanceTransform)
            {
                EntityState.Destroy(this.chargeVfxInstanceTransform.gameObject);
                this.PlayAnimation("Gesture, Additive", BaseChargeFist.EmptyStateHash);
                this.PlayAnimation("Gesture, Override", BaseChargeFist.EmptyStateHash);
                CrosshairUtils.OverrideRequest overrideRequest = this.crosshairOverrideRequest;
                if (overrideRequest != null)
                {
                    overrideRequest.Dispose();
                }
                this.chargeVfxInstanceTransform = null;
            }
            base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
            Util.PlaySound(BaseChargeFist.endChargeLoopSFXString, base.gameObject);
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            AkSoundEngine.SetRTPCValueByPlayingID("loaderShift_chargeAmount", this.charge * 100f, this.soundID);
            base.characterBody.SetSpreadBloom(this.charge, true);
            base.characterBody.SetAimTimer(3f);
            if (this.charge >= BaseChargeFist.minChargeForChargedAttack && !this.chargeVfxInstanceTransform && BaseChargeFist.chargeVfxPrefab)
            {
                if (BaseChargeFist.crosshairOverridePrefab && this.crosshairOverrideRequest == null)
                {
                    this.crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, BaseChargeFist.crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
                }
                Transform transform = base.FindModelChild(BaseChargeFist.chargeVfxChildLocatorName);
                if (transform)
                {
                    this.chargeVfxInstanceTransform = UnityEngine.Object.Instantiate<GameObject>(BaseChargeFist.chargeVfxPrefab, transform).transform;
                    ScaleParticleSystemDuration component = this.chargeVfxInstanceTransform.GetComponent<ScaleParticleSystemDuration>();
                    if (component)
                    {
                        component.newDuration = (1f - BaseChargeFist.minChargeForChargedAttack) * this.chargeDuration;
                    }
                }
                base.PlayCrossfade("Gesture, Additive", BaseChargeFist.ChargePunchIntroStateHash, BaseChargeFist.ChargePunchIntroParamHash, this.chargeDuration, 0.1f);
                base.PlayCrossfade("Gesture, Override", BaseChargeFist.ChargePunchIntroStateHash, BaseChargeFist.ChargePunchIntroParamHash, this.chargeDuration, 0.1f);
            }
            if (this.chargeVfxInstanceTransform)
            {
                base.characterMotor.walkSpeedPenaltyCoefficient = BaseChargeFist.walkSpeedCoefficient;
            }
            if (base.isAuthority)
            {
                this.AuthorityFixedUpdate();
            }
            this.stopwatch += Time.fixedDeltaTime;
            this.charge += Time.fixedDeltaTime * base.characterBody.attackSpeed;

            //if (!this.hasPunched && (this.stopwatch >= chargeDuration || !IsKeyDownAuthority()) && !this.hasPunched && this.stopwatch >= minChargeDuration)
            //{
            //    this.DoPunch();
            //}
            //if (this.stopwatch >= this.windDownDuration && this.hasPunched && base.isAuthority)
            //{
            //    this.outer.SetNextStateToMain();
            //    return;
            //}
        }
        public override void Update()
        {
            base.Update();
            Mathf.Clamp01(base.age / this.chargeDuration);
        }
        private void AuthorityFixedUpdate()
        {
            if (!this.ShouldKeepChargingAuthority())
            {
                this.outer.SetNextState(this.GetNextStateAuthority(false));
                return;
            }
            if (this.charge >= this.chargeDuration)
            {
                this.outer.SetNextState(this.GetNextStateAuthority(true));
                return;
            }
        }
        protected virtual bool ShouldKeepChargingAuthority()
        {
            return base.IsKeyDownAuthority();
        }
        protected virtual EntityState GetNextStateAuthority(bool isKeyHeld)
        {
            if (isKeyHeld)
            {
                return new DynamicPunchRush 
                {
                activatorSkillSlot = base.activatorSkillSlot
                };
            }
            return new DynamicPunchJab { };
        }        
    }
}
