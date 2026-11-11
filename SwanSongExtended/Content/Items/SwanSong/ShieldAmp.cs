using R2API;
using RainrotSharedUtils.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static MoreStats.OnHit;
using static R2API.RecalculateStatsAPI;
using static RainrotSharedUtils.Styling;
using static RainrotSharedUtils.Extensions;
using RoR2.Orbs;

namespace SwanSongExtended.Items
{
    class ShieldAmp : ItemBase<ShieldAmp>
    {
        public override bool isEnabled => true;
        public static float shieldFlatBase = 40f;
        public static float shieldDrainFractionBase = 0.4f;
        public static float shieldDrainFractionStack = -0.08f;
        public static float amplifyDamageIncreaseBase = 4f;
        public static float amplifyDamageIncreaseStack = 2.5f;
        public static float amplifyDamageMultiplierForFullShield = 2f;
        public static float strikeRange = 18f;
        public static int strikeTargetsBase = 2;
        public static float strikeSpeed = 25f;
        public static ModdedProcType AmpedProc;
        public override string ItemName => "Snapping Stone";

        public override string ItemLangTokenName => "SHIELDAMP";

        public override string ItemPickupDesc => "While shields are full, your next attack drains a small amount of shield for amplified damage.";

        public override string ItemFullDescription => $"Gain {HealingColor(shieldFlatBase + " shield")}. " +
            $"While shields are full, {DamageColor("amplify")} damage from skills. " +
            $"{DamageColor("Amplified")} hits {DamageColor($"drain {shieldDrainFractionBase.AsPercent()}")} of your max shield " +
            $"{StackText($"{shieldDrainFractionStack.AsPercent()}")}, " +
            $"dealing {DamageColor($"+{amplifyDamageIncreaseBase.AsPercent()} BASE damage")} " +
            $"{StackText($"+{amplifyDamageIncreaseStack.AsPercent()}")}. " +
            $"Amplified damage spreads to {DamageColor(strikeTargetsBase.ToString())} nearby enemies.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => LoadDropPrefab("mdlShieldAmp");

        public override Sprite ItemIcon => LoadItemIcon("texIconShieldAmp");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            base.Init();
            AmpedProc = ProcTypeAPI.ReserveProcType();
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += ShieldAmpOnTakeDamage;
            //GetHitBehavior += ShieldAmpOnHit;
            GetStatCoefficients += ShieldAmpStats;
        }

        private void ShieldAmpStats(CharacterBody sender, StatHookEventArgs args)
        {
            int stack = GetCount(sender);
            if(stack > 0)
            {
                args.baseShieldAdd += shieldFlatBase;// sender.maxHealth * shieldPercentBase;//(shieldPercentBase + (shieldPercentStack * (itemCount - 1)));
            }
        }

