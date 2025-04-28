using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RainrotSharedUtils.Components
{
    [RequireComponent(typeof(ProjectileDamage))]
    public class ProjectileIncreaseDamageAfterDistance : MonoBehaviour
    {
        ProjectileDamage projectileDamage;
        public float requiredDistance = 21;
        public float damageMultiplierOnIncrease = 1;
        public GameObject effectPrefab;
        float requiredDistanceSqr => requiredDistance * requiredDistance;

        Vector3 initialPosition;
        bool damageIncreased;
        
        void OnEnable()
        {
            initialPosition = transform.position;
            projectileDamage = GetComponent<ProjectileDamage>();
        }
        void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;
            if (damageIncreased)
                return;
            float sqrDistance = (transform.position - initialPosition).sqrMagnitude;
            if(sqrDistance >= requiredDistanceSqr)
            {
                damageIncreased = true;
                projectileDamage.damage *= damageMultiplierOnIncrease;
                if(effectPrefab != null)
                {
                    EffectData effectData = new EffectData
                    {
                        origin = transform.position
                    };
                    EffectManager.SpawnEffect(effectPrefab, effectData, true);
                }
            }
        }
    }
}
