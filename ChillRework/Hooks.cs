using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.DamageAPI;
using static R2API.RecalculateStatsAPI;

namespace ChillRework
{
    public partial class ChillRework : BaseUnityPlugin
    {
        public void FixSnapfreeze()
        {
            GameObject iceWallPillarPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIcewallPillarProjectile");
            ProjectileImpactExplosion pie = iceWallPillarPrefab.GetComponentInChildren<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.destroyOnEnemy = false;
            }
        }

        #region chill on hit
        private readonly Dictionary<GameObject, GameObject> frozenBy = new Dictionary<GameObject, GameObject>();
        /// <summary>
        /// attacker, victim
        /// </summary>
        public static event Action<CharacterBody, CharacterBody> OnMaxChill;
        public void ChillHooks()
        {
            On.RoR2.GlobalEventManager.ProcessHitEnemy += ChillOnHitHook;
            On.RoR2.CharacterBody.FixedUpdate += UpdateMaxChill;
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += IceRingMultiChill;
            //IL.RoR2.CharacterBody.RecalculateStats += ChillStatRework;
            GetStatCoefficients += ChillStats;
        }

        private void UpdateMaxChill(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            orig(self);
            if (self.GetBuffCount(RoR2Content.Buffs.Slow80.buffIndex) >= chillStacksMax)
            {
                if (NetworkServer.active)
                {
                    //clear current chill stacks
                    self.ClearTimedBuffs(RoR2Content.Buffs.Slow80.buffIndex);
                    //set to frozen for 2 seconds
                    SetStateOnHurt component = self.healthComponent.GetComponent<SetStateOnHurt>();
                    if (component != null)
                    {
                        component.SetFrozen(2f);
                    }

                    //add 3 stacks of chill limiter, this will cap chill at 1 for 0.2 seconds, then 4 for 0.2, then 7 for 0.2, then 10
                    for(int i = 1; i < 4; i++)
                    {
                        self.AddTimedBuff(ChillLimitBuff, 0.2f * i);
                    }
                    //apply a single stack of chill just to maintain synergies
                    ApplyChillStacks(self, 100);
                }

                GameObject lastAttacker = self.healthComponent.lastHitAttacker;
                if (lastAttacker != null && lastAttacker.TryGetComponent(out CharacterBody aBody))
                {
                    OnMaxChill?.Invoke(aBody, self);
                }
            }
        }

        private void ChillStats(CharacterBody sender, StatHookEventArgs args)
        {
            int chillCount = sender.GetBuffCount(RoR2Content.Buffs.Slow80);
            if(chillCount > 0)
            {
                args.moveSpeedReductionMultAdd -= 0.8f;
                args.moveSpeedReductionMultAdd += CalculateChillSlowCoefficient(chillCount);
            }
        }

        private void ChillStatRework(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR.RoR2Content/Buffs", "Slow80")
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(out _) //0.8, chill slow coefficient
                );
            c.Emit(OpCodes.Ldarg_0);//CharacterBody self
            c.EmitDelegate<Func<float, CharacterBody, float>>((baseSlowCoefficient, body) =>
            {
                int chillStacks = body.GetBuffCount(RoR2Content.Buffs.Slow80);
                float newSlowCoefficient = CalculateChillSlowCoefficient(chillStacks, baseSlowCoefficient);

                return newSlowCoefficient;
            });
        }

        private void IceRingMultiChill(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLocation = 51;

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "IceRing")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out itemCountLocation)
                );

            int victimBodyLocation = 2;

            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out victimBodyLocation),
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "Slow80")
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.AddTimedBuff))
                );
            c.Remove();
            c.EmitDelegate<Action<CharacterBody, BuffDef, float>>((victimBody, buffDef, duration) =>
            {
                ApplyChillStacks(victimBody, 100, 3, duration);
            });
        }

        private void ChillOnHitHook(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            CharacterMaster attackerMaster = null;
            if(damageInfo.attacker != null)
            {
                CharacterBody aBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if(aBody != null)
                {
                    attackerMaster = aBody.master;
                }
            }

            if(victim != null)
            {
                CharacterBody vBody = victim?.GetComponent<CharacterBody>();
                if (vBody != null)
                {
                    float procCoefficient = damageInfo.procCoefficient;
                    if (procCoefficient != 0 && !damageInfo.rejected)
                    {
                        if (damageInfo.damageType.damageType.HasFlag(DamageType.Freeze2s))
                        {
                            this.frozenBy[victim] = damageInfo.attacker;
                            float chillCount = chillStacksOnFreeze;
                            if (damageInfo.damageType.damageType.HasFlag(DamageType.AOE))
                            {
                                chillCount -= 1;
                            }
                            ApplyChillStacks(attackerMaster, vBody, procCoefficient * 100, chillCount);
                        }
                        else
                        {
                            bool chillOnHit = damageInfo.HasModdedDamageType(ChillOnHit);
                            bool multiChill = damageInfo.HasModdedDamageType(MultiChillOnHit);
                            if(chillOnHit || multiChill)
                            {
                                if(chillOnHit)
                                    damageInfo.RemoveModdedDamageType(ChillOnHit);
                                if(multiChill)
                                    damageInfo.RemoveModdedDamageType(MultiChillOnHit);
                                float procChance = chillProcChance * procCoefficient * 100;

                                ApplyChillStacks(attackerMaster, vBody, procChance, chillCount: (multiChill ? 3 : 1));
                            }
                        }
                    }
                }
            }
            orig(self, damageInfo, victim);
        }
        #endregion
    }
}
