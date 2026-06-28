using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine.AddressableAssets;
using EntityStates;
using UnityEngine;
using SwanSongExtended.Modules;
using SwanSongExtended.States;
using RoR2.Projectile;
using R2API;

namespace SwanSongExtended.Changes
{
    class Squolyp : ReworkBase<Squolyp>
    {
        public static GameObject squidBlasterBall;
        public static GameObject squidBlasterBallGhost;

        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Squid.Squid_asset;

        public override string ItemName => "Squid Polyp";

        public override string ItemPickupDesc => null;

        public override string ItemFullDesc => null;

        public override void Init()
        {
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Toolbot.ToolbotGrenadeLauncherProjectile_prefab, CreateSquidBlasterBall);
            SwanSongPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Squid.SquidTurretBodyTurret_asset, SquolypChangeAttack);
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Squid.SquidTurretBody_prefab, SquolypChangeStats);
            base.Init();
        }
        public override void Hooks()
        {
        }

        private static void CreateSquidBlasterBall(GameObject grenade)
        {
            squidBlasterBall = grenade.InstantiateClone("MiredUrnTarball", true);
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ClayBoss.TarballGhost_prefab, (ghost) =>
            {
                squidBlasterBallGhost = ghost.InstantiateClone("SquidBlasterBallGhost", false);
                if (squidBlasterBall.TryGetComponent(out ProjectileController pc))
                {
                    pc.ghostPrefab = squidBlasterBallGhost;
                }
                else
                {
                    Log.Error("squid projectile conrroller rip");
                }
            });


            if (squidBlasterBall.TryGetComponent(out ProjectileSteerTowardTarget pstt))
            {
                //no homing
                UnityEngine.Object.Destroy(pstt);
            }
            if (squidBlasterBall.TryGetComponent(out ProjectileDirectionalTargetFinder pdtf))
            {
                pdtf.ignoreAir = false;
            }
            /*ProjectileCharacterController pcc = squidBlasterBall.GetComponent<ProjectileCharacterController>();
            if (pcc)
            {
                pcc.
            }
            CharacterController cc = squidBlasterBall.GetComponent<CharacterController>();
            if (cc)
            {
                UnityEngine.Object.Destroy(cc);
            }*/
            if (squidBlasterBall.TryGetComponent(out ProjectileImpactExplosion pie))
            {
                pie.lifetime = 1;
                pie.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClayBoss/TarballExplosion.prefab").WaitForCompletion();
            }


            R2API.ContentAddition.AddProjectile(squidBlasterBall);
        }

        void SquolypChangeAttack(SkillDef squolypFire)
        {
            Content.AddEntityState(typeof(SquidBlaster));
            SerializableEntityStateType newSquolypState = new SerializableEntityStateType(typeof(SquidBlaster));
            squolypFire.activationState = newSquolypState;
        }
        void SquolypChangeStats(GameObject squidTurretPrefab)
        {
            CharacterBody squidBody = squidTurretPrefab.GetComponent<CharacterBody>();
            if (squidBody)
            {
                squidBody.baseDamage = 12;
                squidBody.levelDamage = 2.4f;
            }
        }
    }
}
