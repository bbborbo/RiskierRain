using EntityStates.MiniMushroom;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.ExpansionManagement;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

using EntityStates.Captain.Weapon;
using EntityStates.ArtifactShell;
using EntityStates.LemurianMonster;
using EntityStates.VagrantMonster;
using EntityStates.LunarWisp;
using EntityStates.BeetleGuardMonster;
using EntityStates;
using EntityStates.ClayBoss;
using EntityStates.Mage.Weapon;
using EntityStates.Loader;
using EntityStates.MiniMushroom;
using EntityStates.ChildMonster;
using RoR2.Orbs;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;
using EntityStates.Bell.BellWeapon;

namespace RainrotSharedUtils.MoreProjectiles
{
    public static partial class MoreProjectilesHooks
    {
        public const float missileSpread = 45;
        public const float projectileSpread = 20;

        public static void OverrideIcbmMissiles(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILLabel itemCountLoc = c.DefineLabel(); ;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "MoreMissile"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective)),
                x => x.MatchDup()
                );
            if (!b1)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(OverrideIcbmMissiles), 1);
                return;
            }

            c.Index--;
            itemCountLoc = c.MarkLabel();

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _)
                );
            if (!b2)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(OverrideIcbmMissiles), 2);
                return;
            }
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<CharacterBody, int>>((body) =>
            {
                if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(body))
                    return 1;
                return 0;
            });

            c.GotoLabel(itemCountLoc);
            c.Remove();

            //bool b = c.TryGotoNext(MoveType.Before,
            //    x => x.MatchLdloc(0),
            //    x => x.MatchLdcI4(0),
            //    x => x.MatchBle(out _)
            //    );
            //c.Remove();
            //c.EmitDelegate<Func<int>>(() =>
            //{
            //    return RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact) ? 1 : 0;
            //});
        }

        public static void MissileArtifact_FireProjectile(On.EntityStates.GenericProjectileBaseState.orig_FireProjectile orig, EntityStates.GenericProjectileBaseState self)
        {
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                if (self.isAuthority)
                {
                    bool isValidState = false;
                    bool isVertical = false;
                    float spread = projectileSpread;

                    //alpha construct, phase round, scrap cannon
                    if ((self is EntityStates.MinorConstruct.Weapon.FireConstructBeam && MoreProjectilesModule.UseExpensiveProjectiles)
                        || self is EntityStates.Commando.CommandoWeapon.FireFMJ
                        || self is EntityStates.Toolbot.FireGrenadeLauncher)
                    {
                        isValidState = true;
                    }
                    else
                    {
                        //blind pest
                        if (self is EntityStates.FlyingVermin.Weapon.Spit && MoreProjectilesModule.UseExpensiveProjectiles)
                        {
                            isValidState = true;
                            isVertical = true;
                        }

                        //barnacle
                        if (self is EntityStates.VoidBarnacle.Weapon.Fire)
                        {
                            isValidState = true;
                            spread = missileSpread;
                        }
                    }

                    if (isValidState)
                    {
                        Ray aimRay = self.GetAimRay();
                        aimRay = self.ModifyProjectileAimRay(aimRay);
                        aimRay.direction = Util.ApplySpread(aimRay.direction, self.minSpread, self.maxSpread, 1f, 1f, 0f, self.projectilePitchBonus);

                        FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                        {
                            projectilePrefab = self.projectilePrefab,
                            position = aimRay.origin,
                            rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                            owner = self.gameObject,
                            damage = self.damageStat * self.damageCoefficient,
                            crit = Util.CheckRoll(self.critStat, self.characterBody.master),
                            force = self.force
                        };
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                        Vector3 axis = Vector3.Cross(Vector3.up, aimRay.direction);
                        if(!isVertical)
                            axis = Vector3.Cross(aimRay.direction, axis);

                        MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, spread, axis);
                        return;
                    }
                }
            }
            orig(self);
        }
    }
}
