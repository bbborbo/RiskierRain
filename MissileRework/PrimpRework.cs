using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MissileRework
{
    public partial class MissileReworkPlugin
    {
        float shrimpShieldBase = 40;
        float shrimpDamageCoeffBase = 0.30f;
        float shrimpDamageCoeffStack = 0.30f;

        internal void ReworkPrimp()
        {
            IL.RoR2.GlobalEventManager.OnHitEnemy += ShrimpRework;
            GetStatCoefficients += ShrimpShieldFix;

            LanguageAPI.Add("ITEM_MISSILEVOID_PICKUP", "While you have shield, fire missiles on every hit. <style=cIsVoid>Corrupts all AtG Missile Mk. 3s</style>.");
            LanguageAPI.Add("ITEM_MISSILEVOID_DESC",
                $"Gain <style=cIsHealing>{shrimpShieldBase} shield</style>. " +
                $"While you have a <style=cIsHealing>shield</style>, " +
                $"hitting an enemy fires <style=cIsDamage>3</style> missiles that each deal " +
                $"<style=cIsDamage>{shrimpDamageCoeffBase * 100}%</style> " +
                $"<style=cStack>(+{shrimpDamageCoeffStack * 100}% per stack)</style> TOTAL damage. " +
                $"<style=cIsVoid>Corrupts all AtG Missile Mk. 3s</style>.");
        }
        private void ShrimpShieldFix(CharacterBody sender, StatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv && inv.GetItemCount(DLC1Content.Items.MissileVoid) > 0)
            {
                args.shieldMultAdd -= 0.1f;
                args.baseShieldAdd += shrimpShieldBase;
            }
        }

        private void ShrimpRework(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //jump to shrimp implementation
            int shrimpLoc = 32;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "MissileVoid"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out shrimpLoc)
                );

            /*int dmgLoc = 37;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdloc(shrimpLoc),
                x => x.MatchConvR4(),
                x => x.MatchMul(),
                x => x.MatchStloc(out dmgLoc)
                );*/

            //inject our new damage coefficient
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnHitProcDamage))
                );
            c.Emit(OpCodes.Ldloc, shrimpLoc);
            c.EmitDelegate<Func<float, int, float>>((damageCoefficient, itemCount) =>
            {
                float damageOut = shrimpDamageCoeffBase + (shrimpDamageCoeffStack * (itemCount - 1));
                return damageOut;
            });

            if(ShouldReworkIcbm.Value == true)
            {
                //override for missile artifact
                c.GotoNext(MoveType.Before,
                    x => x.MatchLdloc(out _),
                    x => x.MatchLdcI4(out _),
                    x => x.MatchBgt(out _)
                    );
                c.Remove();
                c.EmitDelegate<Func<int>>(() =>
                {
                    return RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact) ? 1 : 0;
                });
            }
        }
    }
}
