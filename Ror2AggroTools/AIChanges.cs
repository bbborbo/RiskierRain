using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;

namespace Ror2AggroTools
{
    public static class AIChanges
    {
        public static Dictionary<BaseAI, BaseAI.Target> aiTargetPairs;

        public static void Init()
        {
            aiTargetPairs = new Dictionary<BaseAI, BaseAI.Target>();


            On.RoR2.CharacterAI.BaseAI.FindEnemyHurtBox += BaseAI_FindEnemyHurtbox;
            On.RoR2.CharacterAI.BaseAI.OnBodyDamaged += BaseAI_OnBodyDamaged;

            return;
            Hook ospHook = new Hook(
              typeof(BaseAI).GetMethod("set_currentEnemy", (BindingFlags)(-1)),
              typeof(AggroToolsPlugin).GetMethod(nameof(Set_CurrentEnemy), (BindingFlags)(-1))
            );
            On.RoR2.CharacterAI.BaseAI.OnDestroy += ClearTargetPairing;
        }

        private static void ClearTargetPairing(On.RoR2.CharacterAI.BaseAI.orig_OnDestroy orig, BaseAI self)
        {
            orig(self);
            if (aiTargetPairs.ContainsKey(self))
            {
                aiTargetPairs.Remove(self);
            }
        }
        public static void Set_CurrentEnemy(orig_setCurrentEnemy orig, BaseAI self, BaseAI.Target target)
        {
            if(target != self.currentEnemy)
            {
                if(target == null)
                {
                    aiTargetPairs.Remove(self);
                }
                else
                {
                    aiTargetPairs[self] = target;
                }
            }
            orig(self, target);
        }
        public delegate void orig_setCurrentEnemy(BaseAI self, BaseAI.Target target);

        private static HurtBox BaseAI_FindEnemyHurtbox(On.RoR2.CharacterAI.BaseAI.orig_FindEnemyHurtBox orig, RoR2.CharacterAI.BaseAI self, float maxDistance, bool full360Vision, bool filterByLoS)
        {
            HurtBox originalTarget = orig(self, maxDistance, full360Vision, filterByLoS);
            bool modified = false;

            IEnumerable<BullseyeSearch.CandidateInfo> candidates = self.enemySearch.candidatesEnumerable;
            List<BullseyeSearch.CandidateInfo> newCandidates = new List<BullseyeSearch.CandidateInfo>();
            foreach(BullseyeSearch.CandidateInfo c in candidates)
            {
                float newDistance = c.distanceSqr;
                CharacterBody b = c.hurtBox.healthComponent?.body;
                if (b)
                {
                    switch (Aggro.GetAggroPriority(b))
                    {
                        case AggroPriority.Killeth:
                            return c.hurtBox;
                        case AggroPriority.HighAggro:
                            modified = true;
                            newDistance /= AggroToolsPlugin.highPriorityAggroWeight;
                            break;
                        case AggroPriority.LowAggro:
                            modified = true;
                            newDistance *= AggroToolsPlugin.lowPriorityAggroWeight;
                            break;
                    }
                }

                BullseyeSearch.CandidateInfo newCandidate = new BullseyeSearch.CandidateInfo
                {
                    hurtBox = c.hurtBox,
                    position = c.position,
                    dot = c.dot,
                    distanceSqr = newDistance
                };
                newCandidates.Add(newCandidate);
            }

            if (modified)
            {
                Func<BullseyeSearch.CandidateInfo, float> sorter = self.enemySearch.GetSorter();
                if (sorter != null)
                {
                    self.enemySearch.candidatesEnumerable = (newCandidates as IEnumerable<BullseyeSearch.CandidateInfo>).OrderBy(sorter).ToList();
                }
                return self.enemySearch.GetResults().FirstOrDefault<HurtBox>();
            }
            else
            {
                return originalTarget;
            }
        }

        private static void BaseAI_OnBodyDamaged(On.RoR2.CharacterAI.BaseAI.orig_OnBodyDamaged orig, RoR2.CharacterAI.BaseAI self, DamageReport damageReport)
        {
            if(damageReport.damageInfo == null || damageReport.damageInfo.attacker == null || damageReport.damageInfo.attacker == self.body.gameObject)
            {
                orig(self, damageReport);
                return;
            }
            AggroPriority currentTargetPriority = Aggro.GetAggroPriority(self.currentEnemy?.characterBody);
            AggroPriority attackerPriority = Aggro.GetAggroPriority(damageReport.attackerBody);

            //if the current target is higher priority, ignore the attacker and keep prioritizing the current target
            if(currentTargetPriority > attackerPriority)
            {
                return;
            }

            //if the current target is lower priority, immediately prioritize the attacker
            if(currentTargetPriority < attackerPriority && currentTargetPriority != AggroPriority.None)
            {
                self.currentEnemy.gameObject = null;
            }
            //if the current target is of equal priority, the behavior stays the same
            orig(self, damageReport);
        }
    }
}
