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
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FruityElites.EliteReworks
{
    class VoidtouchedReworks : EliteReworkBase<VoidtouchedReworks>
    {
        [AutoConfig("Nullify Base Duration", 12)]
        public static float voidtouchedNullifyBaseDuration = 12;
        public override string eliteName => "Voidtouched";

        public override void Hooks()
        {
            //ChangeLightningStake(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteLightning/LightningStake.prefab").WaitForCompletion());
            //AssetReferenceT<GameObject> ref1 = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_ElementalRingVoid.ElementalRingVoidBlackHole_prefab);
            //AssetAsyncReferenceManager<GameObject>.LoadAsset(ref1).Completed += (ctx) => GetSingularityPrefab(ctx.Result);

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += RemoveVoidtouchedCollapse;
            On.RoR2.GlobalEventManager.ProcessHitEnemy += AddVoidtouchedNullify;
            On.RoR2.GlobalEventManager.OnCharacterDeath += VoidtouchedSingularity;
        }

        private void GetSingularityPrefab(GameObject result)
        {
            //voidSingularityPrefab = 
        }

        private void VoidtouchedSingularity(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            if (CommonAssets.voidtouchedSingularity != null)
            {
                CharacterBody victimBody = damageReport.victimBody;
                if (victimBody != null)
                {
                    if (victimBody.HasBuff(DLC1Content.Buffs.EliteVoid))
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
                    }
                }
            }
            else
            {
                Log.Error("Voidtouched singularity null!!");
            }
            orig(self, damageReport);
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
