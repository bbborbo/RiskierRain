using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine.AddressableAssets;
using EntityStates;
using UnityEngine;
using SwanSongExtended.Modules;
using SwanSongExtended.States;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public void SquolypRework()
        {
            SquolypChangeAttack();
            SquolypChangeStats();
        }

        void SquolypChangeAttack()
        {
            SkillDef squolypFire = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Squid/SquidTurretBodyTurret.asset").WaitForCompletion();
            Content.AddEntityState(typeof(SquidBlaster));
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
