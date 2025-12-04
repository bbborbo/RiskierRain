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

namespace SwanSongExtended.Items
{
    class LightningAttractor : ItemBase<LightningAttractor>
    {
        public static BuffDef forkReadyBuff;
        public static BuffDef forkRechargeBuff;
        public static BuffDef forkRepeatHitBuff;
        public static BuffDef forkedBuff;
        public static float forkRecharge = 5;
        public static float forkDuration = 3;
        public static int forkAttackRequirement = 6;
        public static float forkTotalDamageBase = 1.5f;
        public static float forkTotalDamageStack = 1.5f;
        public static float forkStrikeRange = 25;
        public override string ItemName => "Copper Fork";

        public override string ItemLangTokenName => "LIGHTNINGATTRACTOR";

        public override string ItemPickupDesc => "Attract lightning on repeated hits.";

        public override string ItemFullDescription => 
            $"Damage from any {DamageColor("skill or equipment")} also sticks the enemy with " +
            $"a copper fork for {UtilityColor($"{forkDuration}")} seconds. " +
            $"Repeatedly attacking a forked enemy resets the fork's duration " +
            $"and attracts lightning, {DamageColor("Stunning")} a nearby enemy " +
            $"for {DamageColor(ConvertDecimal(forkTotalDamageStack) + " TOTAL damage")} " +
            $"{StackText($"+{ConvertDecimal(forkTotalDamageStack)}")}. " +
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
            forkedBuff = Content.CreateAndAddBuff("bdForked",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texAttackIcon_png).WaitForCompletion(),
                new Color32(255, 125, 0, 255), false, false);
            forkedBuff.isHidden = true;
            forkedBuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
        }

        public override void Hooks()
        {
            GetHitBehavior += ForkOnHit;
            On.RoR2.CharacterBody.OnInventoryChanged += AddForkItemBehavior;
        }

        private void AddForkItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<LightningAttractorBehavior>(GetCount(self));
            }
        }

        private void ForkOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            int itemCount = GetCount(attackerBody);
            if (itemCount <= 0 || !NetworkServer.active)
                return;

            if (!damageInfo.damageType.IsDamageSourceSkillBased && damageInfo.damageType.damageSource != DamageSource.Equipment)
                return;

            int forkHits = victimBody.GetBuffCount(forkRepeatHitBuff);
            bool forked = victimBody.HasBuff(forkedBuff);
            bool forkReady = attackerBody.HasBuff(forkReadyBuff);
            //if the attacker can fork or if the victim is already forked
            if(forkReady || forked)
            {
                if (!victimBody.healthComponent.alive)
                {
                    DoForkLightningStrike(attackerBody, damageInfo, victimBody, itemCount);
                    return;
                }

                //if the attacker can fork, take the fork
                if (forkReady)
                {
                    attackerBody.RemoveBuff(forkReadyBuff);
                }
                //refresh fork cooldown always
                attackerBody.AddTimedBuff(forkRechargeBuff, forkRecharge);
                victimBody.AddTimedBuff(forkedBuff, forkDuration);

                //if the next hit goes over the attack requirement, do lightning
                //otherwise, extend all fork hit counts
                //i do it this way so the fork attack count always stays at or above 1
                int overspillHitCount = CalculateOverspillCount(damageInfo.damage, attackerBody.damage);// Mathf.FloorToInt(damageInfo.damage / (attackerBody.damage * 2f));
                int CalculateOverspillCount(float attackDamage, float baseDamage)
                {
                    int count = 0;
                    float idek = baseDamage * 2;
                    while (attackDamage >= idek)
                    {
                        count++;
                        attackDamage -= idek;
                        idek += baseDamage * 2;
                    }
                    return count;
                }
                if(forkHits + overspillHitCount >= forkAttackRequirement)
                {
                    int a = forkAttackRequirement;
                    a -= forkHits;
                    overspillHitCount -= a;
                    if (overspillHitCount >= forkAttackRequirement)
                        overspillHitCount = forkAttackRequirement - 1;

                    victimBody.ClearTimedBuffs(forkRepeatHitBuff);
                    forkHits = 0;
                    //do lightning
                    DoForkLightningStrike(attackerBody, damageInfo, victimBody, itemCount);
                }

                for (int l = 0; l < victimBody.timedBuffs.Count; l++)
                {
                    TimedBuff timedBuff = victimBody.timedBuffs[l];
                    if (timedBuff.buffIndex == forkRepeatHitBuff.buffIndex)
                    {
                        if (timedBuff.timer < forkDuration)
                        {
                            timedBuff.timer = forkDuration;
                            timedBuff.totalDuration = forkDuration;
                        }
                    }
                }
                for (int i = 0; i <= overspillHitCount; i++)
                {
                    //add a fork hit
                    victimBody.AddTimedBuff(forkRepeatHitBuff, forkDuration);
                }
            }
        }

        private static void DoForkLightningStrike(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody, int itemCount)
        {
            float range = forkStrikeRange;// overloadingSmiteRangeBase + victimBody.radius * overloadingSmiteRangePerRadius;
            float baseDamage = damageInfo.damage;
            float smiteDamageCoefficient = forkTotalDamageBase + forkTotalDamageStack * (itemCount - 1);
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
    public class LightningAttractorBehavior : CharacterBody.ItemBehavior
    {

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

        private void OnDisable()
        {
            this.body.RemoveBuff(LightningAttractor.forkReadyBuff);
            this.body.ClearTimedBuffs(LightningAttractor.forkRechargeBuff);
        }
    }
}
