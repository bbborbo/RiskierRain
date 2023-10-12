using BepInEx;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.DamageAPI;

namespace ChillRework
{
    public partial class ChillReworkPlugin : BaseUnityPlugin
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
        /// damageInfo, victim
        /// </summary>
        public static event Action<DamageInfo, GameObject> OnMaxChill;
        public void ChillHooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += ChillOnHitHook;
            On.RoR2.CharacterBody.AddBuff_BuffIndex += MaxChillStacks;
        }

        private void ChillOnHitHook(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            CharacterBody vBody = victim?.GetComponent<CharacterBody>();
            CharacterBody aBody = damageInfo.attacker?.GetComponent<CharacterBody>();

            if (vBody != null && aBody != null && damageInfo.procCoefficient != 0 && !damageInfo.rejected)
            {
                if (damageInfo.damageType.HasFlag(DamageType.Freeze2s))
                {
                    this.frozenBy[victim] = damageInfo.attacker;
                    float chillCount = chillStacksOnFreeze;
                    if (damageInfo.damageType.HasFlag(DamageType.AOE))
                    {
                        chillCount -= 1;
                    }
                    for (int i = 0; i < chillCount; i++)
                    {
                        if (Util.CheckRoll(damageInfo.procCoefficient * 100, aBody.master))
                        {
                            vBody.AddTimedBuffAuthority(RoR2Content.Buffs.Slow80.buffIndex, chillProcDuration);
                        }
                    }
                }
                else
                {

                    if (damageInfo.HasModdedDamageType(ChillOnHit))//(damageInfo.damageType.HasFlag(DamageType.SlowOnHit))
                    {
                        damageInfo.RemoveModdedDamageType(ChillOnHit);
                        float procChance = Mathf.Min(1, chillProcChance * damageInfo.procCoefficient * damageInfo.procCoefficient) * 100;

                        if (Util.CheckRoll(procChance, aBody.master))
                        {
                            vBody.AddTimedBuffAuthority(RoR2Content.Buffs.Slow80.buffIndex, chillProcDuration);
                        }
                    }
                }
            }
            int chillDebuffCount = vBody.GetBuffCount(RoR2Content.Buffs.Slow80);
            if (chillDebuffCount >= chillStacksMax) //Arctic Blast
            {
                OnMaxChill?.Invoke(damageInfo, victim);
                /*vBody.ClearTimedBuffs(RoR2Content.Buffs.Slow80);
                AltArtiPassive.DoNova(aBody, icePower, damageInfo.position, AltArtiPassive.novaDebuffThreshold);*/
            }
            orig(self, damageInfo, victim);
        }

        private void MaxChillStacks(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
        {
            if (buffType == RoR2Content.Buffs.Slow80.buffIndex && self.GetBuffCount(RoR2Content.Buffs.Slow80.buffIndex) >= chillStacksMax)
            {
                return;
            }
            orig(self, buffType);
        }
        #endregion
    }
}
