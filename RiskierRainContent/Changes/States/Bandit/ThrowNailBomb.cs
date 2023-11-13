using RiskierRainContent.Skills;
using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRainContent.EntityState.Bandit
{
    class ThrowNailBomb : GenericProjectileBaseState
    {
        public static float damageCoeff = 1.5f;
        public override void OnEnter()
        {
            base.projectilePrefab = NailBombSkill.nailBombProjectile;
            base.baseDuration = 0.3f;
            base.damageCoefficient = damageCoeff;
            base.OnEnter();
            base.PlayAnimation("Gesture, Additive", "SlashBlade", "SlashBlade.playbackRate", this.duration);
        }
    }
}
