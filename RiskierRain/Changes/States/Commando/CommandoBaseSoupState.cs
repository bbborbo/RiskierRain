using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain.EntityState.Commando
{
    public class CommandoBaseSoupState : BaseSkillState
	{
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Pain;
		}
	}
}
