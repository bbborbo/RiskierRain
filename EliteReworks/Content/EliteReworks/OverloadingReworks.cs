using FruityElites.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreStats;
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
using static MoreStats.StatHooks;

namespace FruityElites.EliteReworks
{
    class OverloadingReworks : EliteReworkBase<OverloadingReworks>
    {
        [AutoConfig("Bomb On World Impact: Blast Radius", 8f)]
        public static float overloadingBombBlastRadius =  8f;
        [AutoConfig("Bomb On World Impact: Lifetime", 1.25f)]
        public static float overloadingBombLifetime = 1.25f;
        [AutoConfig("Bomb On World Impact: Total Damage Coefficient", "Vanilla is 0.5", 1.0f)]
        public static float overloadingBombDamage = 1.0f; //0.5f

        [AutoConfig("Shield Conversion Fraction", "Set to 0.5 or -1 to disable the hook, which should make it compatible with ZetAspects", 0.33f)]
        public static float overloadingShieldConversionFraction = 0.33f; //5f
        [AutoConfig("Shield Recharge Delay", "Seconds to increase shield recharge delay. Vanilla is 0", 1f)]
        public static float overloadingShieldRechargeDelay = 1f; //0f
        [AutoConfig("Shield Recharge Delay", "Seconds to further increase shield recharge delay for Champion/Boss enemies. Vanilla is 0", 1f)]
        public static float overloadingShieldRechargeDelayChampions = 1f; //0f
        [AutoConfig("Smite On Death: Count Base", "Rounded up", 2f)]
        public static float overloadingSmiteCountBase = 2;
        [AutoConfig("Smite On Death: Count By Radius", "Rounded up", 1f)]
        public static float overloadingSmiteCountPerRadius = 1f;
        [AutoConfig("Smite On Death: Max Range Base", 18f)]
        public static float overloadingSmiteRangeBase = 18f;
        [AutoConfig("Smite On Death: Max Range By Radius", "How much to scale the smite range by unit of body size", 9f)]
        public static float overloadingSmiteRangePerRadius = 9f;
        [AutoConfig("Smite On Death: Damage Coefficient Initial", 10f)]
        public static float overloadingSmiteStartingDamage = 10f;
        [AutoConfig("Smite On Death: Damage Coefficient Per Strike", 5f)]
        public static float overloadingSmiteDamagePerStrike = 5f;
        public override string eliteName => "Overloading";

        public override void Init()
        {
            base.Init();
        }

