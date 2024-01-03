using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRainContent.Components
{
    public class TargetRandomNearbyBehavior : RoR2.CharacterBody.ItemBehavior
    {
        public delegate void OnTargetFoundHandler(TargetRandomNearbyBehavior instance, CharacterBody newTarget, CharacterBody oldTarget);
        public static event OnTargetFoundHandler OnTargetFoundEvent;
        public static void InvokeTargetFound(TargetRandomNearbyBehavior instance, CharacterBody newTarget, CharacterBody oldTarget)
        {
            OnTargetFoundEvent?.Invoke(instance, newTarget, oldTarget);
        }

        CharacterBody currentTarget;
        public Dictionary<string, int> itemCounts = new Dictionary<string, int>();

        public static float baseHauntInterval = 10;
        public static float baseHauntRadius = 35;
        public static float hauntRetryTime = 1;
        float hauntStopwatch = 0;
        public int RecalculateItemCount()
        {
            int i = 0;
            foreach(KeyValuePair<string, int> kvp in itemCounts)
            {
                i += kvp.Value;
            }
            return i;
        }
        public void EvaluateItemCount()
        {
            stack = RecalculateItemCount();
            if(stack == 0)
            {
                UnityEngine.Object.Destroy(this);
            }
        }

        void Start()
        {
            hauntStopwatch = baseHauntInterval;
        }
        private void FixedUpdate()
        {
            hauntStopwatch += Time.fixedDeltaTime;
            if (hauntStopwatch >= baseHauntInterval)
            {
                if (NetworkServer.active)
                {
                    SphereSearch sphereSearch = new SphereSearch
                    {
                        mask = LayerIndex.entityPrecise.mask,
                        origin = body.transform.position,
                        queryTriggerInteraction = QueryTriggerInteraction.Collide,
                        radius = baseHauntRadius
                    };

                    TeamMask teamMask = TeamMask.AllExcept(body.teamComponent.teamIndex);
                    List<HurtBox> hurtBoxesList = new List<HurtBox>();

                    sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(hurtBoxesList);

                    int hurtBoxCount = hurtBoxesList.Count;
                    while (hurtBoxCount > 0)
                    {
                        int i = UnityEngine.Random.Range(0, hurtBoxCount - 1);
                        HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
                        CharacterBody enemyBody = healthComponent.body;

                        if (!enemyBody)
                        {
                            hurtBoxesList.Remove(hurtBoxesList[i]);
                            hurtBoxCount--;
                            continue;
                        }

                        Debug.Log("Target found");
                        InvokeTargetFound(this, enemyBody, currentTarget);
                        currentTarget = enemyBody;
                        hauntStopwatch -= baseHauntInterval;
                        return;
                    }
                    Debug.Log("Retrying target");
                    hauntStopwatch -= hauntRetryTime;
                }
            }
        }

        private void DebuffEnemy(CharacterBody enemyBody)
        {
            for (int n = 0; n < stack; n++)
            {
                enemyBody.AddTimedBuffAuthority(Assets.harpoonDebuff.buffIndex, RiskierRainContent.harpoonTargetTime);
            }

            //thanks hifu <3
            Transform modelTransform = enemyBody.modelLocator?.modelTransform;
            if (modelTransform != null)
            {
                var temporaryOverlay = modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                temporaryOverlay.duration = RiskierRainContent.harpoonTargetTime;
                temporaryOverlay.animateShaderAlpha = true;
                temporaryOverlay.alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);// AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlay.destroyComponentOnEnd = true;
                temporaryOverlay.originalMaterial = RiskierRainContent.harpoonTargetMaterial;
                temporaryOverlay.AddToCharacerModel(modelTransform.GetComponent<CharacterModel>());
            }
        }

        private void OnDisable()
        {
            hauntStopwatch = 0;
        }
    }
}
