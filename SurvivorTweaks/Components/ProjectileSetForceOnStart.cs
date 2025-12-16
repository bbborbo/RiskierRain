using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SurvivorTweaks.Components
{
    class ProjectileSetForceOnStart : MonoBehaviour
    {
        public float force = 0;

        void Start()
        {
            if(this.TryGetComponent(out ProjectileDamage pd))
            {
                pd.force = force;
            }
        }
    }
}