        public override void Hooks()
        {
            //if(BaseStats.ApplyShieldConversionHook)
            //    BaseStats.OverloadingShieldConversionFraction = overloadingShieldConversionFraction;
            //else { }
                if(overloadingShieldConversionFraction != 0.5f && overloadingShieldConversionFraction > 0)
                IL.RoR2.CharacterBody.RecalculateStats += OverloadingShieldConversion;

            On.RoR2.HealthComponent.TakeDamageProcess += OverloadingKnockbackFix;
            IL.RoR2.GlobalEventManager.OnHitAllProcess += OverloadingBombDamage;
            RoR2.GlobalEventManager.onCharacterDeathGlobal += OverloadingSmiteOnDeath;
            GetMoreStatCoefficients += ShieldRecharge;

            EliteReworksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_EliteLightning.LightningStake_prefab, ChangeLightningStake);
            EliteReworksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_EliteLightning.LightningStakeGhost_prefab, ChangeLightningStakeGhost);
        }

        private void ShieldRecharge(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.HasBuff(RoR2Content.Buffs.AffixBlue))
            {
                args.shieldDelaySecondsIncreaseAddPreMult += overloadingShieldRechargeDelay;
                if (sender.isChampion)
                    args.shieldDelaySecondsIncreaseAddPreMult += overloadingShieldRechargeDelayChampions;
            }
        }

        private void OverloadingSmiteOnDeath(DamageReport damageReport)
        {
            CharacterBody victimBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (victimBody != null && attackerBody != null)
            {
                if (victimBody.HasBuff(RoR2Content.Buffs.AffixBlue))
                {
                    int maxStrikeCount = Mathf.CeilToInt(overloadingSmiteCountBase + victimBody.bestFitRadius * overloadingSmiteCountPerRadius);
                    float range = overloadingSmiteRangeBase + victimBody.radius * overloadingSmiteRangePerRadius;
                    float baseDamage = attackerBody.baseDamage;
                    float smiteDamageCoefficient = 5f;
                    ProcChainMask procChainMask6 = damageReport.damageInfo.procChainMask;
                    //procChainMask6.AddProc(ProcType.LightningStrikeOnHit);

                    SphereSearch sphereSearch = new SphereSearch
                    {
                        mask = LayerIndex.entityPrecise.mask,
                        origin = victimBody.transform.position,
                        queryTriggerInteraction = QueryTriggerInteraction.Collide,
                        radius = range
                    };

                    TeamMask teamMask = TeamMask.GetEnemyTeams(TeamIndex.Player);
                    List<HurtBox> hurtBoxesList = new List<HurtBox>();

                    sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(hurtBoxesList);

                    int hurtBoxCount = hurtBoxesList.Count;
                    if (hurtBoxCount == 0)
                    {
                        OrbManager.instance.AddOrb(new LightningStrikeOrb
                        {
                            attacker = attackerBody.gameObject,
                            damageColorIndex = DamageColorIndex.Default,
                            damageValue = baseDamage * smiteDamageCoefficient,
                            isCrit = damageReport.damageInfo.crit,
                            procChainMask = procChainMask6,
                            procCoefficient = 0.5f,
                            target = damageReport.victimBody.mainHurtBox
                        });
                    }
                    else
                    {
                        int targetsSmited = 0;
                        while (hurtBoxCount > 0 && targetsSmited < maxStrikeCount)
                        {
                            int i = UnityEngine.Random.Range(0, hurtBoxCount - 1);
                            HurtBox targetHurtBox = hurtBoxesList[i];
                            HealthComponent healthComponent = targetHurtBox.healthComponent;
                            CharacterBody enemyBody = healthComponent.body;
                            if (enemyBody.isPlayerControlled)
                                continue;

                            if (!enemyBody || enemyBody == victimBody)
                            {
                                hurtBoxesList.Remove(hurtBoxesList[i]);
                                hurtBoxCount--;
                                continue;
                            }

                            OrbManager.instance.AddOrb(new LightningStrikeOrb
                            {
                                attacker = attackerBody.gameObject,
                                damageColorIndex = DamageColorIndex.Default,
                                damageValue = baseDamage * smiteDamageCoefficient,
                                isCrit = damageReport.damageInfo.crit,
                                procChainMask = procChainMask6,
                                procCoefficient = 0.5f,
                                target = targetHurtBox
                            });
                            targetsSmited++;
                            smiteDamageCoefficient += overloadingSmiteDamagePerStrike;
                            hurtBoxesList.Remove(hurtBoxesList[i]);
                            hurtBoxCount--;
                        }
                    }
                }
            }
        }

        private void ChangeLightningStakeGhost(GameObject lightningStakeGhost)
        {
            GameObject sphere = lightningStakeGhost.transform.GetChild(0).gameObject;
            if(sphere != null)
            {
                EliteReworksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_EliteIce.AffixWhiteDelayEffect_prefab, (delayEffect) =>
                {
                    Transform novaSphere = delayEffect.transform.Find("Nova Sphere");
                    if(novaSphere != null)
                    {
                        GameObject telegraphRealified = UnityEngine.GameObject.Instantiate(novaSphere.gameObject);
                        telegraphRealified.transform.parent = sphere.transform;
                        telegraphRealified.transform.localScale = Vector3.one * overloadingBombBlastRadius * 1.75f;

                        if(telegraphRealified.TryGetComponent(out ParticleSystemRenderer psr))
                        {
                            Material mat = UnityEngine.Object.Instantiate(psr.material);
                            mat.SetColor("_TintColor", new Color32(111, 191, 255, 255));
                            mat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_ColorRamps.texRampFogStage1_png).WaitForCompletion());
                            mat.SetTexture("_Cloud1Tex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_TiledTextures.texCloudWhitenoiseSubtle_png).WaitForCompletion());
                            mat.SetFloat("_SoftFactor", 8.17f);
                            mat.SetFloat("_SoftPower", 1.48f);
                            mat.SetFloat("_BrightnessBoost", 2.15f);
                            mat.SetFloat("_RimPower", 2.98f);
                            mat.SetFloat("_RimStrength", 0.24f);
                            mat.SetFloat("_AlphaBoost", 0.46f);
                            mat.SetFloat("_IntersectionStrength", 15.51f);
                            psr.material = mat;
                        }
                    }
                });
            }
        }

        private void ChangeLightningStake(GameObject lightningStake)
        {
            ProjectileStickOnImpact bombStick = lightningStake.GetComponent<ProjectileStickOnImpact>();
            bombStick.ignoreCharacters = true;
            bombStick.ignoreWorld = false;

            ProjectileImpactExplosion bombPie = lightningStake.GetComponent<ProjectileImpactExplosion>();
            bombPie.blastRadius = overloadingBombBlastRadius;
            bombPie.lifetime = overloadingBombLifetime;
        }

        private void OverloadingShieldConversion(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int shieldsTotalLoc = 72;
            int overloadingShieldConversionLoc = 73;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "AffixBlue")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxHealth")
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, overloadingShieldConversionFraction);
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out overloadingShieldConversionLoc)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("set_maxShield")
                );
            c.GotoPrev(MoveType.Before,
                x => x.MatchLdloc(out shieldsTotalLoc)
                );

            c.GotoPrev(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxHealth"),
                x => x.MatchAdd(),
                x => x.MatchStloc(shieldsTotalLoc)
                );
            c.Remove();
            c.Remove();
            c.Emit(OpCodes.Ldloc, overloadingShieldConversionLoc);
        }

        private void OverloadingBombDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "AffixBlue")
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnHitProcDamage))
                );
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, overloadingBombDamage);
        }

        private void OverloadingKnockbackFix(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if (damageInfo.attacker)
            {
                CharacterBody aBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (aBody)
                {
                    if (aBody.HasBuff(RoR2Content.Buffs.AffixBlue))
                    {
                        damageInfo.force *= 0.25f;
                    }
                }
            }
            orig(self, damageInfo);
        }
    }
}
