using EntityStates;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Enemies.VoidDreamers
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
            ATFieldComponent atField = GameObject.FindObjectOfType<ATFieldComponent>();
            if (atField == null)
            {
                Debug.Log("atfield null wah wah");
                return;
            }
            Debug.Log("shoot the goober");
            ProjectileSimple ps = atField.gameObject.GetComponent<ProjectileSimple>();
            if (ps == null)
            {
                Debug.Log("wahwahwah");
                return;
            }
            ps.desiredForwardSpeed = 20;
        }

        private void FireField()
        {
            hasFiredField = true;

            FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
            fireProjectileInfo.projectilePrefab = DreamersATFieldSkill.atField;
            fireProjectileInfo.owner = base.gameObject;
            fireProjectileInfo.position = base.gameObject.transform.position;
            fireProjectileInfo.rotation = base.gameObject.transform.rotation;
            fireProjectileInfo.damage = 0;
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
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
