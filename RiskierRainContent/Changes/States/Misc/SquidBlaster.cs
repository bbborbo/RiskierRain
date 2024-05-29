using EntityStates;
using EntityStates.Mage.Weapon;
using RiskierRainContent.Skills;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRainContent.CoreModules;
using UnityEngine.Networking;
using System.Linq;

namespace RiskierRainContent.States
{
    class SquidBlaster : BaseSkillState
    {
        public static float damageCoefficient = 3;
        float force = 10;
        //stolen from FireSpine
        public static float procCoefficient = 1f;
        public static float forceScalar = 1f;

        public static float baseDuration = 2f;
        private float duration;
        private bool hasBlasted;
        private BullseyeSearch enemyFinder;
        public bool fullVision = true;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = SquidBlaster.baseDuration / this.attackSpeedStat;
            base.GetAimRay();
            this.PlayAnimation("Gesture", "FireGoo");
            if (base.isAuthority)
            {
                this.FireBomb();
            }
        }


        private void FireBomb()//aiming and shooting
        {
            if (this.hasBlasted || !NetworkServer.active)
            {
                return;
            }
            Ray aimRay = base.GetAimRay();
            this.enemyFinder = new BullseyeSearch();
            this.enemyFinder.viewer = base.characterBody;
            this.enemyFinder.maxDistanceFilter = float.PositiveInfinity;
            this.enemyFinder.searchOrigin = aimRay.origin;
            this.enemyFinder.searchDirection = aimRay.direction;
            this.enemyFinder.sortMode = BullseyeSearch.SortMode.Distance;
            this.enemyFinder.teamMaskFilter = TeamMask.allButNeutral;
            this.enemyFinder.minDistanceFilter = 0f;
            this.enemyFinder.maxAngleFilter = (this.fullVision ? 180f : 90f);
            this.enemyFinder.filterByLoS = true;
            if (base.teamComponent)
            {
                this.enemyFinder.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
            }
            this.enemyFinder.RefreshCandidates();
            HurtBox hurtBox = this.enemyFinder.GetResults().FirstOrDefault<HurtBox>();
            if (hurtBox)
            {
                Blast(aimRay, hurtBox);
            }
        }        
        private void Blast(Ray aimRay, HurtBox hurtBox)//actual shooting
        {
            Vector3 vector = hurtBox.transform.position - base.GetAimRay().origin;
            aimRay.origin = base.GetAimRay().origin;
            aimRay.direction = vector;
            base.inputBank.aimDirection = vector;
            base.StartAimMode(aimRay, 2f, false);
            this.hasBlasted = true;

            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = Assets.squidBlasterBall,
                position = aimRay.origin,
                rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                owner = base.gameObject,
                damage = damageCoefficient * base.characterBody.damage,
                force = force,
                crit = Util.CheckRoll(this.critStat, base.characterBody.master),
                speedOverride = 20,
                useSpeedOverride = true,
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }

        public override void OnExit()
        {
            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}

