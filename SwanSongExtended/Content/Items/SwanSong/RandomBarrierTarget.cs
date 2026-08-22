using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static MoreStats.OnHit;
using static MoreStats.StatHooks;
using static SwanSongExtended.Modules.Language.Styling;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class RandomBarrierTarget : ItemBase<RandomBarrierTarget>
    {
        public static BuffDef harpoonDebuff;
        public static GameObject harpoonEffectPrefab;
        public override bool isEnabled => true; 

        public static float harpoonBarrierBase = 10;
        public static float harpoonBarrierStack = 10;
        public static float harpoonTargetTime = 15;
        public static float harpoonDecayReduction = 0.2f;
        public static float harpoonCritChanceBase = 20f;
        public static float harpoonCritChanceStack = 10f;

        public static Material harpoonTargetMaterial;

        public override string ItemName => "Crystal Ball";

        public override string ItemLangTokenName => "RANDOMBARRIERTARGET";

        public override string ItemPickupDesc => "Target a nearby enemy, gaining barrier and critical strike chance on hit.";

        public override string ItemFullDescription => 
            $"Reduce barrier decay by <style=cIsHealing>-{harpoonDecayReduction.AsPercent()}</style>. " +
            $"<style=cIsDamage>Targets</style> a random enemy. " +
            $"Attacking the targeted enemy grants a <style=cIsHealing>temporary barrier</style> " +
            $"for <style=cIsHealing>{harpoonBarrierBase} health</style> <style=cStack>(+{harpoonBarrierStack} per stack)</style> " +
            $"and increases {DamageColor("critical strike chance")} by " +
            $"{DamageColor($"+{harpoonCritChanceBase}%")} {StackText($"+{harpoonCritChanceStack}%")}.";

        public override string ItemLore =>
@"Order: Crystal Ball
Tracking Number: 66***********
Estimated Delivery: 5/6/2056
Shipping Method:  Return
Shipping Address: Arcane Suppliers Inc., The Floating Citadel, Neptune
Shipping Details:

I ordered this Omen Globe from you guys, and oh my gods, I am NEVER buying anything from this place again. This is a total fucking SCAM!!! 

I tried for HOURS, for this ball to show me ANYTHING, and never once was I granted a moment of divine clairvoyance. 

And believe me, I KNOW I'm enlightened, I'm WAY MORe enlightened than any of you DAMN WEASELS at Arcane SCAMMERS Inc. Don't fucking tell me I'm not enlightened enough. 

Your crystal, or should I say plastic, ball cost me more than my ENTIRE life savings. I DEMAND MY MONEY BACK, OR YOU WILL FACE DIRE CONSEQUENCES!!!";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override GameObject ItemModel => LoadDropPrefab("mdlRandomBarrierTarget");

        public override Sprite ItemIcon => LoadItemIcon("texIconRandomBarrierTarget");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Init()
        {
            harpoonDebuff = Content.CreateAndAddBuff(
                "bdHarpoonTargetDebuff",
                Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion(),
                new Color(0.9f, 0.7f, 0.1f),
                true,
                true);
            harpoonDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;

            GameObject deathMarkVisualEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathMark/DeathMarkEffect.prefab").WaitForCompletion();
            harpoonEffectPrefab = PrefabAPI.InstantiateClone(deathMarkVisualEffect, "HarpoonTargetVisualEffect");
            base.Init();
        }
        public override void Hooks()
        {
            harpoonTargetMaterial = CreateMatRecolor(new Color32(210, 140, 32, 100));

            GetMoreStatCoefficients += HarpoonDecay;
            //IL.RoR2.HealthComponent.TakeDamageProcess += HarpoonCritReroll;
            On.RoR2.HealthComponent.TakeDamageProcess += HarpoonRerollCrit;
        }

        private void HarpoonRerollCrit(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            damageInfo.crit = RerollCrit(damageInfo.crit);
            bool RerollCrit(bool isCrit)
            {
                if (isCrit == true)
                    return true;
                if (self.body == null)
                    return isCrit;
                if (self.body.HasBuff(harpoonDebuff) == false)
                {
                    return isCrit;
                }

                //idgaf
                if (damageInfo.attacker != null && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody))
                {
                    int crystalBallCt = GetCount(attackerBody);
                    if (crystalBallCt > 0)
                    {
                        float reroll = Util.ConvertAmplificationPercentageIntoReductionPercentage(GetStackValue(harpoonCritChanceBase, harpoonCritChanceStack, crystalBallCt));
                        if (Util.CheckRoll(reroll, attackerBody.master))
                            return true;
                    }
                }
                return false;
            }
            orig(self, damageInfo);
        }

        private void HarpoonCritReroll(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_critMultiplier"))
                && c.TryGotoPrev(MoveType.After,
                x => x.MatchLdfld<DamageInfo>(nameof(DamageInfo.crit))
                );
            if (!b1)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(HarpoonCritReroll), 1);
                return;
            }
            c.Emit(OpCodes.Ldarg_0); //healthcomponent self
            c.Emit(OpCodes.Ldarg_1); //damageinfo
            c.EmitDelegate<Func<bool, HealthComponent, DamageInfo, bool>>((isCrit, self, damageInfo) =>
            {
                Log.Error("a");
                if (isCrit == true)
                    return isCrit;
                if (self.body == null )
                    return isCrit;
                Log.Error("b");
                if (self.body.HasBuff(harpoonDebuff) == false)
                {
                    Log.Error("c");
                    return isCrit;
                }

                //idgaf
                if (damageInfo.attacker != null && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody))
                {
                    Log.Error("d");
                    int crystalBallCt = GetCount(attackerBody);
                    if(crystalBallCt > 0)
                    {
                        float reroll = Util.ConvertAmplificationPercentageIntoReductionPercentage(GetStackValue(harpoonCritChanceBase, harpoonCritChanceStack, crystalBallCt));
                        Log.Error(reroll);
                        if (Util.CheckRoll(reroll, attackerBody.master))
                            return true;
                    }
                }
                return false;
            });
        }

        private void HarpoonDecay(CharacterBody sender, MoreStatHookEventArgs args)
        {
            int count = GetCount(sender);
            if (count > 0)
                args.barrierDecayRatePercentIncreaseMult *= 1 - harpoonDecayReduction;
        }

        public static Material CreateMatRecolor(Color32 blueEquivalent)
        {
            var mat = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/Base/Huntress/matHuntressFlashExpanded.mat").WaitForCompletion());

            mat.SetColor("_TintColor", blueEquivalent);
            mat.SetInt("_Cull", 1);

            return mat;
        }

        private void RevokeHarpoonRights(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "MoveSpeedOnKill"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }
    }
    public class RandomBarrierTargetBehavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = true)]
        private static ItemDef GetItemDef() => RandomBarrierTarget.instance.ItemsDef;
        public static float baseHauntRadius = 35;
        public static float hauntRetryTime = 1;
        float hauntStopwatch = 0;
        void Start()
        {
            hauntStopwatch = RandomBarrierTarget.harpoonTargetTime;
        }

        public void OnDamageDealtServer(DamageReport damageReport)
        {
            if (!damageReport.victimBody.HasBuff(RandomBarrierTarget.harpoonDebuff))
                return;

            if (stack > 0)
            {
                float barrierGrant = RandomBarrierTarget.harpoonBarrierBase + RandomBarrierTarget.harpoonBarrierStack * (stack - 1);
                body.healthComponent.AddBarrierAuthority(barrierGrant * damageReport.damageInfo.procCoefficient);
            }
        }
        private void FixedUpdate()
        {
            hauntStopwatch += Time.fixedDeltaTime;
            if (hauntStopwatch >= RandomBarrierTarget.harpoonTargetTime)
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

                        DebuffEnemy(enemyBody);
                        hauntStopwatch -= RandomBarrierTarget.harpoonTargetTime;
                        return;
                    }
                    hauntStopwatch -= hauntRetryTime;
                }
            }
        }

        private void DebuffEnemy(CharacterBody enemyBody)
        {
            for (int n = 0; n < stack; n++)
            {
                enemyBody.AddTimedBuffAuthority(RandomBarrierTarget.harpoonDebuff.buffIndex, RandomBarrierTarget.harpoonTargetTime);
            }

            //thanks hifu <3
            Transform modelTransform = enemyBody.modelLocator?.modelTransform;
            if (modelTransform != null)
            {
                TemporaryOverlayInstance temporaryOverlay = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
                temporaryOverlay.duration = RandomBarrierTarget.harpoonTargetTime;
                temporaryOverlay.animateShaderAlpha = true;
                temporaryOverlay.alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);// AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlay.destroyComponentOnEnd = true;
                temporaryOverlay.originalMaterial = RandomBarrierTarget.harpoonTargetMaterial;
                temporaryOverlay.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
            }
        }

        private void OnDisable()
        {
            hauntStopwatch = 0;
        }

    }
}
