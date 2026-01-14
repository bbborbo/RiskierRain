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
    class OverloadingReworks : EliteReworkBase<OverloadingReworks>
    {
        [AutoConfig("Bomb Blast Radius", 9f)]
        public static float overloadingBombBlastRadius = 9f;
        [AutoConfig("Bomb Lifetime", 1.35f)]
        public static float overloadingBombLifetime = 1.35f;
        [AutoConfig("Bomb Total Damage Coefficient", "Vanilla is 0.5", 1.5f)]
        public static float overloadingBombDamage = 1.5f; //0.5f

        [AutoConfig("Shield Conversion Fraction", "Set to 0.5 or -1 to disable the hook, which should make it compatible with ZetAspects", 0.33f)]
        public static float overloadingShieldConversionFraction = 0.33f; //5f
        [AutoConfig("Smite Count Base", "Rounded up", 2f)]
        public static float overloadingSmiteCountBase = 2;
        [AutoConfig("Smite Count By Radius", "Rounded up", 1f)]
        public static float overloadingSmiteCountPerRadius = 1f;
        [AutoConfig("Smite Range Base", 18f)]
        public static float overloadingSmiteRangeBase = 18f;
        [AutoConfig("Smite Range By Radius", 9f)]
        public static float overloadingSmiteRangePerRadius = 9f;
        [AutoConfig("Smite Damage Coefficient Initial", 1f)]
        public static float overloadingSmiteStartingDamage = 10f;
        [AutoConfig("Smite Damage Coefficient Per Strike", 1f)]
        public static float overloadingSmiteDamagePerStrike = 5f;
        public override string eliteName => "Overloading";

        public override void Hooks()
        {
            if(overloadingShieldConversionFraction != 0.5f || overloadingShieldConversionFraction < 0)
                IL.RoR2.CharacterBody.RecalculateStats += OverloadingShieldConversion;
            On.RoR2.HealthComponent.TakeDamageProcess += OverloadingKnockbackFix;
            IL.RoR2.GlobalEventManager.OnHitAllProcess += OverloadingBombDamage;
            On.RoR2.GlobalEventManager.OnCharacterDeath += OverloadingSmiteDeath;

            //ChangeLightningStake(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteLightning/LightningStake.prefab").WaitForCompletion());
            AssetReferenceT<GameObject> ref1 = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_EliteLightning.LightningStake_prefab);
            AssetAsyncReferenceManager<GameObject>.LoadAsset(ref1).Completed += (ctx) => ChangeLightningStake(ctx.Result);
        }

        private void ChangeLightningStake(GameObject overloadingBomb)
        {
            ProjectileStickOnImpact bombStick = overloadingBomb.GetComponent<ProjectileStickOnImpact>();
            bombStick.ignoreCharacters = true;
            bombStick.ignoreWorld = false;

            ProjectileImpactExplosion bombPie = overloadingBomb.GetComponent<ProjectileImpactExplosion>();
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
        private void OverloadingSmiteDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
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
                            target = targetHurtBox,

                        });
                        targetsSmited++;
                        smiteDamageCoefficient += overloadingSmiteDamagePerStrike;
                        hurtBoxesList.Remove(hurtBoxesList[i]);
                        hurtBoxCount--;
                    }
                }
            }
            orig(self, damageReport);
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
