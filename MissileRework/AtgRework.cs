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
        public float atgMk3BaseDamageCoefficientPerRocket = 3;
        static float atgMk3TotalDamageMultiplierBase = 0.0f;
        static float atgMk3TotalDamageMultiplierStack = 1.5f;
        static int maxMissiles = 100;
        string damagePerStack = (atgMk3TotalDamageMultiplierStack * 100).ToString() + "%";
        string damageBase = ((atgMk3TotalDamageMultiplierBase + atgMk3TotalDamageMultiplierStack) * 100).ToString() + "%";

        internal void ReworkAtg()
        {
            missilePrefab.GetComponent<ProjectileController>().procCoefficient = procCoefficient;

            IL.RoR2.GlobalEventManager.OnHitEnemy += RemoveVanillaAtgLogic;

            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.GlobalEventManager.OnHitEnemy += AtgReworkLogic;

            LanguageAPI.Add("ITEM_MISSILE_NAME", "AtG Missile Mk.3");
            LanguageAPI.Add("ITEM_MISSILE_PICKUP", "Chance to fire missiles.");
            LanguageAPI.Add("ITEM_MISSILE_DESC", $"<style=cIsDamage>{procChance}%</style> chance to fire a volley of missiles on hit, " +
            $"that deal <style=cIsDamage>{damageBase}</style> <style=cStack>(+{damagePerStack} per stack)</style> TOTAL combined damage.");
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

            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Missile"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );
            c.Index--;
            c.Remove();
            c.Remove();
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, 0);
        }
        #endregion

        void AtgReworkLogic(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);

            if (damageInfo.attacker && damageInfo.procCoefficient > 0f && !damageInfo.procChainMask.HasProc(ProcType.Missile))
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim ? victim.GetComponent<CharacterBody>() : null;
                if (attackerBody)
                {
                    CharacterMaster attackerMaster = attackerBody.master;
                    Inventory inv = attackerBody.inventory;
                    if (attackerMaster != null && inv != null)
                    {
                        int missileItemCount = inv.GetItemCount(RoR2Content.Items.Missile);
                        TeamComponent teamComponent = attackerBody.GetComponent<TeamComponent>();
                        TeamIndex team = teamComponent ? teamComponent.teamIndex : TeamIndex.Neutral;

                        DoMissileProc(damageInfo, victim, attackerBody, attackerMaster, missileItemCount);
                    }
                }
            }
        }

        private void DoMissileProc(DamageInfo damageInfo, GameObject victim, CharacterBody attackerBody, CharacterMaster attackerMaster, int missileItemCount)
        {
            if (missileItemCount <= 0)
                return;

            Mk3MissileBehavior missileLauncher = attackerBody.gameObject.GetComponent<Mk3MissileBehavior>();
            if (missileLauncher == null)
                return;

            //calculates the combined damage for all missiles in the proc
            float atgTotalDamage = damageInfo.damage * (atgMk3TotalDamageMultiplierBase + atgMk3TotalDamageMultiplierStack * missileItemCount);
            float atgDamagePerRocket = atgMk3BaseDamageCoefficientPerRocket * attackerBody.damage;
            float atgDamageRemainder = atgTotalDamage % atgDamagePerRocket;

            int totalMissilesToFire = (int)((atgTotalDamage - atgDamageRemainder) / atgDamagePerRocket);
            if (atgDamageRemainder > 0)
            {
                float remainderFraction = atgDamageRemainder / atgDamagePerRocket;
                if (Util.CheckRoll(remainderFraction * 100, 0))
                {
                    totalMissilesToFire++;
                }
            }

            if (Util.CheckRoll(procChance, attackerMaster) && totalMissilesToFire > 0)
            {
                FireProjectileInfo newMissile = new FireProjectileInfo
                {
                    projectilePrefab = missilePrefab,
                    procChainMask = damageInfo.procChainMask,
                    damage = atgDamagePerRocket,
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
            for (int i = 0; i < count; i++)
            {
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
