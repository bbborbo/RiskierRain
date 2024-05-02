using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine.AddressableAssets;
using RiskierRainContent.CoreModules;
using RiskierRainContent.States;
using EntityStates;

namespace RiskierRainContent
{
    public partial class RiskierRainContent
    {
        public void SquolypRework()
        {
            SkillDef squolypFire = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Squid/SquidTurretBodyTurret.asset").WaitForCompletion();
            Assets.RegisterEntityState(typeof(SquidBlaster));
            SerializableEntityStateType newSquolypState = new SerializableEntityStateType(typeof(SquidBlaster));
            squolypFire.activationState = newSquolypState;
        }
    }
}
