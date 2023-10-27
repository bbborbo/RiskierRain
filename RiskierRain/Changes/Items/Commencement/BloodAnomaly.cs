using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Items
{
    class BloodAnomaly : ItemBase<BloodAnomaly>
    {
        public static BuffDef bloodBuff;
        public static GameObject retaliateTracer;
        public static GameObject retaliateExplosion;

        public static float procChancePerPercentBase = 6f;
        public static float procChancePerPercentStack = 4f;
        public static float procChanceMultiplier = 0.5f;

        public static float fractionDamage = 0.03f;
        public static float healFraction = 0.5f;
        public override string ItemName => "Relic of Blood";

        public override string ItemLangTokenName => "BLOODANOMALY";

        public override string ItemPickupDesc => "Gain a chance to retaliate upon taking a large hit, sapping the health of a nearby enemy.";

        public override string ItemFullDescription => $"When taking damage, you gain a " +
            $"<style=cIsDamage>{procChancePerPercentBase * procChanceMultiplier}%</style> " +
            $"<style=cStack>(+{procChancePerPercentStack * procChanceMultiplier}% per stack)</style> chance " +
            $"<style=cIsDamage>per % of maximum health taken</style> " +
            $"to <style=cIsUtility>retaliate</style> against the enemy that hit you, " +
            $"dealing {Tools.ConvertDecimal(fractionDamage)} of their max health " +
            $"and healing for {Tools.ConvertDecimal(healFraction)} of that.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Boss;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist , ItemTag.WorldUnique, ItemTag.CannotSteal };

        public override BalanceCategory Category => BalanceCategory.StateOfCommencement;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.HealthComponent.TakeDamage += BloodAnomalyRetaliate;
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<BloodAnomalyBehavior>(GetCount(self));
            }
        }

        private void BloodAnomalyRetaliate(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);

            if (self.alive && !damageInfo.rejected && damageInfo.procCoefficient > 0 && damageInfo.attacker)
            {
                CharacterBody victimBody = self.body;
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody && victimBody && victimBody.inventory)
                {
                    int itemCount = GetCount(self.body);

                    if (itemCount > 0 && !victimBody.HasBuff(bloodBuff) && victimBody.teamComponent.teamIndex != attackerBody.teamComponent.teamIndex)
                    {
                        BloodAnomalyBehavior bloodAnomaly = victimBody.GetComponent<BloodAnomalyBehavior>();
                        bloodAnomaly.ClearBuffCount();

                        float victimMaxHealth = self.fullCombinedHealth;
                        float attackEndDamage = damageInfo.damage;
                        float retaliationChance = procChancePerPercentBase + procChancePerPercentStack * (itemCount - 1);

                        float maxHealthFractionDealt = Mathf.Min(attackEndDamage / victimMaxHealth * 100, 90);
                        float maxHealthFractionPerRetaliation = Mathf.RoundToInt(100 / retaliationChance);

                        //Debug.Log($"Required: {maxHealthFractionPerRetaliation}% - Dealt: {maxHealthFractionDealt}%");

                        float remainderFractionPercent = maxHealthFractionDealt % maxHealthFractionPerRetaliation;
                        int wholeRetaliations = (int)((maxHealthFractionDealt - remainderFractionPercent) / maxHealthFractionPerRetaliation);
                        //Debug.Log($"Whole: {wholeRetaliations} - Remainder: {remainderFractionPercent * retaliationChance}%");

                        float rollMultiplier = 100;
                        if (wholeRetaliations < 1)
                            rollMultiplier = remainderFractionPercent * retaliationChance;

                        if (Util.CheckRoll(rollMultiplier * procChanceMultiplier * damageInfo.procCoefficient, victimBody.master))
                        {
                            if (rollMultiplier < 1)
                            {
                                wholeRetaliations++;
                            }
                            else if (Util.CheckRoll(remainderFractionPercent * procChanceMultiplier * retaliationChance * damageInfo.procCoefficient, victimBody.master))
                            {
                                wholeRetaliations++;
                            }

                            if (wholeRetaliations > 0)
                            {
                                bloodAnomaly.SetTarget(attackerBody);
                                bloodAnomaly.AddBuffCount(wholeRetaliations);
                            }
                        }
                    }
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            CreateEffects();
            Hooks();
        }

        private void CreateEffects()
        {
            retaliateTracer = Resources.Load<GameObject>("prefabs/effects/tracers/TracerGolem").InstantiateClone("retaliateTracer", false);
            Tracer buckshotTracer = retaliateTracer.GetComponent<Tracer>();
            buckshotTracer.speed = 300f;
            buckshotTracer.length = 15f;
            buckshotTracer.beamDensity = 10f;
            VFXAttributes buckshotAttributes = retaliateTracer.AddComponent<VFXAttributes>();
            buckshotAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            buckshotAttributes.vfxIntensity = VFXAttributes.VFXIntensity.High;

            Tools.GetParticle(retaliateTracer, "SmokeBeam", new Color(0.5f, 0.4f, 0.3f), 0.66f);
            ParticleSystem.MainModule main = retaliateTracer.GetComponentInChildren<ParticleSystem>().main;
            main.startSizeXMultiplier *= 0.5f;
            main.startSizeYMultiplier *= 0.5f;
            main.startSizeZMultiplier *= 2f;

            Assets.CreateEffect(retaliateTracer);

            retaliateExplosion = Resources.Load<GameObject>("prefabs/effects/BleedOnHitAndExplode_Explosion").InstantiateClone("retaliateBlast", false);
            ShakeEmitter shake = retaliateExplosion.GetComponent<ShakeEmitter>();
            shake.radius = 150;
            shake.duration = 0.7f;

            Assets.CreateEffect(retaliateExplosion);
        }

        private void CreateBuff()
        {
            bloodBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                bloodBuff.name = "BloodRetaliateBuff";
                bloodBuff.buffColor = Color.red;
                bloodBuff.canStack = true;
                bloodBuff.isDebuff = true;
                bloodBuff.iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            };
            Assets.buffDefs.Add(bloodBuff);
        }
    }
    public class BloodAnomalyBehavior : RoR2.CharacterBody.ItemBehavior
    {
        public CharacterBody target;
        public int buffCount = 0;

        float chargeMaxTimer = 0.3f;
        float chargeTimerStartMultiplier = 4;
        float currentChargeTimer = 0;

        public void ClearBuffCount()
        {
            buffCount = 0;
            currentChargeTimer = 0;
            target = null;
        }

        public void AddBuffCount(int n)
        {
            for (int i = 0; i < n; i++)
            {
                body.AddBuff(BloodAnomaly.bloodBuff);
                buffCount++;
            }
            currentChargeTimer = GetScaledDelay() * chargeTimerStartMultiplier;
        }
        public void RemoveBuff()
        {
            body.RemoveBuff(BloodAnomaly.bloodBuff);
            buffCount--;
        }

        public void SetTarget(CharacterBody body)
        {
            target = body;
        }

        private void FixedUpdate()
        {
            if (buffCount > 0 && stack > 0)
            {
                while (currentChargeTimer <= 0f)
                {
                    if (body.HasBuff(BloodAnomaly.bloodBuff))
                    {
                        bool flag4 = body.healthComponent.itemCounts.invadingDoppelganger > 0;
                        TeamIndex teamIndex = body.teamComponent.teamIndex;
                        HealthComponent targetHealthComponent = null;

                        if(target != null)
                        {
                            targetHealthComponent = target.healthComponent;
                        }
                        else
                        {
                            HurtBox[] hurtBoxes = new SphereSearch
                            {
                                origin = body.corePosition,
                                radius = 150,
                                mask = LayerIndex.entityPrecise.mask,
                                queryTriggerInteraction = QueryTriggerInteraction.UseGlobal
                            }.RefreshCandidates()
                            .FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamIndex))
                            .OrderCandidatesByDistance().FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes();
                            if (hurtBoxes.Length > 0)
                            {
                                targetHealthComponent = hurtBoxes[0].healthComponent;
                            }
                        }

                        if (targetHealthComponent != null)
                        {
                            RemoveBuff();

                            float percentDamage = targetHealthComponent.fullCombinedHealth * BloodAnomaly.fractionDamage;
                            float endDamage = Mathf.Clamp(percentDamage, 2 * body.damage, 20 * body.damage);

                            Vector3 startPos = body.corePosition;
                            Vector3 endPos = targetHealthComponent.body.corePosition;

                            #region attack
                            new BulletAttack
                            {
                                weapon = body.gameObject,
                                origin = startPos,
                                aimVector = (endPos - startPos).normalized,
                                minSpread = 0f,
                                maxSpread = 0f,
                                damage = endDamage,
                                damageType = DamageType.BypassArmor,
                                procCoefficient = flag4 ? 0f : 0.5f,
                                force = 0,
                                tracerEffectPrefab = BloodAnomaly.retaliateTracer,
                                isCrit = false,
                                radius = 0.25f,
                                falloffModel = BulletAttack.FalloffModel.None,
                                smartCollision = true
                            }.Fire();

                            EffectManager.SpawnEffect(BloodAnomaly.retaliateExplosion, new EffectData
                            {
                                origin = endPos,
                                scale = 1
                            }, true);
                            #endregion

                            body.healthComponent.Heal(endDamage * BloodAnomaly.healFraction, default(ProcChainMask));
                        }

                        currentChargeTimer += GetScaledDelay();
                    }
                    else
                    {
                        ClearBuffCount();
                        break;
                    }
                }

                if (currentChargeTimer > 0f)
                {
                    currentChargeTimer -= Time.fixedDeltaTime;
                }
            }
            else
            {
                ClearBuffCount();
            }
        }

        private float GetScaledDelay()
        {
            return chargeMaxTimer / body.attackSpeed;
        }
    }
}
