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
using System.Linq;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class RandomBarrierTarget : ItemBase<RandomBarrierTarget>
    {
        public static BuffDef harpoonDebuff;
        public static GameObject harpoonBodyEffectPrefab;
        public static GameObject harpoonTetherOriginBodyAttachmentPrefab;
        public static Material harpoonTargetMaterial;
        public override bool isEnabled => true; 

        public static float harpoonBarrierBase = 12;
        public static float harpoonBarrierStack = 12;
        public static float harpoonTargetTime = 4;
        public static float harpoonDecayReduction = 0.2f;
        public static float harpoonCritChanceBase = 20f;
        public static float harpoonCritChanceStack = 10f;
        public static float harpoonTargetInitialRange = 35f;
        public static float harpoonTargetLossRange = 50f;

        public override string ItemName => "Crystal Ball";

        public override string ItemLangTokenName => "RANDOMBARRIERTARGET";

        public override string ItemPickupDesc => "Highlight a nearby enemy. Gain barrier and critical strike chance on hit.";

        public override string ItemFullDescription => 
            $"Reduce barrier decay by <style=cIsHealing>-{harpoonDecayReduction.AsPercent()}</style>. " +
            $"<style=cIsDamage>Highlights</style> a random enemy. " +
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
                canStack: false,
                isDebuff: true,
                isHidden: true);
            harpoonDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;

            GameObject deathMarkVisualEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathMark/DeathMarkEffect.prefab").WaitForCompletion();
            harpoonBodyEffectPrefab = PrefabAPI.InstantiateClone(deathMarkVisualEffect, "HarpoonTargetVisualEffect");
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_SharedSuffering.SharedSufferingTetherOrigin_prefab, CreateBodyAttachment);
            harpoonTargetMaterial = CreateMatRecolor(new Color32(210, 140, 32, 170));
            base.Init();
        }

        private void CreateBodyAttachment(GameObject sharedSufferingAttachment)
        {
            harpoonTetherOriginBodyAttachmentPrefab = sharedSufferingAttachment.InstantiateClone("RandomBarrierTargetTetherOriginBodyAttachment", true);

            //actually i want this
            //if(harpoonTetherOriginBodyAttachmentPrefab.TryGetComponent(out SharedSufferingTetherManager sharedSufferingTether))
            //{
            //    UnityEngine.Object.Destroy(sharedSufferingTether);
            //}

            if(harpoonTetherOriginBodyAttachmentPrefab.TryGetComponent(out TetherVfxOrigin tether))
            {
                SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_SharedSuffering.SharedSufferingConnectionTether_prefab, (sharedSufferingTether) =>
                {
                    GameObject tetherPrefab = sharedSufferingTether.InstantiateClone("RandomBarrierTargetConnectionTether", false);
                    tether.tetherPrefab = tetherPrefab;

                    if(tetherPrefab.TryGetComponent(out LineRenderer line))
                    {
                        Material mat = UnityEngine.Object.Instantiate(line.material);
                        mat.SetColor("_TintColor", new Color32(85, 66, 6, 215));
                        mat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Drifter.texDrifterRamp_png).WaitForCompletion());
                        mat.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_blackbeach.texBbDecalMask2_png).WaitForCompletion());

                        mat.SetFloat("_SoftFactor", 0.65f);
                        mat.SetFloat("_BrightnessBoost", 1f);
                        mat.SetFloat("_AlphaBoost", 0.62f);
                        line.material = mat;
                    }
                });
            }

            Content.AddNetworkedObjectPrefab(harpoonTetherOriginBodyAttachmentPrefab);
        }

        public override void Hooks()
        {

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
                if (isCrit == true)
                    return isCrit;
                if (self.body == null )
                    return isCrit;
                if (self.body.HasBuff(harpoonDebuff) == false)
                {
                    return isCrit;
                }

                //idgaf
                if (damageInfo.attacker != null && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody))
                {
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
        private static BuffDef buffDef => RandomBarrierTarget.harpoonDebuff;
        public static float hauntRetryTime = 2;
        float hauntCountdown = 0;
        GameObject tetherOriginInstance;
        SharedSufferingTetherManager tetherManager;
        void Start()
        {
            hauntCountdown = 0;

            this.tetherOriginInstance = UnityEngine.Object.Instantiate<GameObject>(RandomBarrierTarget.harpoonTetherOriginBodyAttachmentPrefab, base.body.gameObject.transform);
            NetworkedBodyAttachment component = this.tetherOriginInstance.GetComponent<NetworkedBodyAttachment>();
            if (component)
            {
                component.AttachToGameObjectAndSpawn(base.gameObject, null);
            }
            this.tetherManager = component.GetComponent<SharedSufferingTetherManager>();
        }

        private void OnDisable()
        {
            hauntCountdown = 0;
        }

        #region barrier on hit
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
        #endregion
        private void FixedUpdate()
        {
            TryHauntInterval();

            this.UpdateAfflicted();
            this.UpdateTethers();
        }

        private void TryHauntInterval()
        {
            if (afflicted.Count >= currentMax)
                return;
            hauntCountdown -= Time.fixedDeltaTime;
            if (hauntCountdown <= 0)
            {
                targetKilled = false;
                if (NetworkServer.active)
                {
                    SphereSearch sphereSearch = new SphereSearch
                    {
                        mask = LayerIndex.entityPrecise.mask,
                        origin = body.transform.position,
                        queryTriggerInteraction = QueryTriggerInteraction.Collide,
                        radius = RandomBarrierTarget.harpoonTargetInitialRange
                    };

                    TeamMask teamMask = TeamMask.GetEnemyTeams(body.teamComponent.teamIndex);
                    List<HurtBox> hurtBoxesList = new List<HurtBox>();

                    sphereSearch
                        .RefreshCandidates()
                        .FilterCandidatesByHurtBoxTeam(teamMask)
                        .FilterCandidatesByDistinctHurtBoxEntities()
                        .GetHurtBoxes(hurtBoxesList);

                    int hurtBoxCount = hurtBoxesList.Count;
                    while (hurtBoxCount > 0)
                    {
                        int i = UnityEngine.Random.Range(0, hurtBoxCount - 1);
                        HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
                        CharacterBody enemyBody = healthComponent.body;

                        if (!enemyBody || enemyBody.HasBuff(buffDef))
                        {
                            hurtBoxesList.Remove(hurtBoxesList[i]);
                            hurtBoxCount--;
                            continue;
                        }

                        if (TryAdd(enemyBody))
                        {
                            if (this.afflicted.Count >= this.currentMax)
                            {
                                return;
                            }
                        }
                        else
                        {
                            hurtBoxesList.Remove(hurtBoxesList[i]);
                            hurtBoxCount--;
                        }
                    }
                    hauntCountdown += hauntRetryTime;
                }
            }
        }

        private void DebuffEnemy(CharacterBody enemyBody)
        {
            enemyBody.AddBuff(buffDef.buffIndex);

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

        #region tether update
        List<CharacterBody> afflicted = new List<CharacterBody>();
        bool afflictedDirty = false;
        int currentMax = 1;
        bool targetKilled = false;
        public bool TryAdd(CharacterBody newTarget)
        {
            if (this.afflicted.Count >= this.currentMax)
            {
                return false;
            }
            this.afflicted.Add(newTarget);
            DebuffEnemy(newTarget);
            this.afflictedDirty = true;
            return true;
        }
        private void UpdateAfflicted()
        {
            for (int i = this.afflicted.Count - 1; i >= 0; i--)
            {
                CharacterBody afflictedBody = this.afflicted[i];
                if (afflictedBody == null || !afflictedBody.healthComponent.alive || !afflictedBody.HasBuff(buffDef))
                {
                    this.afflicted.RemoveAt(i);
                    this.afflictedDirty = true;
                    SetHauntCooldown(true);
                    targetKilled = true;
                    continue;
                }
                if((afflictedBody.corePosition - body.corePosition).sqrMagnitude > RandomBarrierTarget.harpoonTargetLossRange * RandomBarrierTarget.harpoonTargetLossRange)
                {
                    if(afflictedBody.HasBuff(buffDef))
                        afflictedBody.RemoveBuff(buffDef);
                    this.afflicted.RemoveAt(i);
                    this.afflictedDirty = true;
                    SetHauntCooldown(false);
                }
            }
        }

        void SetHauntCooldown(bool kill)
        {
            if(hauntCountdown > 0)
            {
                if (kill == false)
                    return;
                if (targetKilled == true)
                    return;
            }
            hauntCountdown = kill ? RandomBarrierTarget.harpoonTargetTime : hauntRetryTime;
        }
        private void UpdateTethers()
        {
            if (this.afflictedDirty)
            {
                this.tetherManager.SetAfflicted(this.afflicted);
                this.afflictedDirty = false;
            }
        }
        #endregion
    }
}
