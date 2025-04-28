using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Orbs
{
    public class DiseaseOrb : Orb
    {
        public static event Action<DiseaseOrb> onLightningOrbKilledOnAllBounces;
        public float speed = 100f;
        public float damageValue;
        public GameObject attacker;
        public GameObject inflictor;
        public int bouncesRemaining;
        public List<HealthComponent> debuffBlacklistedObjects;
        public List<HealthComponent> bouncedObjects;
        public TeamIndex teamIndex;
        public bool isCrit;
        public ProcChainMask procChainMask;
        public float procCoefficient = 1f;
        public DamageColorIndex damageColorIndex;
        public float range = 20f;
        public float damageCoefficientPerBounce = 1f;
        public int targetsToFindPerBounce = 1;
        public DamageTypeCombo damageType = DamageType.Generic;
        private bool failedToKill;
        private BullseyeSearch search;
        public List<VineOrb.SplitDebuffInformation> splitDotInformation;

        public static List<VineOrb.SplitDebuffInformation> GetSplitDotInformation(CharacterBody victimBody, CharacterBody attackerBody)
        {
            List<VineOrb.SplitDebuffInformation> list = new List<VineOrb.SplitDebuffInformation>();
            DotController dotController = DotController.FindDotController(victimBody.gameObject);
            foreach (BuffIndex buffIndex in BuffCatalog.debuffAndDotsIndicesExcludingNoxiousThorns)
            {
                BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
                if (!buffDef.isDOT || dotController == null)
                    continue;
                int buffCount = victimBody.GetBuffCount(buffDef);
                if (buffCount > 0)
                {
                    int count = Mathf.CeilToInt((float)buffCount * 0.5f);
                    bool isTimed = false;
                    float duration = 0f;

                    DotController.DotIndex dotDefIndex = DotController.GetDotDefIndex(buffDef);
                    isTimed = dotController.GetDotStackTotalDurationForIndex(dotDefIndex, out duration);

                    VineOrb.SplitDebuffInformation item = new VineOrb.SplitDebuffInformation
                    {
                        attacker = attackerBody.gameObject,
                        attackerMaster = attackerBody.master,
                        index = buffIndex,
                        isTimed = isTimed,
                        duration = duration,
                        count = count
                    };
                    list.Add(item);
                }
            }
            return list;
        }

        public override void Begin()
        {
            string path = "Prefabs/Effects/OrbEffects/CrocoDiseaseOrbEffect";
            base.duration = 0.6f;
            this.targetsToFindPerBounce = 2;

            EffectData effectData = new EffectData
            {
                origin = this.origin,
                genericFloat = base.duration
            };
            effectData.SetHurtBoxReference(this.target);
            EffectManager.SpawnEffect(OrbStorageUtility.Get(path), effectData, true);
        }
        public override void OnArrival()
        {
            if (this.target)
            {
                HealthComponent healthComponent = this.target.healthComponent;
                if (healthComponent)
                {
                    DamageInfo damageInfo = new DamageInfo();
                    damageInfo.damage = this.damageValue;
                    damageInfo.attacker = this.attacker;
                    damageInfo.inflictor = this.inflictor;
                    damageInfo.force = Vector3.zero;
                    damageInfo.crit = this.isCrit;
                    damageInfo.procChainMask = this.procChainMask;
                    damageInfo.procCoefficient = this.procCoefficient;
                    damageInfo.position = this.target.transform.position;
                    damageInfo.damageColorIndex = this.damageColorIndex;
                    damageInfo.damageType = this.damageType;
                    healthComponent.TakeDamage(damageInfo);
                    if (splitDotInformation != null && splitDotInformation.Count > 0)
                    {
                        if (debuffBlacklistedObjects == null || debuffBlacklistedObjects.Count == 0 || !debuffBlacklistedObjects.Contains(healthComponent))
                        {
                            ApplySplitDebuffs(healthComponent);
                        }
                    }
                    GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                    GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
                }
                this.failedToKill |= (!healthComponent || healthComponent.alive);
                if (this.bouncesRemaining > 0)
                {
                    for (int i = 0; i < this.targetsToFindPerBounce; i++)
                    {
                        if (this.bouncedObjects != null)
                        {
                            this.bouncedObjects.Add(this.target.healthComponent);
                        }
                        HurtBox hurtBox = this.PickNextTarget(this.target.transform.position);
                        if (hurtBox)
                        {
                            DiseaseOrb diseaseOrb = new DiseaseOrb();
                            diseaseOrb.splitDotInformation = this.splitDotInformation;
                            diseaseOrb.debuffBlacklistedObjects = this.debuffBlacklistedObjects;
                            diseaseOrb.search = this.search;
                            diseaseOrb.origin = this.target.transform.position;
                            diseaseOrb.target = hurtBox;
                            diseaseOrb.attacker = this.attacker;
                            diseaseOrb.inflictor = this.inflictor;
                            diseaseOrb.teamIndex = this.teamIndex;
                            diseaseOrb.damageValue = this.damageValue * this.damageCoefficientPerBounce;
                            diseaseOrb.bouncesRemaining = this.bouncesRemaining - 1;
                            diseaseOrb.isCrit = this.isCrit;
                            diseaseOrb.bouncedObjects = this.bouncedObjects;
                            diseaseOrb.procChainMask = this.procChainMask;
                            diseaseOrb.procCoefficient = this.procCoefficient;
                            diseaseOrb.damageColorIndex = this.damageColorIndex;
                            diseaseOrb.damageCoefficientPerBounce = this.damageCoefficientPerBounce;
                            diseaseOrb.speed = this.speed;
                            diseaseOrb.range = this.range;
                            diseaseOrb.damageType = this.damageType;
                            diseaseOrb.failedToKill = this.failedToKill;
                            OrbManager.instance.AddOrb(diseaseOrb);
                        }
                    }
                    return;
                }
                if (!this.failedToKill)
                {
                    Action<DiseaseOrb> action = DiseaseOrb.onLightningOrbKilledOnAllBounces;
                    if (action == null)
                    {
                        return;
                    }
                    action(this);
                }
            }
        }

        private void ApplySplitDebuffs(HealthComponent hc)
        {
            CharacterBody body = hc.body;
            foreach (VineOrb.SplitDebuffInformation splitDotInfo in this.splitDotInformation)
            {
                BuffDef buffDef = BuffCatalog.GetBuffDef(splitDotInfo.index);
                if (buffDef.isDOT)
                {
                    DotController.DotIndex dotDefIndex = DotController.GetDotDefIndex(buffDef);
                    DotController.DotDef dotDef = DotController.GetDotDef(dotDefIndex);
                    InflictDotInfo inflictDotInfo = new InflictDotInfo
                    {
                        attackerObject = splitDotInfo.attacker,
                        victimObject = body.gameObject,
                        damageMultiplier = 1f,
                        dotIndex = dotDefIndex,
                        duration = Mathf.Max(splitDotInfo.duration, dotDef.interval)
                    };
                    for (int i = 0; i < splitDotInfo.count; i++)
                    {
                        DotController.InflictDot(ref inflictDotInfo);
                    }
                }
                GlobalEventManager.ProcDeathMark(this.target.gameObject, body, splitDotInfo.attackerMaster);
            }
            if(debuffBlacklistedObjects != null)
                debuffBlacklistedObjects.Add(hc);
            Util.PlaySound("Play_item_proc_triggerEnemyDebuffs", body.gameObject);
        }

        public HurtBox PickNextTarget(Vector3 position)
        {
            if (this.search == null)
            {
                this.search = new BullseyeSearch();
            }
            this.search.searchOrigin = position;
            this.search.searchDirection = Vector3.zero;
            this.search.teamMaskFilter = TeamMask.allButNeutral;
            this.search.teamMaskFilter.RemoveTeam(this.teamIndex);
            this.search.filterByLoS = false;
            this.search.sortMode = BullseyeSearch.SortMode.Distance;
            this.search.maxDistanceFilter = this.range;
            this.search.RefreshCandidates();
            HurtBox hurtBox = (from v in this.search.GetResults()
                               where !this.bouncedObjects.Contains(v.healthComponent)
                               select v).FirstOrDefault<HurtBox>();
            if (hurtBox)
            {
                this.bouncedObjects.Add(hurtBox.healthComponent);
            }
            return hurtBox;
        }
    }
}
