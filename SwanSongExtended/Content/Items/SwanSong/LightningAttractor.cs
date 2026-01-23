using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SwanSongExtended.Modules.Language.Styling;
using static MoreStats.OnHit;
using UnityEngine.Networking;
using RoR2.Orbs;
using static RoR2.CharacterBody;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class LightningAttractor : ItemBase<LightningAttractor>
    {
        public override bool isEnabled => true;
        public static BuffDef forkReadyBuff;
        public static BuffDef forkRechargeBuff;
        public static BuffDef forkRepeatHitBuff;
        public static BuffDef forkedBuffHidden;
        public static float forkRecharge = 5;
        public static float forkDuration = 3;
        public static int forkAttackRequirement = 6;
        public static float forkBaseDamageBase = 6.0f;
        public static float forkBaseDamageStack = 4.0f;
        public static float forkStrikeRange = 25;
        public override string ItemName => "Copper Fork";

        public override string ItemLangTokenName => "LIGHTNINGATTRACTOR";

        public override string ItemPickupDesc => "Attract lightning on repeated hits.";

        public override string ItemFullDescription => 
            $"Damage from any {DamageColor("skill or equipment")} also sticks the enemy with " +
            $"a copper fork for {UtilityColor($"{forkDuration}")} seconds. " +
            $"Repeatedly attacking a forked enemy resets the fork's duration " +
            $"and attracts lightning, {DamageColor("Stunning")} a nearby enemy " +
            $"for {DamageColor(ConvertDecimal(forkBaseDamageBase) + " base damage")} " +
            $"{StackText($"+{ConvertDecimal(forkBaseDamageStack)}")}. " +
            $"1 max, recharges {UtilityColor($"{forkRecharge}s")} after the fork expires.";

        public override string ItemLore =>
@"New, from CuCo!

The CopperWare Utensil set offers countless benefits over your mundane Stainless Steel silverware.

A stylish reddish-brown color to match your tableware, and a perfect match for your CuCo CopperWare Pots and Pans set!

Supplemental Copper intake directly from your eating utensils!

Easy cleaning! A rub down with any household acid like Vinegar or Lemon Juice will bring your CopperWare back to a factory shine!

Try CopperWare today!

PRODUCT WARNINGS

To avoid risk of galvanic corrosion, do not allow CopperWare in contact with other metal surfaces, especially in the presence of electrolytes like salt.

Do not use CopperWare utensils in cooking. This presents a significant burn risk and may leach copper into the dish.

Customers over the age of 65 are not recommended to use CopperWare due to links between copper and Alzheimer's Disease.

Due to copper's high electrical condicuctivity, it is recommended not to use any CopperWare products in close proximity to electrical currents or appliances.

To mitigate risk of fatal electrocution, please do not use CopperWare products when dining outside.

With your agreement to purchase and use this product, CuCo is released of liability from any consumer complaints relating to the nature of copper kitchenware.";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.AIBlacklist };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }
        public override void Init()
        {
            base.Init();
            forkReadyBuff = Content.CreateAndAddBuff("bdForkReady",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion(),
                Color.yellow, false, false);
            forkRechargeBuff = Content.CreateAndAddBuff("bdForkRecharge",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion(),
                Color.gray, false, false);
            forkRepeatHitBuff = Content.CreateAndAddBuff("bdForkStack",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion(),
                new Color32(255, 125, 0, 255), true, false);
            forkRepeatHitBuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            forkedBuffHidden = Content.CreateAndAddBuff("bdForked",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion(),
                new Color32(255, 125, 0, 255), false, false);
            forkedBuffHidden.isHidden = true;
            forkedBuffHidden.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
        }

        public override void Hooks()
        {
        }

        public static void DoForkLightningStrike(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody, int itemCount)
        {
            float range = forkStrikeRange;// overloadingSmiteRangeBase + victimBody.radius * overloadingSmiteRangePerRadius;
            float baseDamage = attackerBody.damage;// damageInfo.damage;
            float smiteDamageCoefficient = forkBaseDamageBase + forkBaseDamageStack * (itemCount - 1);
            ProcChainMask procChainMask6 = damageInfo.procChainMask;
            //procChainMask6.AddProc(ProcType.LightningStrikeOnHit);

            SphereSearch sphereSearch = new SphereSearch
            {
                mask = LayerIndex.entityPrecise.mask,
                origin = victimBody.transform.position,
                queryTriggerInteraction = QueryTriggerInteraction.Collide,
                radius = range
            };

            TeamMask teamMask = TeamMask.GetEnemyTeams(attackerBody.teamComponent.teamIndex);
            List<HurtBox> hurtBoxesList = new List<HurtBox>();

            sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(hurtBoxesList);

            int i = UnityEngine.Random.Range(0, hurtBoxesList.Count);
            HurtBox targetHurtBox = hurtBoxesList[i];
            SetStateOnHurt component = targetHurtBox.healthComponent.GetComponent<SetStateOnHurt>();
            if (component)
            {
                component.SetStun(1);
            }

            OrbManager.instance.AddOrb(new SimpleLightningStrikeOrb
            {
                attacker = attackerBody.gameObject,
                damageColorIndex = DamageColorIndex.Default,
                damageValue = baseDamage * smiteDamageCoefficient,
                isCrit = damageInfo.crit,
                procChainMask = procChainMask6,
                procCoefficient = 1f,
                target = targetHurtBox,
                damageType = DamageType.Stun1s
            });
        }
    }
    public class LightningAttractorBehavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver
    {

        [ItemDefAssociation(useOnServer = true, useOnClient = true)]

        private void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;
            int buffCount = body.GetBuffCount(LightningAttractor.forkReadyBuff);
            if (!body.HasBuff(LightningAttractor.forkRechargeBuff) && !body.HasBuff(LightningAttractor.forkReadyBuff))
            {
                body.AddBuff(LightningAttractor.forkReadyBuff);
            }
        }
        public void OnDamageDealtServer(DamageReport damageReport)
        {
            DamageInfo damageInfo = damageReport.damageInfo;
            if (!damageInfo.damageType.IsDamageSourceSkillBased && damageInfo.damageType.damageSource != DamageSource.Equipment)
                return;

            CharacterBody victimBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (victimBody == null || attackerBody == null)
                return;

            int forkHits = victimBody.GetBuffCount(LightningAttractor.forkRepeatHitBuff);
            bool isVictimForkedInternal = victimBody.HasBuff(LightningAttractor.forkedBuffHidden);
            bool isAttackerReadyToFork = attackerBody.HasBuff(LightningAttractor.forkReadyBuff);
            //if the attacker can fork or if the victim is already forked
            if (isAttackerReadyToFork || isVictimForkedInternal)
            {
                if (!victimBody.healthComponent.alive)
                {
                    LightningAttractor.DoForkLightningStrike(attackerBody, damageInfo, victimBody, stack);
                    return;
                }

                //if the attacker can fork, take the fork
                if (isAttackerReadyToFork)
                {
                    attackerBody.RemoveBuff(LightningAttractor.forkReadyBuff);
                }
                //refresh fork cooldown always
                attackerBody.AddTimedBuff(LightningAttractor.forkRechargeBuff, LightningAttractor.forkRecharge);
                victimBody.AddTimedBuff(LightningAttractor.forkedBuffHidden, LightningAttractor.forkDuration);

                //if the next hit goes over the attack requirement, do lightning
                //otherwise, extend all fork hit counts
                //i do it this way so the fork attack count always stays at or above 1
                float damageCoefficient = damageInfo.damage / attackerBody.damage;
                int overspillHitCount = Tools.CountOverspillFibonacci(damageCoefficient, 1f);// Mathf.FloorToInt(damageInfo.damage / (attackerBody.damage * 2f));
                if (damageInfo.procCoefficient < 1)
                {
                    float temp = (float)overspillHitCount * damageInfo.procCoefficient;
                    overspillHitCount = (int)Math.Truncate(temp);
                    if (Util.CheckRoll0To1(temp - overspillHitCount, attackerBody.master))
                        overspillHitCount += 1;
                }

                if (forkHits + overspillHitCount >= LightningAttractor.forkAttackRequirement)
                {
                    int a = LightningAttractor.forkAttackRequirement;
                    a -= forkHits;
                    overspillHitCount -= a;
                    if (overspillHitCount >= LightningAttractor.forkAttackRequirement)
                        overspillHitCount = LightningAttractor.forkAttackRequirement - 1;

                    victimBody.ClearTimedBuffs(LightningAttractor.forkRepeatHitBuff);
                    forkHits = 0;
                    //do lightning
                    LightningAttractor.DoForkLightningStrike(attackerBody, damageInfo, victimBody, stack);
                }

                for (int l = 0; l < victimBody.timedBuffs.Count; l++)
                {
                    TimedBuff timedBuff = victimBody.timedBuffs[l];
                    if (timedBuff.buffIndex == LightningAttractor.forkRepeatHitBuff.buffIndex)
                    {
                        if (timedBuff.timer < LightningAttractor.forkDuration)
                        {
                            timedBuff.timer = LightningAttractor.forkDuration;
                            timedBuff.totalDuration = LightningAttractor.forkDuration;
                        }
                    }
                }
                for (int i = 0; i <= overspillHitCount; i++)
                {
                    //add a fork hit
                    victimBody.AddTimedBuff(LightningAttractor.forkRepeatHitBuff, LightningAttractor.forkDuration);
                }
            }
        }

        private void OnDisable()
        {
            if (!NetworkServer.active)
                return;
            this.body.RemoveBuff(LightningAttractor.forkReadyBuff);
            this.body.ClearTimedBuffs(LightningAttractor.forkRechargeBuff);
        }
    }
}
