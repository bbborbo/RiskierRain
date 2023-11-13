using EntityStates;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Enemies.VoidDreamers
{
    class DreamersATFieldChannel : BaseSkillState
    {
        float duration;
        float baseDuration = 15;
        bool lowHealth;
        bool hasFiredField;

        public override void OnEnter()
        {
            base.OnEnter();
            CheckHealth();
            Debug.Log($"is on low health = {lowHealth}");
            duration = baseDuration;
            hasFiredField = false;
            if (lowHealth)
            {
                duration /= 2;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            duration -= Time.fixedDeltaTime;
            if (!hasFiredField)
            {
                FireField();
            }
            if (duration <= 0)
            {
                OnDurationEnd();
            }
        }

        private void OnDurationEnd()
        {
            if (lowHealth)
            {
                Debug.Log("low health!! launching the goober!!!!!");
                LaunchField();
            }
            outer.SetNextStateToMain();
        }

        private void LaunchField()
        {
            ATFieldComponent[] fieldParts = GameObject.FindObjectsOfType<ATFieldComponent>();
            for (int i = 0; i <
                fieldParts.Length; i++)
            {
                LaunchProjectile(fieldParts[i].gameObject);
            }
        }
        private void LaunchProjectile(GameObject projectile)
        {
            Debug.Log("shootthegoober");
            ProjectileSimple ps = projectile.GetComponent<ProjectileSimple>();
            if (ps == null)
            {
                Debug.Log("wahwahwah");
                return;
            }
            ps.desiredForwardSpeed = 10;
        }

        private void FireField()
        {
            hasFiredField = true;
            Ray aimray = base.GetAimRay();
            FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
            fireProjectileInfo.projectilePrefab = DreamersATFieldSkill.atField;
            fireProjectileInfo.owner = base.gameObject;
            fireProjectileInfo.position = base.gameObject.transform.position;
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimray.direction);
            fireProjectileInfo.damage = 0;
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);

            FireProjectileInfo invisFieldInfo = default(FireProjectileInfo);
            invisFieldInfo.projectilePrefab = DreamersATFieldSkill.invisField;
            invisFieldInfo.owner = base.gameObject;
            invisFieldInfo.position = base.gameObject.transform.position;
            invisFieldInfo.rotation = Util.QuaternionSafeLookRotation(aimray.direction);
            invisFieldInfo.damage = 0;
            ProjectileManager.instance.FireProjectile(invisFieldInfo);

        }

        private void CheckHealth()
        {
            HealthComponent hc = base.characterBody.healthComponent;
            Debug.Log(hc.combinedHealthFraction);
            lowHealth = (hc.combinedHealthFraction < 0.5f);
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}
