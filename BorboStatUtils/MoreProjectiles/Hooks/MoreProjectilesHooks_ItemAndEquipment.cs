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
        public static void MissileArtifact_Shuriken(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<ProjectileManager>(nameof(ProjectileManager.FireProjectileWithoutDamageType))
                );
            if (!b)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_Shuriken));
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<ProjectileManager, GameObject, Vector3, Quaternion, GameObject, float, float, bool, DamageColorIndex, GameObject, float, PrimarySkillShurikenBehavior>>(
                (projectileManagerInstance, projectilePrefab, origin, rotation, owner, damage, force, crit, damageColorIndex, target, speedOverride, behavior) =>
                {
                    projectileManagerInstance.FireProjectileWithoutDamageType(projectilePrefab, origin, rotation, owner, damage, force, crit, damageColorIndex, target, speedOverride);

                    if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(behavior.body))
                    {
                        Ray aimRay = behavior.GetAimRay();
                        FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                        {
                            projectilePrefab = projectilePrefab,
                            position = origin,
                            rotation = rotation,
                            owner = owner,
                            damage = damage,
                            crit = crit
                        };
                        MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread);
                    }
                });
        }
        public static void EquipmentsLol()
        {

            //preon
            On.RoR2.EquipmentSlot.FireBfg += (orig, self) =>
            {
                if (orig(self))
                {
                    MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, 40, RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BFG.BeamSphere_prefab);
                    return true;
                }
                return false;
            };
            //primordial cube
            On.RoR2.EquipmentSlot.FireBlackhole += (orig, self) =>
            {
                if (orig(self))
                {
                    MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, 0, RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Blackhole.GravSphere_prefab);
                    return true;
                }
                return false;
            };
            //molotov
            On.RoR2.EquipmentSlot.FireMolotov += (orig, self) =>
            {
                if (orig(self) && MoreProjectilesModule.UseExpensiveProjectiles)
                {
                    MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, 1, RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_Molotov.MolotovClusterProjectile_prefab);
                    return true;
                }
                return false;
            };
            //goobo
            On.RoR2.EquipmentSlot.FireGummyClone += (orig, self) =>
            {
                if (orig(self) && MoreProjectilesModule.UseExpensiveProjectiles)
                {
                    if (self.characterBody && self.characterBody.master && !self.characterBody.master.IsDeployableLimited(DeployableSlot.GummyClone))
                        MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, 0, RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_GummyClone.GummyCloneProjectile_prefab);
                    return true;
                }
                return false;
            };
        }
    }
}
