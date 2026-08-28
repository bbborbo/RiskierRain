using FruityElites.Modules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static MoreStats.StatHooks;
using HG;

namespace FruityElites.EliteReworks
{
    class BlazingReworks : EliteReworkBase<BlazingReworks>
    {
        public static GameObject flameAuraPrefab;
        public static GameObject flameAuraMaxRangeIndicatorPrefab;

        public static BuffDef accelerantBuff;
        public static float accelerantDuration = 8f;
        public static float accelerantAttackSpeed = 0.25f;
        public static float accelerantMovementSpeed = 0.0f;
        public static float accelerantIgniteChance = 100f;

        public static float flameAuraRange = 18f;
        public static float flameAuraGrowthPerSecond = 0.25f;
        public static float flameAuraDamageInterval = 0.5f;

        public static float flameAuraIgniteTotalDamageBase = 15f;
        public static float flameAuraIgniteTotalDamageLevel = 0.4f;


        [AutoConfig("Fire Trail Damage Per Second", "Scales with ambient level", 80f)]
        public static float fireTrailDPS = 80f; //1.5f
        [AutoConfig("Fire Trail Base Radius", "Vanilla is 3.0", 6f)]
        public static float fireTrailBaseRadius = 6f; //3f
        [AutoConfig("Fire Trail Lifetime", "Might not work, vanilla is 3.0", 100f)]
        public static float fireTrailLifetime = 100f; //3f
        public override string eliteName => "Blazing";

        public override void Init()
        {
            accelerantBuff = Content.CreateAndAddBuff("bdBlazingAccelerant", null, Color.red, false, false);
            base.Init();
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.UpdateFireTrail += BlazingFireTrailChanges;
            On.RoR2.CharacterBody.AddOrRemoveEliteItemBehavior += AddBlazingItemBehavior;
            GetStatCoefficients += AccelerantStats;
            GetMoreStatCoefficients += AccelerantMoreStats;
        }

        private void AccelerantMoreStats(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.HasBuff(accelerantBuff))
            {
                args.burnChanceOnHit += accelerantIgniteChance;
            }
        }

        private void AccelerantStats(CharacterBody sender, StatHookEventArgs args)
        {
            if(sender.HasBuff(accelerantBuff))
            {
                args.attackSpeedMultAdd += accelerantAttackSpeed;
                args.moveSpeedMultAdd += accelerantMovementSpeed;
            }
        }

        private void AddBlazingItemBehavior(On.RoR2.CharacterBody.orig_AddOrRemoveEliteItemBehavior orig, CharacterBody self, BuffDef buffDef, bool add)
        {
            if (buffDef == RoR2Content.Buffs.AffixRed)
            {
                self.AddItemBehavior<AffixRedBehavior>(add ? 1 : 0);
                return;
            }
            orig(self, buffDef, add);
        }

