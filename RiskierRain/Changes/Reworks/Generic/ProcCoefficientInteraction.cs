using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public void FixProcCoeffInteractions()
        {
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += ProcCoeffFix_OnHitEnemy;
        }

        private void ProcCoeffFix_OnHitEnemy(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            FixChanceForProcItem(c, "RoR2.RoR2Content/Items", "BleedOnHitAndExplode"); //this is bleed chance
            FixChanceForProcItem(c, "RoR2.RoR2Content/Items", "Missile");
            FixChanceForProcItem(c, "RoR2.RoR2Content/Items", "ChainLightning");
            FixChanceForProcItem(c, "RoR2.DLC1Content/Items", "ChainLightningVoid");
            FixChanceForProcItem(c, "RoR2.RoR2Content/Items", "BounceNearby");
            FixChanceForProcItem(c, "RoR2.RoR2Content/Items", "StickyBomb");
            FixChanceForProcItem(c, "RoR2.RoR2Content/Items", "FireballsOnHit");
            FixChanceForProcItem(c, "RoR2.RoR2Content/Items", "LightningStrikeOnHit");
            //FixChanceForProcItem(c, "RoR2.DLC2Content/Items", "MeteorAttackOnHighDamage");
            //FixChanceForProcItem(c, "RoR2.DLC2Content/Items", "StunAndPierceDamage");
        }

        private void FixChanceForProcItem(ILCursor c, string a, string b)
        {
            c.Index = 0;

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(a, b),
                x => x.MatchCallOrCallvirt("RoR2.Inventory", nameof(RoR2.Inventory.GetItemCount))
                );
            c.GotoNext(
                MoveType.Before,
                x => x.MatchLdfld<DamageInfo>(nameof(DamageInfo.procCoefficient))
                );
            Debug.LogWarning("proc hook initialized for " + b);
            c.Remove();
            c.EmitDelegate<Func<DamageInfo, float>>((damageInfo) =>
            {
                return GetProcRate(damageInfo);
            });
            

            float GetProcRate(DamageInfo damageInfo)
            {
                return 1;
            }
        }

        private void FixBleedChance(ILCursor c)
        {
            c.Index = 0;

            if(
                c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchCallOrCallvirt<CharacterBody>("get_bleedChance")
                    )
                && c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(1),
                    x => x.MatchLdfld<DamageInfo>(nameof(DamageInfo.procCoefficient))
                    )
                )
            {
                c.Index--;
                c.Remove();
                c.EmitDelegate<Func<DamageInfo, float>>((damageInfo) =>
                {
                    float procRate = 1;
                    return procRate;
                });
            }
            else
            {
                Debug.LogError("Bleed chance hook failed");
            }
        }
    }
}