        private void ShieldAmpOnTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            TriggerShieldAmp(self, damageInfo);
            orig(self, damageInfo);
        }
        private static void TriggerShieldAmp(HealthComponent self, DamageInfo damageInfo)
        {
            if (NetworkServer.active == false)
                return;
            if (damageInfo.dotIndex != DotController.DotIndex.None)
                return;
            if (damageInfo.damageType.IsDamageSourceSkillBased == false || damageInfo.procChainMask.HasModdedProc(AmpedProc) == true)
                return;

            if (self.body.IsInvincible())
                return;

            CharacterBody attackerBody = null;
            if (damageInfo.attacker == null || damageInfo.attacker.TryGetComponent(out attackerBody) == false)
                return;
            if (attackerBody.IsInvincible())
                return;

            int stack = instance.GetCount(attackerBody);
            if (stack <= 0)
                return;

            HealthComponent healthComponent = attackerBody.healthComponent;
            float maxShield = attackerBody.maxShield;
            if (healthComponent.shield < maxShield - 1)
                return;

            float amplifyBaseDamage = amplifyDamageIncreaseBase + amplifyDamageIncreaseStack * (stack - 1);
            float amplifyDamageScale = attackerBody.maxShield > shieldFlatBase ? Mathf.Lerp(1, amplifyDamageMultiplierForFullShield, maxShield / healthComponent.fullCombinedHealth) : 1;
            float amplifyDamage = attackerBody.damage * amplifyBaseDamage * amplifyDamageScale;

            float drainFraction = shieldDrainFractionBase * Mathf.Pow(1 + shieldDrainFractionStack, stack);
            DrainShield(healthComponent, maxShield * drainFraction);


            EffectManager.SpawnEffect(Fuse.fuseNovaEffectPrefab, new EffectData
            {
                origin = self.body.corePosition,
                scale = self.body.bestFitRadius
            }, true);
            damageInfo.damage += amplifyDamage;
            //OrbManager.instance.AddOrb(new LightningStrikeOrb
            //{
            //    attacker = damageInfo.attacker,
            //    damageColorIndex = DamageColorIndex.Default,
            //    damageValue = 0,
            //    isCrit = false,
            //    procChainMask = damageInfo.procChainMask,
            //    procCoefficient = 0.0f,
            //    target = self.body.mainHurtBox
            //});
            LightningStrikeNearby(self.body, attackerBody, damageInfo.damage, damageInfo, strikeTargetsBase);

            //NebulaPickup.CreateBoosterPickup(damageInfo.position, attackerBody.teamComponent.teamIndex, RainrotSharedUtils.Assets.sparkBoosterObject, 2);
        }

        private static void LightningStrikeNearby(CharacterBody victim, CharacterBody attacker, float amplifyDamage, DamageInfo damageInfo, int targetCount)
        {
            if (NetworkServer.active == false)
                return;

            SphereSearch sphereSearch = new SphereSearch
            {
                mask = LayerIndex.entityPrecise.mask,
                origin = victim.transform.position,
                queryTriggerInteraction = QueryTriggerInteraction.Collide,
                radius = strikeRange
            };

            TeamMask teamMask = TeamMask.GetEnemyTeams(TeamIndex.Player);
            List<HurtBox> hurtBoxesList = new List<HurtBox>();

            sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().OrderCandidatesByDistance().GetHurtBoxes(hurtBoxesList);

            int hurtBoxCount = hurtBoxesList.Count;
            int targetsSmited = 0;
            ProcChainMask mask = new ProcChainMask();
            mask.mask = damageInfo.procChainMask.mask;
            mask.AddModdedProc(AmpedProc);
            for(int i = 0; i < hurtBoxCount; i++)
            {
                HurtBox targetHurtBox = hurtBoxesList[i];
                HealthComponent healthComponent = targetHurtBox.healthComponent;
                CharacterBody enemyBody = healthComponent.body;
                if (enemyBody.isPlayerControlled)
                    continue;

                if (!enemyBody || enemyBody == victim)
                    continue;

                OrbManager.instance.AddOrb(new LightningOrb
                {
                    lightningType = LightningOrb.LightningType.EngiOverCharge,
                    attacker = attacker.gameObject,
                    damageColorIndex = DamageColorIndex.Default,
                    damageValue = amplifyDamage,
                    isCrit = damageInfo.crit,
                    procChainMask = mask,
                    procCoefficient = 0.5f,
                    target = targetHurtBox,
                    damageType = damageInfo.damageType,
                    speed = strikeSpeed
                });
                targetsSmited++;
                if (targetsSmited >= targetCount)
                    return;
            }
        }

        public static void DrainShield(HealthComponent healthComponent, float shieldToDrain)
        {
            if (NetworkServer.active)
            {
                float barrier = healthComponent.barrier;
                healthComponent.Networkbarrier = 0;

                DamageInfo damageInfo = new DamageInfo();
                damageInfo.damage = shieldToDrain;
                damageInfo.attacker = null;// healthComponent.gameObject;
                damageInfo.damageType = new DamageTypeCombo(DamageType.NonLethal, DamageTypeExtended.Generic, DamageSource.NoneSpecified);
                damageInfo.damageColorIndex = DamageColorIndex.Fragile;
                damageInfo.procCoefficient = 0;
                damageInfo.position = healthComponent.transform.position;


                TeamDef teamDef = TeamCatalog.GetTeamDef(healthComponent.body.teamComponent.teamIndex);
                if (teamDef != null)
                {
                    damageInfo.damage /= teamDef.friendlyFireScaling;
                }
                healthComponent.TakeDamage(damageInfo);

                healthComponent.Networkbarrier = barrier;
            }
        }

        private void ShieldAmpOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
        }
    }
}
