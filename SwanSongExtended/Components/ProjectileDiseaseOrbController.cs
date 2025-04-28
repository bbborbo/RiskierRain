using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using SwanSongExtended.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SwanSongExtended.Components
{
    [RequireComponent(typeof(ProjectileController), typeof(ProjectileDamage))]
    public class ProjectileDiseaseOrbController : MonoBehaviour, IProjectileImpactBehavior
    {
        private TeamIndex myTeamIndex
        {
            get
            {
                if (!this.teamFilter)
                {
                    return TeamIndex.Neutral;
                }
                return this.teamFilter.teamIndex;
            }
        }
        private ProjectileController projectileController;
        private TeamFilter teamFilter;
        private ProjectileDamage projectileDamage;

        public float maxOrbRange = 100;
        public float blastRadius = 3;
        public float procCoefficient;
        public int bounces;
        public float damageCoefficient;
        bool fired = false;

        void Start()
        {
            if (NetworkServer.active)
            {
                this.projectileController = base.GetComponent<ProjectileController>();
                this.teamFilter = this.projectileController.teamFilter;
                this.projectileDamage = base.GetComponent<ProjectileDamage>();
                return;
            }
            base.enabled = false;
        }
        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            if (fired)
                return;
            fired = true;
            Vector3 impactPosition = impactInfo.estimatedPointOfImpact;
            bool debuffFirstTarget = true;
            List<HealthComponent> targetsHit = new List<HealthComponent>();

            BullseyeSearch bullseyeSearch = new BullseyeSearch();
            bullseyeSearch.searchOrigin = impactPosition;
            bullseyeSearch.maxDistanceFilter = maxOrbRange;
            bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(myTeamIndex);
            bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
            bullseyeSearch.RefreshCandidates();
            //EffectManager.SimpleMuzzleFlash(Disease.muzzleflashEffectPrefab, self.gameObject, Disease.muzzleString, true);
            List<HurtBox> list = bullseyeSearch.GetResults().ToList<HurtBox>();
            if (list.Count > 0)
            {
                HurtBox firstTarget = list.FirstOrDefault<HurtBox>();
                List<VineOrb.SplitDebuffInformation> splitDotInfo = null;
                if (firstTarget)
                {
                    CharacterBody targetBody = firstTarget.healthComponent.body;
                    CharacterBody ownerBody = null;
                    if (projectileController.owner)
                        ownerBody = projectileController.owner.GetComponent<CharacterBody>();
                    if (targetBody != null && ownerBody != null)
                    {
                        Vector3 deltaVector = targetBody.corePosition - impactPosition;
                        //Vector3 normalized = deltaVector.normalized;
                        //Vector3 adjusted = deltaVector - (normalized * targetBody.radius);
                        float sqrDistance = (deltaVector).sqrMagnitude;
                        if (true)//sqrDistance <= Mathf.Pow(blastRadius + targetBody.radius, 2))
                        {
                            splitDotInfo = DiseaseOrb.GetSplitDotInformation(targetBody, ownerBody);
                            targetsHit.Add(firstTarget.healthComponent);
                        }
                    }
                }

                DiseaseOrb diseaseOrb = new DiseaseOrb();
                diseaseOrb.splitDotInformation = splitDotInfo;
                diseaseOrb.bouncedObjects = new List<HealthComponent>();
                diseaseOrb.debuffBlacklistedObjects = targetsHit;
                diseaseOrb.attacker = this.projectileController.owner;
                diseaseOrb.inflictor = base.gameObject;
                diseaseOrb.teamIndex = this.myTeamIndex;
                diseaseOrb.damageValue = this.projectileDamage.damage * this.damageCoefficient;
                diseaseOrb.isCrit = this.projectileDamage.crit;
                diseaseOrb.origin = impactPosition;
                diseaseOrb.bouncesRemaining = this.bounces;
                diseaseOrb.procCoefficient = this.procCoefficient;
                diseaseOrb.target = firstTarget;
                diseaseOrb.damageColorIndex = this.projectileDamage.damageColorIndex;
                diseaseOrb.damageType = this.projectileDamage.damageType;
                //diseaseOrb.range = this.maxOrbRange;
                OrbManager.instance.AddOrb(diseaseOrb);
            }
        }
    }
}
