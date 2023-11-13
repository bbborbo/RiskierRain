using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Enemies.VoidDreamers
{
    class DreamersATFieldEnter : BaseSkillState
    {
        float duration;
        float durationBase = 1;
        public override void OnEnter()
        {
            base.OnEnter();
            duration = durationBase;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            duration -= Time.fixedDeltaTime;
            if (duration <= 0)
            {
                ActivateField();
            }
        }

        private void ActivateField()
        {
            DreamersATFieldChannel state = new DreamersATFieldChannel();
            outer.SetNextState(state);
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}