        private void BlazingFireTrailChanges(On.RoR2.CharacterBody.orig_UpdateFireTrail orig, CharacterBody self)
        {
            orig(self);
            return;

            if (self.fireTrail)
            {
                self.fireTrail.radius = fireTrailBaseRadius * self.radius;
                self.fireTrail.damagePerSecond = (1 + 0.2f * self.level) * fireTrailDPS;
                //self.fireTrail.pointLifetime = fireTrailLifetime;
            }
        }
    }
    public class AffixRedBehavior : CharacterBody.ItemBehavior, IOnTakeDamageServerReceiver, IOnKilledServerReceiver
    {
        float flameAuraMaxRange => body.bestFitRadius + BlazingReworks.flameAuraRange;
        float flameAuraDamageInterval => BlazingReworks.flameAuraDamageInterval / body.attackSpeed;
        private const float rangeIndicatorScale = 1;
        private const float auraScale = 1;
        private const float minRange = 2;
        private float currentRange = 0;
        private float damageStopwatch = 0;
        public GameObject auraInstance;
        public GameObject rangeIndicatorInstance;

        SphereSearch sphereSearch;

        void Start()
        {
            body?.healthComponent?.AddOnTakeDamageServerReceiver(this);

            auraInstance = Instantiate(BlazingReworks.flameAuraPrefab, body.transform);
            SetAuraRange(0);

            rangeIndicatorInstance = Instantiate(BlazingReworks.flameAuraMaxRangeIndicatorPrefab, body.transform);
            rangeIndicatorInstance.transform.localScale = Vector3.one * flameAuraMaxRange * rangeIndicatorScale;

            if (NetworkServer.active)
            {
                this.sphereSearch = new SphereSearch();
                this.sphereSearch.origin = body.corePosition;
                this.sphereSearch.mask = LayerIndex.entityPrecise.mask;
                this.sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
                this.sphereSearch.radius = currentRange;
            }
        }
        void OnDestroy()
        {
            body?.healthComponent?.RemoveOnTakeDamageServerReceiver(this);
            Destroy(auraInstance);
            Destroy(rangeIndicatorInstance);
        }
        public void OnTakeDamageServer(DamageReport damageReport)
        {
            if(damageReport.damageInfo.damageType.IsDamageSourceSkillBased || damageReport.damageInfo.damageType.damageSource == DamageSource.Equipment)
                ResetFlameAura();
        }

        void Update()
        {
            if ((body.outOfDanger || currentRange >= minRange) && currentRange < flameAuraMaxRange)
            {
                if (currentRange < minRange)
                    damageStopwatch = flameAuraDamageInterval;
                if (currentRange < flameAuraMaxRange)
                    SetAuraRange(MathF.Min(flameAuraMaxRange, currentRange + BlazingReworks.flameAuraGrowthPerSecond * Time.deltaTime));
            }
        }

        void FixedUpdate()
        {
            if (NetworkServer.active && currentRange >= minRange)
            {
                damageStopwatch -= Time.fixedDeltaTime;
                if (damageStopwatch < 0)
                {
                    //get targets
                    List<HurtBox> enemies = GetNearbyTargets(currentRange, body.teamComponent.teamIndex, true);

                    float totalDamage = (BlazingReworks.flameAuraIgniteTotalDamageBase + BlazingReworks.flameAuraIgniteTotalDamageLevel * (body.level - 1));
                    Inventory inv = body.inventory;
                    while (damageStopwatch < 0)
                    {
                        damageStopwatch += flameAuraDamageInterval;

                        foreach(HurtBox target in enemies)
                        {
                            InflictDotInfo inflictDotInfo = new InflictDotInfo
                            {
                                attackerObject = body.gameObject,
                                victimObject = target.healthComponent.gameObject,
                                totalDamage = new float?(totalDamage),
                                damageMultiplier = 1f,
                                dotIndex = DotController.DotIndex.Burn,
                                maxStacksFromAttacker = null
                            };
                            StrengthenBurnUtils.CheckDotForUpgrade(inv, ref inflictDotInfo);
                            DotController.InflictDot(ref inflictDotInfo);
                        }
                    }
                }
            }
        }

        public void OnKilledServer(DamageReport damageReport)
        {
            //get targets
            List<HurtBox> enemies = GetNearbyTargets(flameAuraMaxRange, damageReport.attackerTeamIndex, false);

            //buff targets/send buff orb
            foreach (HurtBox target in enemies)
            {
                target.healthComponent.body.AddTimedBuff(BlazingReworks.accelerantBuff, BlazingReworks.accelerantDuration);
            }
        }

        List<HurtBox> GetNearbyTargets(float radius, TeamIndex targetTeam, bool invertTeam)
        {
            List<HurtBox> candidates = new List<HurtBox>();
            sphereSearch.origin = this.transform.position;
            sphereSearch.radius = this.currentRange;

            TeamMask mask = default(TeamMask);
            if (invertTeam)
                mask = TeamMask.GetEnemyTeams(targetTeam);
            else
                mask.AddTeam(targetTeam);

            sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(mask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(candidates);
            return candidates;
        }

        void SetAuraRange(float newRange)
        {
            currentRange = newRange;
            bool shouldBeActive = currentRange > minRange;
            bool isActive = auraInstance.activeSelf;
            if(isActive != shouldBeActive)
            {
                auraInstance.SetActive(shouldBeActive);
            }
            if (shouldBeActive)
            {
                auraInstance.transform.localScale = Vector3.one * rangeIndicatorScale * currentRange;
            }
        }

        void ResetFlameAura()
        {
            SetAuraRange(0);
            damageStopwatch = flameAuraDamageInterval;
        }
    }
}
