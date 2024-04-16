using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;
using Unity;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;
using UnityEngine.Networking;

namespace Ror2AggroTools
{
    public enum AggroPriority
    {
        LowAggro = -1,
        Normal = 0,
        HighAggro,
        Killeth
    }

    public static class Aggro
    {
        public static void AggroMinionsToEnemy(CharacterMaster master, CharacterBody victim)
        {
            ApplyAggroBuff(victim);
            ResetMinionAggro(master);
        }
        public static void ApplyAggroBuff(CharacterBody victim)
        {
            if (NetworkServer.active)
            {
                victim.AddTimedBuff(AggroToolsPlugin.priorityAggro, 5f);
            }
        }
        public static void ShedAggroFromCharacter(CharacterBody body)
        {
            ShedAggroFromCharacter(body.gameObject);
        }
        public static void ShedAggroFromCharacter(GameObject gameObject)
        {
            //loop through every ai 
            /*BaseAI[] baseAIs = GameObject.FindObjectsOfType<BaseAI>();
            foreach (BaseAI baseAI in baseAIs)
            {
                //if an ai is targeting the character that's trying to shed aggro
                if (baseAI.currentEnemy.gameObject == gameObject)
                {
                    ResetAggro(baseAI);
                }
            }*/

            //loop through every ai 
            foreach (KeyValuePair<BaseAI, BaseAI.Target> keyValuePair in AIChanges.aiTargetPairs)
            {
                //if an ai is known to be targeting the character that's trying to shed aggro
                if (keyValuePair.Value.gameObject == gameObject)
                {
                    ResetAggro(keyValuePair.Key);
                }
            }
        }
        public static void ResetMinionAggro(CharacterMaster master)
        {
            AIOwnership[] baseAIs = GameObject.FindObjectsOfType<AIOwnership>();
            foreach (AIOwnership baseAI in baseAIs)
            {
                //if an ai is targeting the character that's trying to shed aggro
                if (baseAI.ownerMaster == master)
                {
                    ResetAggro(baseAI.baseAI);
                }
            }
        }
        public static void ResetAggro(BaseAI baseAI)
        {
            baseAI.currentEnemy.Reset();
        }
        public static AggroPriority GetAggroPriority(CharacterBody body)
        {
            if (body.HasBuff(AggroToolsPlugin.priorityAggro))
                return AggroPriority.HighAggro;
            return AggroPriority.Normal;
        }
    }
}
