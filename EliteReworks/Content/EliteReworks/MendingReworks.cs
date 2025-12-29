using HG;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FruityElites.EliteReworks
{
    class MendingReworks : EliteReworkBase<MendingReworks>
    {
        public override string eliteName => "Mending";

        public override void Hooks()
        {
            base.Hooks();
            IL.RoR2.HealNearbyController.Tick += ReplaceHealingWithBarrier;
            //On.RoR2.HealNearbyController.Tick += BarrierTick;
        }
        private void BarrierTick(On.RoR2.HealNearbyController.orig_Tick orig, HealNearbyController self)
        {
            if (!self.networkedBodyAttachment || !self.networkedBodyAttachment.attachedBody
                || !self.networkedBodyAttachment.attachedBodyObject || !self.networkedBodyAttachment.attachedBody.healthComponent.alive)
            {
                return;
            }
            List<HurtBox> possibleTargets = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
            self.SearchForTargets(possibleTargets);
            float amount = self.damagePerSecondCoefficient * self.networkedBodyAttachment.attachedBody.damage / self.tickRate;
            List<Transform> chosenTargets = CollectionPool<Transform, List<Transform>>.RentCollection();
            int i = 0;
            while (i < possibleTargets.Count)
            {
                HurtBox hurtBox = possibleTargets[i];
                if (!hurtBox || !hurtBox.healthComponent
                    || hurtBox.healthComponent.body.HasBuff(DLC1Content.Buffs.EliteEarth))
                {
                    goto IL_14A;
                }
                HealthComponent healthComponent = hurtBox.healthComponent;
                if (!(hurtBox.healthComponent.body == self.networkedBodyAttachment.attachedBody))
                {
                    CharacterBody body = healthComponent.body;
                    Transform item = ((body != null) ? body.coreTransform : null) ?? hurtBox.transform;
                    chosenTargets.Add(item);
                    if (NetworkServer.active)
                    {
                        //healthComponent.Heal(amount, default(ProcChainMask), true);
                        healthComponent.AddBarrier(amount);
                        goto IL_14A;
                    }
                    goto IL_14A;
                }
            IL_158:
                i++;
                continue;
            IL_14A:
                if (chosenTargets.Count < self.maxTargets)
                {
                    goto IL_158;
                }
                break;
            }
            self.isTetheredToAtLeastOneObject = ((float)chosenTargets.Count > 0f);
            if (self.tetherVfxOrigin)
            {
                self.tetherVfxOrigin.SetTetheredTransforms(chosenTargets);
            }
            if (self.activeVfx)
            {
                self.activeVfx.SetActive(self.isTetheredToAtLeastOneObject);
            }
            CollectionPool<Transform, List<Transform>>.ReturnCollection(chosenTargets);
            CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(possibleTargets);
        }

        private void ReplaceHealingWithBarrier(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<HurtBox>(nameof(HurtBox.healthComponent)),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_fullHealth")
                );
            c.Index--;
            c.Remove();
            c.EmitDelegate<Func<HealthComponent, float>>((healthComponent) =>
            {
                return healthComponent.fullHealth * 2f;
            });

            c.GotoNext(MoveType.Before,
                    x => x.MatchCallOrCallvirt<RoR2.HealthComponent>(nameof(HealthComponent.Heal))
                );
            c.Remove();
            c.Remove();
            //c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<HealthComponent, float, RoR2.ProcChainMask, bool/*, HealNearbyController*/>>((targetHealthComponent, healAmount, procChainMask, nonRegen/*, self*/) =>
            {
                //CharacterBody body = self.networkedBodyAttachment.attachedBody;
                //if (body.HasBuff(DLC1Content.Buffs.EliteEarth))
                //{
                float barrierAmt = 0;
                barrierAmt = healAmount;
                targetHealthComponent.AddBarrier(barrierAmt);
                //}
                //else
                //{
                //    targetHealthComponent.Heal(healAmount, procChainMask, isRegen);
                //}
            });
        }
    }
}
