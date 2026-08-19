using FruityElites.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FruityElites.EliteReworks
{
    class VoidtouchedReworks : EliteReworkBase<VoidtouchedReworks>
    {
        [AutoConfig("Singularity On Death: Projectile Min Travel Time", 0.2f)]
        public static float singularityMinimumTravelTime = 0.3f;
        [AutoConfig("Singularity On Death: Projectile Max Travel Distance", 60f)]
        public static float singularityMaximumTravelDistance = 60f;
        [AutoConfig("Singularity On Death: Projectile Max Horizontal Speed", 20f)]
        public static float singularityHorizontalSpeed = 20f;
        public static float singularityProjectileAntiGravity = 1f;
        [AutoConfig("Singularity On Death: Singularity Radius", 8f)]
        public static float singularityRadius = 8f;
        [AutoConfig("Singularity On Death: Singularity Duration", 3)]
        public static float singularityDuration = 3f;

        [AutoConfig("Nullify Stack On Hit: Base Duration", 18)]
        public static float voidtouchedNullifyBaseDuration = 18;
        public override string eliteName => "Voidtouched";

        public override void Hooks()
        {
            //ChangeLightningStake(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteLightning/LightningStake.prefab").WaitForCompletion());
            //AssetReferenceT<GameObject> ref1 = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_ElementalRingVoid.ElementalRingVoidBlackHole_prefab);
            //AssetAsyncReferenceManager<GameObject>.LoadAsset(ref1).Completed += (ctx) => GetSingularityPrefab(ctx.Result);

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += RemoveVoidtouchedCollapse;
            On.RoR2.GlobalEventManager.ProcessHitEnemy += AddVoidtouchedNullify;
            //On.RoR2.GlobalEventManager.OnCharacterDeath += VoidtouchedSingularity;
            GlobalEventManager.onCharacterDeathGlobal += FireSingularityBomb;
        }

        private void FireSingularityBomb(DamageReport damageReport)
        {
            if (!NetworkServer.active)
                return;

            if (CommonAssets.voidSingularityBomb == null || CommonAssets.voidtouchedSingularity == null)
            {
                Debug.LogError("FruityAspectsGaming: Void bomb null sjdnfjhsdjcskdnfjsdfnvcszchbsdahujcsd");
                return;
            }

            CharacterBody victimBody = damageReport.victimBody;
            if (victimBody == null || !victimBody.HasBuff(DLC1Content.Buffs.EliteVoid))
                return;

            GameObject target = damageReport.attacker;
            if(target == null || (target.transform.position - victimBody.corePosition).sqrMagnitude > singularityMaximumTravelDistance * singularityMaximumTravelDistance)
            {
                target = FindNewTarget(victimBody.corePosition, damageReport.victimTeamIndex);
            }

            //no target, drop singularity
            if(target == null)
            {
                ProcChainMask procChainMask6 = damageReport.damageInfo.procChainMask;
                procChainMask6.AddProc(ProcType.Rings);
                float damageCoefficient10 = 0;
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    damage = damageCoefficient10,
                    crit = false,
                    damageColorIndex = DamageColorIndex.Void,
                    position = victimBody.previousPosition,
                    procChainMask = procChainMask6,
                    force = 6000f,
                    owner = victimBody.gameObject,
                    projectilePrefab = Modules.CommonAssets.voidtouchedSingularity,
                    rotation = Quaternion.identity,
                    target = null,
                });
                return;
            }

            Vector3 targetPosition = target.transform.position;
            Vector3 horizontal = targetPosition - victimBody.corePosition;
            horizontal.y = 0;
            float horizontalDistance = horizontal.magnitude;
            float travelTime = Mathf.Max(singularityMinimumTravelTime, horizontalDistance / singularityHorizontalSpeed);
            Vector3 initialVelocity = Trajectory.CalculateInitialVelocityFromTime(
                victimBody.corePosition, targetPosition, travelTime, 
                Physics.gravity.y * (1f - singularityProjectileAntiGravity), 0, singularityMaximumTravelDistance);

            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = CommonAssets.voidSingularityBomb,
                owner = victimBody.gameObject,
                position = victimBody.corePosition,
                rotation = Util.QuaternionSafeLookRotation(initialVelocity.normalized),
                damage = 0,
                crit = false,
                speedOverride = initialVelocity.magnitude,
                useSpeedOverride = true,
                fuseOverride = travelTime,
                useFuseOverride = true
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }

        private GameObject FindNewTarget(Vector3 origin, TeamIndex team)
        {
            TeamMask enemyTeams = TeamMask.GetEnemyTeams(team);

            SphereSearch search = new SphereSearch();
            search.mask = LayerIndex.entityPrecise.mask;
            search.origin = origin;
            search.radius = singularityMaximumTravelDistance;
            search.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
            search.RefreshCandidates();
            search.FilterCandidatesByHurtBoxTeam(enemyTeams);
            search.OrderCandidatesByDistance();
            search.FilterCandidatesByDistinctHurtBoxEntities();
            HurtBox[] hurtBoxes = search.GetHurtBoxes();
            search.ClearCandidates();

            if (hurtBoxes.Length == 0)
                return null;
            if (hurtBoxes.Length <= 3)
                return hurtBoxes[0].healthComponent.gameObject;

            int indexOfBestTarget = -1;
            int targetCountNearBestTarget = 0;
            for (int i = 0; i < hurtBoxes.Length; i++)
            {
                HurtBox[] hurtBoxes2 = new SphereSearch
                {
                    radius = singularityRadius,
                    mask = LayerIndex.entityPrecise.mask,
                    origin = hurtBoxes[i].transform.position,
                    queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
                }.RefreshCandidates().FilterCandidatesByHurtBoxTeam(enemyTeams).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();
                if (hurtBoxes2.Length > targetCountNearBestTarget)
                {
                    indexOfBestTarget = i;
                    targetCountNearBestTarget = hurtBoxes2.Length;
                }
            }

            if (indexOfBestTarget != -1)
                return hurtBoxes[indexOfBestTarget].healthComponent.gameObject;
            return null;
        }

        private void AddVoidtouchedNullify(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.attacker != null && victim != null && damageInfo.procCoefficient > 0)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();
                if (attackerBody && victimBody)
                {
                    float luck = attackerBody.master ? attackerBody.master.luck : 0;
                    if (attackerBody.HasBuff(DLC1Content.Buffs.EliteVoid) && Util.CheckRoll0To1(damageInfo.procCoefficient, luck))
                    {
                        victimBody.AddTimedBuffAuthority(RoR2Content.Buffs.NullifyStack.buffIndex, voidtouchedNullifyBaseDuration);
                    }
                }
            }
            orig(self, damageInfo, victim);
        }

        private void RemoveVoidtouchedCollapse(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "BleedOnHitVoid")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Buffs", "EliteVoid")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.HasBuff))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);

            return;
            c.GotoNext(MoveType.Before,
                x => x.MatchStloc(out _)
                );
            c.EmitDelegate<Func<int, int>>((guh) =>
            {
                return 0;
            });
        }
    }
}
