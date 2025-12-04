using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RainrotSharedUtils.Assets;

namespace RainrotSharedUtils.Components
{

    public class NebulaPickup : MonoBehaviour
    {
        public BuffDef buffDef;

        [Tooltip("The base object to destroy when this pickup is consumed.")]
        public GameObject baseObject;
        [Tooltip("The team filter object which determines who can pick up this pack.")]
        public TeamFilter teamFilter;
        public GameObject pickupEffect;
        private bool alive = true;

        private void OnTriggerStay(Collider other)
        {
            if (other != null)
            {
                if (NetworkServer.active && this.alive && TeamComponent.GetObjectTeam(other.gameObject) == this.teamFilter.teamIndex)
                {
                    CharacterBody body = other.GetComponent<CharacterBody>();
                    if (body)
                    {
                        NebulaPickup.ApplyNebulaBooster(this.buffDef, body);
                        EffectManager.SpawnEffect(this.pickupEffect, new EffectData
                        {
                            origin = base.transform.position
                        }, true);

                        UnityEngine.Object.Destroy(this.baseObject);
                    }
                }
            }
        }
        public static void ApplyNebulaBooster(BuffDef buffDef, CharacterBody targetBody)
        {
            if (!NetworkServer.active)
            {
                return;
            }
            if (!buffDef)
            {
                return;
            }

            AddBoosterBuff(buffDef, targetBody);

            IEnumerable<TeamComponent> recipients = TeamComponent.GetTeamMembers(targetBody.teamComponent.teamIndex);

            foreach (TeamComponent teamComponent in recipients)
            {
                if (teamComponent != targetBody.teamComponent 
                    && (teamComponent.transform.position - targetBody.corePosition).sqrMagnitude <= nebulaBoosterBuffRadius * nebulaBoosterBuffRadius)
                {
                    CharacterBody body = teamComponent.body;//.GetComponent<CharacterBody>();
                    if (body)
                    {
                        AddBoosterBuff(buffDef, body);
                    }
                }
            }
        }
        public static void AddBoosterBuff(BuffDef buffDef, CharacterBody body)
        {
            body.AddTimedBuff(buffDef, nebulaBoosterBuffDuration, maxNebulaBoosterStackCount);
        }
        public static void CreateBoosterPickup(Vector3 spawnPoint, TeamIndex team, GameObject boosterPrefab, int boosterCount = 1)
        {
            if (boosterPrefab != null)
            {
                for(int i = 0; i < boosterCount; i++)
                {
                    GameObject boosterToSpawn = UnityEngine.Object.Instantiate<GameObject>(boosterPrefab, spawnPoint, UnityEngine.Random.rotation);
                    boosterToSpawn.GetComponent<TeamFilter>().teamIndex = team;
                    VelocityRandomOnStart boosterVROS = boosterToSpawn.GetComponent<VelocityRandomOnStart>();
                    if (boosterVROS != null)
                    {
                        boosterVROS.baseDirection = new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-0.6f, 0.2f), UnityEngine.Random.Range(-1, 1));
                    }

                    //gameObject6.transform.localScale = Vector3.one;
                    NetworkServer.Spawn(boosterToSpawn);
                }
            }
        }
    }
    public class NebulaGravitate : GravitatePickup
    {

        new void FixedUpdate()
        {
            if (this.gravitateTarget)
            {
                this.rigidbody.velocity = Vector3.MoveTowards(this.rigidbody.velocity, (this.gravitateTarget.transform.position - base.transform.position).normalized * this.maxSpeed, this.acceleration);
            }
        }
    }
}
