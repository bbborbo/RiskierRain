using EntityStates;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Enemies.VoidDreamers
{
    class DreamersVolleyState : BaseSkillState
    {
        float damageCoefficient = 1;
        float force = 10;
        public override void OnEnter()
        {
            FireVolley();
            outer.SetNextStateToMain();
        }


        private void FireVolley()
        {
            Ray aimRay = base.GetAimRay();
            for (int i = 20; i > 0; i--)
            {
                FireOrb(aimRay, i);
            }            
        }
        private void FireOrb(Ray aimray, int a)
        {
            Vector3 vector = Util.ApplySpread(aimray.direction, 0 + a, 2 + 2 * a, 1, 1) ;
            float speed = UnityEngine.Random.Range(1, 20);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = VoidDreamerSkill.dreamOrbPrefab,
                position = aimray.origin,
                rotation = Util.QuaternionSafeLookRotation(vector),
                owner = base.gameObject,
                damage = damageCoefficient * base.characterBody.damage,
                force = force,
                crit = Util.CheckRoll(this.critStat, base.characterBody.master),
                speedOverride = speed,
                useSpeedOverride = true,
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
