using BepInEx;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRain.CoreModules;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        float softEliteHealthBoostCoefficient = 2f; //3
        float hardEliteHealthBoostCoefficient = 4f; //5
        float baseEliteHealthBoostCoefficient = 3f; //4
        float T2EliteHealthBoostCoefficient = 9; //18
        float baseEliteDamageBoostCoefficient = 1.5f; //2
        float T2EliteDamageBoostCoefficient = 4.5f; //6
        public static float overloadingBombDamage = 1.5f; //0.5f

        public static int Tier2EliteMinimumStageDefault = 5;
        public static int Tier2EliteMinimumStageDrizzle = 10;
        public static int Tier2EliteMinimumStageRainstorm = 5;
        public static int Tier2EliteMinimumStageMonsoon = 3;
        public static int Tier2EliteMinimumStageEclipse = 3;

        static string Tier2EliteName = "Tier 2";

        void ChangeEliteStats()
        {
            if(Tier2EliteMinimumStageDrizzle != Tier2EliteMinimumStageDefault 
                || Tier2EliteMinimumStageMonsoon != Tier2EliteMinimumStageDefault
                || Tier2EliteMinimumStageEclipse != Tier2EliteMinimumStageDefault)
            {
                drizzleDesc += $"\n>{Tier2EliteName} Elites appear starting on <style=cIsHealing>Stage {Tier2EliteMinimumStageDrizzle + 1}</style>";
                rainstormDesc += $"\n>{Tier2EliteName} Elites appear starting on Stage {Tier2EliteMinimumStageRainstorm + 1}";
                monsoonDesc += $"\n>{Tier2EliteName} Elites appear starting on <style=cIsHealth>Stage {Tier2EliteMinimumStageMonsoon + 1}</style>";
            }

            ChangeEliteTierStats();
        }

        private void ChangeEliteTierStats()
        {
            RoR2Content.Elites.Fire.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.Fire.healthBoostCoefficient = baseEliteHealthBoostCoefficient;
            RoR2Content.Elites.FireHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.FireHonor.healthBoostCoefficient = baseEliteHealthBoostCoefficient / 2;

            RoR2Content.Elites.Ice.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.Ice.healthBoostCoefficient = baseEliteHealthBoostCoefficient;
            RoR2Content.Elites.IceHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.IceHonor.healthBoostCoefficient = baseEliteHealthBoostCoefficient / 2;

            RoR2Content.Elites.Lightning.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.Lightning.healthBoostCoefficient = baseEliteHealthBoostCoefficient;
            RoR2Content.Elites.LightningHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.LightningHonor.healthBoostCoefficient = baseEliteHealthBoostCoefficient / 2;

            DLC1Content.Elites.Earth.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            DLC1Content.Elites.Earth.healthBoostCoefficient = softEliteHealthBoostCoefficient;
            DLC1Content.Elites.EarthHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            DLC1Content.Elites.EarthHonor.healthBoostCoefficient = softEliteHealthBoostCoefficient / 2;

            DLC2Content.Elites.Aurelionite.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            DLC2Content.Elites.Aurelionite.healthBoostCoefficient = hardEliteHealthBoostCoefficient;
            DLC2Content.Elites.AurelioniteHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            DLC2Content.Elites.AurelioniteHonor.healthBoostCoefficient = hardEliteHealthBoostCoefficient / 2;

            foreach (CombatDirector.EliteTierDef etd in EliteAPI.VanillaEliteTiers)//CombatDirector.eliteTiers)
            {
                //Debug.Log(etd.eliteTypes[0].name);
                if (etd.eliteTypes[0] == RoR2Content.Elites.Poison || etd.eliteTypes[0] == RoR2Content.Elites.Haunted)
                {
                    //Debug.LogError("gwagwag");
                    foreach (EliteDef elite in etd.eliteTypes)
                    {
                        elite.healthBoostCoefficient = Mathf.Pow(baseEliteHealthBoostCoefficient, 2); //18
                        elite.damageBoostCoefficient = 4.5f; //6
                    }

                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                    (Run.instance.stageClearCount >= Tier2EliteMinimumStageDrizzle && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty <= DifficultyIndex.Easy)
                    || (Run.instance.stageClearCount >= Tier2EliteMinimumStageRainstorm && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Normal)
                    || (Run.instance.stageClearCount >= Tier2EliteMinimumStageMonsoon && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Hard)
                    || (Run.instance.stageClearCount >= Tier2EliteMinimumStageEclipse && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty > DifficultyIndex.Hard);
                }
            }
        }

        #region blazing
        void BlazingEliteChanges()
        {
            On.RoR2.CharacterBody.UpdateFireTrail += BlazingFireTrailChanges;
        }

        public static float fireTrailDPS = 80f; //1.5f
        public static float fireTrailBaseRadius = 6f; //3f
        public static float fireTrailLifetime = 100f; //3f
        private void BlazingFireTrailChanges(On.RoR2.CharacterBody.orig_UpdateFireTrail orig, CharacterBody self)
        {
            orig(self);
            return;

            if (self.fireTrail)
            {
                self.fireTrail.radius = fireTrailBaseRadius * self.radius;
                self.fireTrail.damagePerSecond = (1 + 0.2f * self.level) * fireTrailDPS;
                //self.fireTrail.pointLifetime = fireTrailLifetime;
            }
        }
        #endregion

        #region overloading
        private void OverloadingEliteChanges()
        {
            //Debug.Log("Modifying Overloading Elite bombs!");
            GameObject overloadingBomb = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LightningStake");

            ProjectileStickOnImpact bombStick = overloadingBomb.GetComponent<ProjectileStickOnImpact>();
            bombStick.ignoreCharacters = true;
            bombStick.ignoreWorld = false;

            ProjectileImpactExplosion bombPie = overloadingBomb.GetComponent<ProjectileImpactExplosion>();
            bombPie.blastRadius = 9;
            bombPie.lifetime = 1.35f;

            IL.RoR2.CharacterBody.RecalculateStats += OverloadingShieldConversion;
            On.RoR2.HealthComponent.TakeDamageProcess += OverloadingKnockbackFix;
            IL.RoR2.GlobalEventManager.OnHitAllProcess += OverloadingBombDamage;
            On.RoR2.GlobalEventManager.OnCharacterDeath += OverloadingSmiteDeath;
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

        public static float overloadingShieldConversionFraction = 0.33f;
        public static float overloadingSmiteCountBase = 2;
        public static float overloadingSmiteCountPerRadius = 1f;
        public static float overloadingSmiteRangeBase = 18f;
        public static float overloadingSmiteRangePerRadius = 9f;
        public static float overloadingSmiteStartingDamage = 10f;
        public static float overloadingSmiteDamagePerStrike = 5f;
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

                    TeamMask teamMask = TeamMask.GetEnemyTeams(attackerBody.teamComponent.teamIndex);
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
        #endregion

        #region voidtouched
        public float voidtouchedNullifyBaseDuration = 12;
        void VoidtouchedEliteChanges()
        {
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += RemoveVoidtouchedCollapse;
            On.RoR2.GlobalEventManager.ProcessHitEnemy += AddVoidtouchedNullify;
            On.RoR2.GlobalEventManager.OnCharacterDeath += VoidtouchedSingularity;
        }

        private void VoidtouchedSingularity(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            CharacterBody victimBody = damageReport.victimBody;
            if (victimBody != null)
            {
                if (victimBody.HasBuff(DLC1Content.Buffs.EliteVoid))
                {
                    ProcChainMask procChainMask6 = damageReport.damageInfo.procChainMask;
                    procChainMask6.AddProc(ProcType.Rings);
                    float damageCoefficient10 = 0;
                    GameObject projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/ElementalRingVoidBlackHole");
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        damage = damageCoefficient10,
                        crit = false,
                        damageColorIndex = DamageColorIndex.Void,
                        position = victimBody.previousPosition,
                        procChainMask = procChainMask6,
                        force = 6000f,
                        owner = victimBody.gameObject,
                        projectilePrefab = CoreModules.Assets.voidtouchedSingularity,
                        rotation = Quaternion.identity,
                        target = null,
                    });
                }
            }
            orig(self, damageReport);
        }

        private void AddVoidtouchedNullify(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if(damageInfo.attacker != null && victim != null && damageInfo.procCoefficient > 0)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();
                if (attackerBody && victimBody)
                {
                    if (attackerBody.HasBuff(DLC1Content.Buffs.EliteVoid))
                    {
                        victimBody.AddTimedBuffAuthority(RoR2Content.Buffs.NullifyStack.buffIndex, voidtouchedNullifyBaseDuration * damageInfo.procCoefficient);
                    }
                }
            }
            orig(self, damageInfo, victim);
        }

        private void RemoveVoidtouchedCollapse(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Buffs", "EliteVoid")
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchStloc(out _)
                );
            c.EmitDelegate<Func<int, int>>((guh) =>
            {
                return 0;
            });
        }
        #endregion

        #region mending
        void MendingEliteChanges()
        {
            IL.RoR2.HealNearbyController.Tick += ReplaceHealingWithBarrier;
            //On.RoR2.HealNearbyController.Tick += BarrierTick;
        }
        private void BarrierTick(On.RoR2.HealNearbyController.orig_Tick orig, HealNearbyController self)
        {
            if (!self.networkedBodyAttachment || !self.networkedBodyAttachment.attachedBody 
                || !self.networkedBodyAttachment.attachedBodyObject || !self.networkedBodyAttachment.attachedBody.healthComponent.alive)
            {
                return;
            }
            List<HurtBox> possibleTargets = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
            self.SearchForTargets(possibleTargets);
            float amount = self.damagePerSecondCoefficient * self.networkedBodyAttachment.attachedBody.damage / self.tickRate;
            List<Transform> chosenTargets = CollectionPool<Transform, List<Transform>>.RentCollection();
            int i = 0;
            while (i < possibleTargets.Count)
            {
                HurtBox hurtBox = possibleTargets[i];
                if (!hurtBox || !hurtBox.healthComponent
                    || hurtBox.healthComponent.body.HasBuff(DLC1Content.Buffs.EliteEarth))
                {
                    goto IL_14A;
                }
                HealthComponent healthComponent = hurtBox.healthComponent;
                if (!(hurtBox.healthComponent.body == self.networkedBodyAttachment.attachedBody))
                {
                    CharacterBody body = healthComponent.body;
                    Transform item = ((body != null) ? body.coreTransform : null) ?? hurtBox.transform;
                    chosenTargets.Add(item);
                    if (NetworkServer.active)
                    {
                        //healthComponent.Heal(amount, default(ProcChainMask), true);
                        healthComponent.AddBarrier(amount);
                        goto IL_14A;
                    }
                    goto IL_14A;
                }
            IL_158:
                i++;
                continue;
            IL_14A:
                if (chosenTargets.Count < self.maxTargets)
                {
                    goto IL_158;
                }
                break;
            }
            self.isTetheredToAtLeastOneObject = ((float)chosenTargets.Count > 0f);
            if (self.tetherVfxOrigin)
            {
                self.tetherVfxOrigin.SetTetheredTransforms(chosenTargets);
            }
            if (self.activeVfx)
            {
                self.activeVfx.SetActive(self.isTetheredToAtLeastOneObject);
            }
            CollectionPool<Transform, List<Transform>>.ReturnCollection(chosenTargets);
            CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(possibleTargets);
        }
        
        private void ReplaceHealingWithBarrier(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<HurtBox>(nameof(HurtBox.healthComponent)),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_fullHealth")
                );
            c.Index--;
            c.Remove();
            c.EmitDelegate<Func<HealthComponent, float>>((healthComponent) =>
            {
                return healthComponent.fullHealth * 2f;
            });
        
            c.GotoNext(MoveType.Before,
                    x => x.MatchCallOrCallvirt<RoR2.HealthComponent>(nameof(HealthComponent.Heal))
                );
            c.Remove();
            c.Remove();
            //c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<HealthComponent, float, RoR2.ProcChainMask, bool/*, HealNearbyController*/>>((targetHealthComponent, healAmount, procChainMask, nonRegen/*, self*/) =>
            {
                //CharacterBody body = self.networkedBodyAttachment.attachedBody;
                //if (body.HasBuff(DLC1Content.Buffs.EliteEarth))
                //{
                    float barrierAmt = 0;
                    barrierAmt = healAmount;
                    targetHealthComponent.AddBarrier(barrierAmt);
                //}
                //else
                //{
                //    targetHealthComponent.Heal(healAmount, procChainMask, isRegen);
                //}
            });
        }

        

        #endregion
    }
}
