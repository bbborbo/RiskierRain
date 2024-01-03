using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RiskierRainContent.CoreModules.StatHooks;
using static RiskierRainContent.Components.TargetRandomNearbyBehavior;

namespace RiskierRainContent
{
    public partial class RiskierRainContent
    {
        public static float harpoonBarrierBase = 5;
        public static float harpoonBarrierStack = 5;
        public static float harpoonTargetTime = 15;

        public static Material harpoonTargetMaterial;

        public void HuntersHarpoonRework()
        {
            harpoonTargetMaterial = CreateMatRecolor(new Color32(210, 140, 32, 100));

            On.RoR2.CharacterBody.OnInventoryChanged += AddHarpoonBehavior;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += RevokeHarpoonRights;
            OnTargetFoundEvent += HarpoonOnTargetFound;
            GetHitBehavior += HarpoonOnHit;
            LanguageAPI.Add("ITEM_MOVESPEEDONKILL_PICKUP", "Target a nearby enemy, gaining barrier on hit.");
            LanguageAPI.Add("ITEM_MOVESPEEDONKILL_DESC", $"Once every <style=cIsDamage>{TargetRandomNearbyBehavior.baseHauntInterval}</style> seconds, <style=cIsDamage>target</style> a random enemy. " +
                $"Attacking the targeted enemy grants a <style=cIsHealing>temporary barrier</style> " +
                $"for <style=cIsHealing>{harpoonBarrierBase} health</style> <style=cStack>(+{harpoonBarrierStack} per stack)</style>.");
        }

        private void AddHarpoonBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            int hasHarpoon = self.inventory.GetItemCount(DLC1Content.Items.MoveSpeedOnKill);
            if (hasHarpoon > 0)
            {
                TargetRandomNearbyBehavior trnb = self.AddItemBehavior<TargetRandomNearbyBehavior>(1);
                trnb.itemCounts["Harpoon"] = hasHarpoon;
                trnb.EvaluateItemCount();
            }
        }

        private void HarpoonOnTargetFound(TargetRandomNearbyBehavior instance, CharacterBody newTarget, CharacterBody oldTarget)
        {
            CharacterBody body = instance.body;
            if (body?.inventory)
            {
                int harpoonCount = body.inventory.GetItemCount(DLC1Content.Items.MoveSpeedOnKill);
                if (harpoonCount > 0)
                {
                    if(oldTarget != null && oldTarget.HasBuff(Assets.harpoonDebuff))
                    {
                        oldTarget.RemoveOldestTimedBuff(Assets.harpoonDebuff.buffIndex);
                    }
                    if(newTarget != null)
                    {
                        newTarget.AddTimedBuffAuthority(Assets.harpoonDebuff.buffIndex, TargetRandomNearbyBehavior.baseHauntInterval);

                        //thanks hifu <3
                        Transform modelTransform = newTarget.modelLocator?.modelTransform;
                        if (modelTransform != null)
                        {
                            var temporaryOverlay = modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                            temporaryOverlay.duration = RiskierRainContent.harpoonTargetTime;
                            temporaryOverlay.animateShaderAlpha = true;
                            temporaryOverlay.alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);// AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                            temporaryOverlay.destroyComponentOnEnd = true;
                            temporaryOverlay.originalMaterial = RiskierRainContent.harpoonTargetMaterial;
                            temporaryOverlay.AddToCharacerModel(modelTransform.GetComponent<CharacterModel>());
                        }
                    }
                }
            }
        }

        public static Material CreateMatRecolor(Color32 blueEquivalent)
        {
            var mat = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/Base/Huntress/matHuntressFlashExpanded.mat").WaitForCompletion());

            mat.SetColor("_TintColor", blueEquivalent);
            mat.SetInt("_Cull", 1);

            return mat;
        }

        private void HarpoonOnHit(CharacterBody attackerBody, DamageInfo damageInfo, GameObject victim)
        {
            CharacterBody victimBody = victim.GetComponent<CharacterBody>();
            Inventory inv = attackerBody.inventory;
            HealthComponent hc = attackerBody.healthComponent;
            if (inv != null && hc != null && victimBody != null && victimBody.HasBuff(Assets.harpoonDebuff))
            {
                int harpoonCount = inv.GetItemCount(DLC1Content.Items.MoveSpeedOnKill);
                if(harpoonCount > 0)
                {
                    float barrierGrant = harpoonBarrierBase + harpoonBarrierStack * (harpoonCount - 1);
                    hc.AddBarrierAuthority(barrierGrant * damageInfo.procCoefficient);
                }
            }
        }

        private void RevokeHarpoonRights(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "MoveSpeedOnKill"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }
    }
}
