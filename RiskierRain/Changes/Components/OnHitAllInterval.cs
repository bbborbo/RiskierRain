using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// used for things like adding lightning elite bombs to beetle guard's sunder projectile
/// </summary>
namespace RiskierRain.Components
{
    [RequireComponent(typeof(ProjectileController))]
    class OnHitAllInterval : MonoBehaviour
    {
        public ProjectileController pc;
        public ProjectileDamage pd;
        public float interval = 0.5f;
        float timer = 0;
        void Start()
        {
            if (pc == null)
                pc = GetComponent<ProjectileController>();
            if (pd == null)
                pd = GetComponent<ProjectileDamage>();
            timer = interval;
        }
        void FixedUpdate()
        {
            while (timer <= interval)
            {
                timer += interval;
                DoOnHitAll();
            }
            timer -= Time.fixedDeltaTime;
        }

        private void DoOnHitAll()
        {
            if (!pc)
                return;
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.attacker = pc.owner;
            damageInfo.inflictor = this.gameObject;
            damageInfo.position = pc.transform.position;
            damageInfo.procCoefficient = pc.procCoefficient;
            damageInfo.damage = pd ? pd.damage : 0;
            damageInfo.crit = pd ? pd.crit : false;
            GlobalEventManager.instance.OnHitAll(damageInfo, null);
        }
    }
}
