using Mono.Cecil.Cil;
using MonoMod.Cil;
using ProcSolver;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static MoreStats.OnHit;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MissileRework
{
    public partial class MissileReworkPlugin
    {
        public static GameObject missilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/MissileProjectile");
        public float procCoefficient = 0.5f;
        public float procChance = 10;
        public static float atgMk3BaseDamageCoefficientPerRocket = 3;
        static int maxMissiles = 100;
        string damagePerMissile = (atgMk3BaseDamageCoefficientPerRocket * 100).ToString() + "%";
        string overspillThreshold = (overspillThresholdCoefficient * 100).ToString() + "%";
        public static float overspillThresholdCoefficient = 2;
        public static int missilesPerOverspillBase = 1;
        public static int missilesPerOverspillStack = 1;

        internal void ReworkAtg()
        {
            missilePrefab.GetComponent<ProjectileController>().procCoefficient = procCoefficient;

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += RemoveVanillaAtgLogic;

            GetHitBehavior += AtgReworkLogic;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;

            LanguageAPI.Add("ITEM_MISSILE_NAME", "AtG Missile Mk.3");
            LanguageAPI.Add("ITEM_MISSILE_PICKUP", "Chance to fire a volley of missiles. Missiles fired are increased by higher damage hits.");
            LanguageAPI.Add("ITEM_MISSILE_DESC", 
                $"<style=cIsDamage>{procChance}%</style> chance to fire a volley of " +
                $"<style=cIsDamage>{missilesPerOverspillBase}</style> <style=cStack>(+{missilesPerOverspillStack} per stack)</style> missiles on hit " +
                $"for <style=cIsDamage>{damagePerMissile}</style> base damage each. " +
                $"Every <style=cIsDamage>{overspillThreshold}</style> attack damage dealt increases " +
                $"volleys loaded by <style=cIsDamage>1</style>."
            );
        }

        private void AtgReworkLogic(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (!damageInfo.procChainMask.HasProc(ProcType.Missile))
            {
                CharacterMaster attackerMaster = attackerBody.master;
                Inventory inv = attackerBody.inventory;
                if (attackerMaster != null && inv != null)
                {
                    int missileItemCount = inv.GetItemCount(RoR2Content.Items.Missile);
                    if(missileItemCount > 0 && Util.CheckRoll(procChance * GetProcRate(damageInfo), attackerMaster))
                    {
                        DoMissileProc(damageInfo, victimBody.gameObject, attackerBody, attackerMaster, missileItemCount);
                    }
                }
            }
        }
        private float GetProcRate(DamageInfo damageInfo)
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos[ProcSolverPlugin.guid] == null)
            {
                return 1;
            }
            return _GetProcRate(damageInfo);
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private float _GetProcRate(DamageInfo damageInfo)
        {
            float mod = ProcSolverPlugin.GetProcRateMod();
            return mod;
        }

        #region mundane stuff
        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<Mk3MissileBehavior>(self.inventory.GetItemCount(RoR2Content.Items.Missile));
            }
        }
        private void RemoveVanillaAtgLogic(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Missile"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }
        #endregion

        public static int CalculateOverspill(DamageInfo damageInfo, float attackerBodyDamage, float overspillThreshold, bool rollForMore = false)
        {
            int overspillCount = 0;

            float damageCoefficient = damageInfo.damage / attackerBodyDamage;
            overspillCount = Mathf.FloorToInt(damageCoefficient / overspillThreshold);
            if(!rollForMore)
                return overspillCount;
            float overspillRemainder = damageCoefficient % overspillThreshold;
            if (overspillRemainder <= Mathf.Epsilon)
                return overspillCount;

            if (Util.CheckRoll0To1(overspillRemainder / overspillThreshold))
                overspillCount++;

            return overspillCount;
        }

        private void DoMissileProc(DamageInfo damageInfo, GameObject victim, CharacterBody attackerBody, CharacterMaster attackerMaster, int missileItemCount)
        {
            if (missileItemCount <= 0)
                return;

            Mk3MissileBehavior missileLauncher = attackerBody.gameObject.GetComponent<Mk3MissileBehavior>();
            if (missileLauncher == null)
                return;

            //calculates the combined damage for all missiles in the proc
            //float atgTotalDamage = damageInfo.damage * (atgMk3TotalDamageMultiplierBase + atgMk3TotalDamageMultiplierStack * missileItemCount);
            //float atgDamagePerRocket = atgMk3BaseDamageCoefficientPerRocket * attackerBody.damage;
            //float atgDamageRemainder = atgTotalDamage % atgDamagePerRocket;
            //
            //int totalMissilesToFire = (int)((atgTotalDamage - atgDamageRemainder) / atgDamagePerRocket);
            //if (atgDamageRemainder > 0)
            //{
            //    float remainderFraction = atgDamageRemainder / atgDamagePerRocket;
            //    if (Util.CheckRoll(remainderFraction * 100, 0))
            //    {
            //        totalMissilesToFire++;
            //    }
            //}

            int overspillCount = 1 + CalculateOverspill(damageInfo, attackerBody.damage, overspillThresholdCoefficient);
            int missilesPerOverspill = missilesPerOverspillBase + missilesPerOverspillStack * (missileItemCount - 1);
            int totalMissilesToFire = overspillCount * missilesPerOverspill;

            FireProjectileInfo newMissile = new FireProjectileInfo
            {
                projectilePrefab = missilePrefab,
                procChainMask = damageInfo.procChainMask,
                damage = atgMk3BaseDamageCoefficientPerRocket * attackerBody.damage,
                crit = damageInfo.crit,
                target = victim
            };

            missileLauncher.AddMissiles(newMissile, Mathf.Min(totalMissilesToFire, maxMissiles - missileLauncher.currentMissiles.Count));

            /*int currentMissiles = missileLauncher.currentMissiles.Count;
            List<FireProjectileInfo> missilesToFire = new List<FireProjectileInfo>();
            for (int i = 0; i < totalMissilesToFire; i++)
            {
                missilesToFire.Add(newMissile);
            }

            missileLauncher.SetMissiles(missilesToFire);*/
        }
    }
    public class Mk3MissileBehavior : RoR2.CharacterBody.ItemBehavior
    {
        public List<FireProjectileInfo> currentMissiles = new List<FireProjectileInfo>(0);

        float missileMaxTimer = 0.075f;
        float currentMissileTimer = 0;
        float missileSpread = 0;
        float missileSpreadFraction = 0.33f;
        float missileSpreadMax = 0.6f;

        public void AddMissiles(FireProjectileInfo newMissile, int count)
        {
            while (count > 0)
            {
                count--;
                currentMissiles.Add(newMissile);
            }
        }

        private void FixedUpdate()
        {
            if (currentMissiles.Count > 0 && stack > 0)
            {
                while (currentMissileTimer <= 0f)
                {
                    FireProjectileInfo missile = currentMissiles[0];
                    missile.position = body.gameObject.transform.position;
                    missileSpread += (missileSpreadMax - missileSpread) * missileSpreadFraction;

                    //ProjectileManager.instance.FireProjectile(missile);
                    MissileUtils.FireMissile(body.corePosition, body, missile.procChainMask, missile.target, 
                        missile.damage, missile.crit, missile.projectilePrefab, DamageColorIndex.Item, Vector3.up + UnityEngine.Random.insideUnitSphere * missileSpread, 200f, true);

                    currentMissiles.RemoveAt(0);
                    currentMissileTimer += GetScaledDelay();
                }

                if (this.currentMissileTimer > 0f)
                {
                    currentMissileTimer -= Time.fixedDeltaTime;
                }
            }
            else
            {
                currentMissileTimer = GetScaledDelay();
                missileSpread = 0;
            }
        }
        private float GetScaledDelay()
        {
            return missileMaxTimer / body.attackSpeed;
        }
    }
}
