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
        public static void MissileArtifact_VerminSpit(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int aimRayLoc = 0;
            int damageLoc = 0;
            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<BaseState>(nameof(BaseState.GetAimRay)),
                x => x.MatchStloc(out aimRayLoc)
                );
            if (!b1)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_VerminSpit), 1);
                return;
            }
            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdfld<BaseState>(nameof(BaseState.damageStat)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(out damageLoc)
                );
            if (!b2)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_VerminSpit), 2);
                return;
            }
            bool b4 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<ProjectileManager>(nameof(ProjectileManager.FireProjectileWithoutDamageType))
                );
            if (!b4)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_VerminSpit), 3);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, aimRayLoc);
            c.Emit(OpCodes.Ldloc, damageLoc);
            c.EmitDelegate<Action<EntityStates.FlyingVermin.Weapon.Spit, Ray, float>>((self, aimRay, damage) =>
            {
                if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody) && MoreProjectilesModule.UseExpensiveProjectiles)
                {
                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = self.projectilePrefab,
                        position = aimRay.origin,
                        rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                        owner = self.gameObject,
                        damage = damage,
                        crit = Util.CheckRoll(self.critStat, self.characterBody.master)
                    };
                    Vector3 axis = Vector3.Cross(Vector3.up, aimRay.direction);
                    MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread, axis);
                }
            });
        }
        #region simples
        public static void MissileArtifact_ChildSpark(On.EntityStates.ChildMonster.SparkBallFire.orig_FireBomb orig, EntityStates.ChildMonster.SparkBallFire self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, SparkBallFire.bombDamageCoefficient, SparkBallFire.projectilePrefab);
        }
        #endregion
        public static void MissileArtifact_LemurianFireball(On.EntityStates.LemurianMonster.FireFireball.orig_OnEnter orig, EntityStates.LemurianMonster.FireFireball self)
        {
            orig(self);
            if (MoreProjectilesModule.UseExpensiveProjectiles)
                MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, FireFireball.damageCoefficient, FireFireball.projectilePrefab);
        }

        public static void MissileArtifact_GupDeathEnter(On.EntityStates.Gup.BaseSplitDeath.orig_OnEnter orig, EntityStates.Gup.BaseSplitDeath self)
        {
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody) && MoreProjectilesModule.UseExpensiveProjectiles)
            {
                self.spawnCount = 3;
            }
            orig(self);
        }
    }
}
