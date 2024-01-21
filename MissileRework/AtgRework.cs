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
        public float procChance = 15;
        public float atgMk3BaseDamageCoefficientPerRocket = 3;
        static float atgMk3TotalDamageMultiplierBase = 0.0f;
        static float atgMk3TotalDamageMultiplierStack = 1.5f;
        static int maxMissiles = 100;
        string damagePerStack = (atgMk3TotalDamageMultiplierStack * 100).ToString() + "%";
        string damageBase = (atgMk3TotalDamageMultiplierBase + atgMk3TotalDamageMultiplierStack).ToString() + "%";

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

            if (Util.CheckRoll(procChance, attackerMaster))
            {
                //calculates the combined damage for all missiles in the proc
                float atgTotalDamage = damageInfo.damage * (atgMk3TotalDamageMultiplierBase + atgMk3TotalDamageMultiplierStack * missileItemCount);
                float atgDamagePerRocket = atgMk3BaseDamageCoefficientPerRocket * attackerBody.damage;
                float atgDamageRemainder = atgTotalDamage % atgDamagePerRocket;

                int totalMissilesToFire = (int)((atgTotalDamage - atgDamageRemainder) / atgDamagePerRocket);
                if(atgDamageRemainder > 0)
                {
                    float remainderFraction = atgDamageRemainder / atgDamagePerRocket;
                    if (Util.CheckRoll(procChance * remainderFraction, attackerMaster))
                    {
                        totalMissilesToFire++;
                    }
                }

                totalMissilesToFire = Mathf.Min(totalMissilesToFire, maxMissiles - missileLauncher.currentMissiles.Count);

                if (totalMissilesToFire > 0)
                {
                    int currentMissiles = missileLauncher.currentMissiles.Count;
                    List<FireProjectileInfo> missilesToFire = new List<FireProjectileInfo>();
                    for (int i = 0; i < totalMissilesToFire; i++)
                    {
                        FireProjectileInfo newMissile = NewMissile(atgMk3BaseDamageCoefficientPerRocket, damageInfo, attackerBody, victim);

                        missilesToFire.Add(newMissile);
                        if (missilesToFire.Count + currentMissiles > maxMissiles)
                        {
                            int remainingMissiles = missilesToFire.Count + currentMissiles - maxMissiles;
                            Debug.Log($"Discarded {remainingMissiles} missiles!");
                            break;
                        }
                    }

                    missileLauncher.SetMissiles(missilesToFire);
                }
            }
        }
        public static FireProjectileInfo NewMissile(float damage, DamageInfo damageInfo, CharacterBody attackerBody, GameObject victim)
        {
            GameObject gameObject = attackerBody.gameObject;
            InputBankTest component = gameObject.GetComponent<InputBankTest>();
            Vector3 position = component ? component.aimOrigin : gameObject.transform.position;
            Vector3 up = Vector3.up;
            float rotationVariance = UnityEngine.Random.Range(0.1f, 0.5f); //0.1f

            float rocketDamage = attackerBody.damage * damage;
            ProcChainMask procChainMask2 = damageInfo.procChainMask;
            procChainMask2.AddProc(ProcType.Missile);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = missilePrefab,
                position = position,
                rotation = Util.QuaternionSafeLookRotation(up + UnityEngine.Random.insideUnitSphere * rotationVariance),
                procChainMask = procChainMask2,
                owner = gameObject,
                damage = rocketDamage,
                crit = damageInfo.crit,
                force = 200f,
                damageColorIndex = DamageColorIndex.Item,
                target = victim
            };

            return (fireProjectileInfo);
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

        public void SetMissiles(List<FireProjectileInfo> newMissiles, bool replace = false)
        {
            if (replace == true)
            {
                currentMissiles = new List<FireProjectileInfo>(newMissiles);
            }
            else
            {
                for (int i = 0; i < newMissiles.Count; i++)
                {
                    currentMissiles.Add(newMissiles[i]);
                }
            }
            currentMissileTimer += GetScaledDelay();
        }

        private void FixedUpdate()
        {
            if (currentMissiles.Count > 0 && stack > 0)
            {
                while (currentMissileTimer <= 0f)
                {
                    FireProjectileInfo missile = currentMissiles[0];
                    missile.position = body.gameObject.transform.position;
                    missile.rotation = Util.QuaternionSafeLookRotation(Vector3.up + UnityEngine.Random.insideUnitSphere * missileSpread);
                    missileSpread += (missileSpreadMax - missileSpread) * missileSpreadFraction;

                    ProjectileManager.instance.FireProjectile(missile);

                    List<FireProjectileInfo> newMissileList = new List<FireProjectileInfo>(currentMissiles);
                    newMissileList.RemoveAt(0);
                    //Debug.Log(newMissileList.Count);
                    SetMissiles(newMissileList, true);
                }

                if (this.currentMissileTimer > 0f)
                {
                    currentMissileTimer -= Time.fixedDeltaTime;
                }
            }
            else
            {
                currentMissileTimer = 0;
                missileSpread = 0;
            }
        }
        private float GetScaledDelay()
        {
            return missileMaxTimer / body.attackSpeed;
        }
    }
}
