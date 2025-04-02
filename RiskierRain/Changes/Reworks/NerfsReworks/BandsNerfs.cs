using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        // could go with 6 base, 1 total
        static float runaldBaseDamage = 6f;
        static float runaldTotalDamage = 1f;
        static float kjaroBaseDamage = 6;
        static float kjaroTotalDamage = 1f;
        static string runaldTotal = Tools.ConvertDecimal(runaldTotalDamage);
        static string kjaroTotal = Tools.ConvertDecimal(kjaroTotalDamage);

        void NerfBands()
        {
            //IL.RoR2.GlobalEventManager.ProcessHitEnemy += CooldownBuff;

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += RunaldNerf;
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += KjaroNerf;

            LanguageAPI.Add("ITEM_ICERING_DESC", 
                $"Hits from <style=cIsUtility>skills or equipment</style> " +
                $"that deal <style=cIsDamage>more than 400% damage</style> also blast enemies with a " +
                $"<style=cIsDamage>runic ice blast</style>, " +
                $"<style=cIsUtility>Chilling</style> them for <style=cIsUtility>3s</style> <style=cStack>(+3s per stack)</style> and " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(runaldBaseDamage)}</style> BASE damage, " +
                $"plus <style=cIsDamage>{runaldTotal}</style> <style=cStack>(+{runaldTotal} per stack)</style> TOTAL damage. " +
                $"Recharges every <style=cIsUtility>10</style> seconds.");
            LanguageAPI.Add("ITEM_FIRERING_DESC", 
                $"Hits from <style=cIsUtility>skills or equipment</style> " +
                $"that deal <style=cIsDamage>more than 400% damage</style> also blast enemies with a " +
                $"<style=cIsDamage>runic flame tornado</style>, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(runaldBaseDamage)}</style> BASE damage, " +
                $"plus <style=cIsDamage>{runaldTotal}</style> <style=cStack>(+{runaldTotal} per stack)</style> TOTAL damage over time. " +
                $"Recharges every <style=cIsUtility>10</style> seconds.");
        }

        private void CooldownBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLocation = 51;
            int cooldownTrackerLocation = 51;

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "IceRing")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out itemCountLocation)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(1),
                x => x.MatchStloc(out cooldownTrackerLocation)
                );

            // % CDR (alien head, brainstalks)
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(cooldownTrackerLocation),
                x => x.MatchConvR4()
                );
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<float, CharacterBody, float>>((cooldown, self) =>
            {
                float multiplier = 1;
                if (self.skillLocator.special)
                {
                    float scale = self.skillLocator.special.cooldownScale;
                    multiplier *= scale;

                    if (self.skillLocator.special.flatCooldownReduction < 9)
                    {
                    }
                    else
                    {
                        //multiplier = 0.5f / 10;
                    }
                }

                return cooldown * multiplier;
            });

            // flat CDR (purity)
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(cooldownTrackerLocation),
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(out _)
                );
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<float, CharacterBody, float>>((seconds, self) =>
            {
                float flat = 0;
                if (self.skillLocator.special)
                {
                    //flat = self.skillLocator.special.flatCooldownReduction;
                }

                return Mathf.Max(seconds - flat, 1);
            });
        }

        private void RunaldNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLocation = 80;
            int totalDamageMultiplierLocation = 85;

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "IceRing")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out itemCountLocation)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _), //original damage multiplier, to be replaced
                x => x.MatchLdloc(itemCountLocation),
                x => x.MatchConvR4(),
                x => x.MatchMul(),
                x => x.MatchStloc(out totalDamageMultiplierLocation)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, runaldTotalDamage);

            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(totalDamageMultiplierLocation),
                x => x.MatchCallOrCallvirt(out _)
                );
            //c.Index--;
            c.Emit(OpCodes.Ldloc_2);
            c.EmitDelegate<Func<float, CharacterBody, float>>((damage, self) =>
            {
                float dam = self.baseDamage * runaldBaseDamage;

                return damage + dam;
            });
        }

        private void KjaroNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLocation = 81;
            int totalDamageMultiplierLocation = 91;

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "FireRing")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out itemCountLocation)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _), //original damage multiplier, to be replaced
                x => x.MatchLdloc(itemCountLocation),
                x => x.MatchConvR4(),
                x => x.MatchMul(),
                x => x.MatchStloc(out totalDamageMultiplierLocation)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, kjaroTotalDamage);

            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(totalDamageMultiplierLocation),
                x => x.MatchCallOrCallvirt(out _)
                );
            c.Emit(OpCodes.Ldloc_2);
            c.EmitDelegate<Func<float, CharacterBody, float>>((damage, self) =>
            {
                float dam = self.baseDamage * kjaroBaseDamage;

                return damage + dam;
            });
        }
    }
}
