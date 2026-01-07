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

namespace SwanSongExtended.Items
{
    class RandomBarrierTarget : ItemBase<RandomBarrierTarget>
    {
        public override bool isEnabled => true; 

        public static float harpoonBarrierBase = 6;
        public static float harpoonBarrierStack = 6;
        public static float harpoonTargetTime = 15;
        public static float harpoonDecayReduction = 0.2f;

        public static Material harpoonTargetMaterial;

        public override string ItemName => "Borbo\u2019s Arrowhead";

        public override string ItemLangTokenName => "RANDOMBARRIERTARGET";

        public override string ItemPickupDesc => "Target a nearby enemy, gaining barrier on hit.";

        public override string ItemFullDescription => $"Reduce barrier decay by <style=cIsHealing>-{ConvertDecimal(harpoonDecayReduction)}</style>." +
                $"Once every <style=cIsDamage>{harpoonTargetTime}</style> seconds, <style=cIsDamage>target</style> a random enemy. " +
                $"Attacking the targeted enemy grants a <style=cIsHealing>temporary barrier</style> " +
                $"for <style=cIsHealing>{harpoonBarrierBase} health</style> <style=cStack>(+{harpoonBarrierStack} per stack)</style>.";

        public override string ItemLore => "Not to be confused with Hunter's Harpoon!";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Hooks()
        {
            harpoonTargetMaterial = CreateMatRecolor(new Color32(210, 140, 32, 100));

            //IL.RoR2.GlobalEventManager.OnCharacterDeath += RevokeHarpoonRights;
            On.RoR2.CharacterBody.OnInventoryChanged += AddHarpoonBehavior;
            GetHitBehavior += HarpoonOnHit;
            GetMoreStatCoefficients += HarpoonDecay;
        }

        private void HarpoonDecay(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.inventory && sender.inventory)
            {
                int count = GetCount(sender);
                if (count > 0)
                    args.barrierDecayRatePercentIncreaseMult *= 1 - harpoonDecayReduction;
            }
        }

        public static Material CreateMatRecolor(Color32 blueEquivalent)
        {
            var mat = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/Base/Huntress/matHuntressFlashExpanded.mat").WaitForCompletion());

            mat.SetColor("_TintColor", blueEquivalent);
            mat.SetInt("_Cull", 1);

            return mat;
        }

        private void HarpoonOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            Inventory inv = attackerBody.inventory;
            HealthComponent hc = attackerBody.healthComponent;
            if (inv != null && hc != null && victimBody != null && victimBody.HasBuff(CommonAssets.harpoonDebuff))
            {
                int harpoonCount = inv.GetItemCountEffective(DLC1Content.Items.MoveSpeedOnKill);
                if (harpoonCount > 0)
                {
                    float barrierGrant = harpoonBarrierBase + harpoonBarrierStack * (harpoonCount - 1);
                    hc.AddBarrierAuthority(barrierGrant * damageInfo.procCoefficient);
                }
            }
        }

        private void AddHarpoonBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            int maskCount = self.inventory.GetItemCountEffective(DLC1Content.Items.MoveSpeedOnKill);
            self.AddItemBehavior<RandomBarrierTargetBehavior>(maskCount);
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
    public class RandomBarrierTargetBehavior : RoR2.CharacterBody.ItemBehavior
    {
        public static float baseHauntRadius = 35;
        public static float hauntRetryTime = 1;
        float hauntStopwatch = 0;
        void Start()
        {
            hauntStopwatch = RandomBarrierTarget.harpoonTargetTime;
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
                enemyBody.AddTimedBuffAuthority(CommonAssets.harpoonDebuff.buffIndex, RandomBarrierTarget.harpoonTargetTime);
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
