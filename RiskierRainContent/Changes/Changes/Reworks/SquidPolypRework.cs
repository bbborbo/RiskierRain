using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine.AddressableAssets;
using RiskierRainContent.CoreModules;
using RiskierRainContent.States;
using EntityStates;
using UnityEngine;

namespace RiskierRainContent
{
    public partial class RiskierRainContent
    {
        public void SquolypRework()
        {
            SquolypChangeAttack();
            SquolypChangeStats();
        }

        void SquolypChangeAttack()
        {
            SkillDef squolypFire = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Squid/SquidTurretBodyTurret.asset").WaitForCompletion();
            Assets.RegisterEntityState(typeof(SquidBlaster));
            SerializableEntityStateType newSquolypState = new SerializableEntityStateType(typeof(SquidBlaster));
            squolypFire.activationState = newSquolypState;
        }
        void SquolypChangeStats()
        {
            GameObject squidTurretPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Squid/SquidTurretBody.prefab").WaitForCompletion();
            CharacterBody squidBody = squidTurretPrefab.GetComponent<CharacterBody>();
            if (squidBody)
            {
                squidBody.baseDamage = 12;
                squidBody.levelDamage = 2.4f;
            }
        }
    }
}
