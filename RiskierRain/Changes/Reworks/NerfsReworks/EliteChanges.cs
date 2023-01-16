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

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        float softEliteHealthBoostCoefficient = 2f; //3
        float baseEliteHealthBoostCoefficient = 3f; //4
        float baseEliteDamageBoostCoefficient = 1.5f; //2
        public static float overloadingBombDamage = 1.5f; //0.5f

        public static int Tier2EliteMinimumStageDefault = 5;
        public static int Tier2EliteMinimumStageDrizzle = 10;
        public static int Tier2EliteMinimumStageRainstorm = 5;
        public static int Tier2EliteMinimumStageMonsoon = 3;
        public static int Tier2EliteMinimumStageEclipse = 0;

        static string Tier2EliteName = "Tier 2";

        void ChangeEliteStats()
        {
            if(Tier2EliteMinimumStageDrizzle != Tier2EliteMinimumStageDefault 
                || Tier2EliteMinimumStageMonsoon != Tier2EliteMinimumStageDefault
                || Tier2EliteMinimumStageEclipse != Tier2EliteMinimumStageDefault)
            {
                drizzleDesc += $"\n>{Tier2EliteName} Elites appear starting on <style=cIsHealing>Stage {Tier2EliteMinimumStageDrizzle + 1}</style>";
                rainstormDesc += $"\n>{Tier2EliteName} Elites appear starting on Stage {Tier2EliteMinimumStageRainstorm + 1}</style></style>";
                monsoonDesc += $"\n>{Tier2EliteName} Elites appear starting on <style=cIsHealth>Stage {Tier2EliteMinimumStageMonsoon + 1}</style>";
            }


            //On.RoR2.CombatDirector.Init += EliteTierChanges;
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

            foreach (CombatDirector.EliteTierDef etd in CombatDirector.eliteTiers)
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

        private void EliteTierChanges(On.RoR2.CombatDirector.orig_Init orig)
        {
            orig();

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

            foreach(CombatDirector.EliteTierDef etd in CombatDirector.eliteTiers)
            {
                //Debug.Log(etd.eliteTypes[0].name);
                if (etd.eliteTypes[0] == RoR2Content.Elites.Poison || etd.eliteTypes[0] == RoR2Content.Elites.Haunted)
                {
                    //Debug.LogError("gwagwag");
                    foreach(EliteDef elite in etd.eliteTypes)
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

        public static float fireTrailDPS = 0.5f; //1.5f
        public static float fireTrailBaseRadius = 6f; //3f
        public static float fireTrailLifetime = 100f; //3f
        private void BlazingFireTrailChanges(On.RoR2.CharacterBody.orig_UpdateFireTrail orig, CharacterBody self)
        {
            orig(self);
            return;

            if (self.fireTrail)
            {
                self.fireTrail.radius = fireTrailBaseRadius * self.radius;
                self.fireTrail.damagePerSecond = self.damage * fireTrailDPS;
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
            bombPie.lifetime = 1.2f;

            On.RoR2.HealthComponent.TakeDamage += OverloadingKnockbackFix;
            IL.RoR2.GlobalEventManager.OnHitAll += OverloadingBombDamage;
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

        private void OverloadingKnockbackFix(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
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
        public float voidtouchedNullifyBaseDuration = 20;
        void VoidtouchedEliteChanges()
        {
            IL.RoR2.GlobalEventManager.OnHitEnemy += RemoveVoidtouchedCollapse;
            On.RoR2.GlobalEventManager.OnHitEnemy += AddVoidtouchedNullify;
        }

        private void AddVoidtouchedNullify(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            CharacterBody attackerBody = damageInfo.attacker?.GetComponent<CharacterBody>();
            CharacterBody victimBody = victim?.GetComponent<CharacterBody>();
            if (attackerBody && victimBody && damageInfo.procCoefficient > 0)
            {
                if (attackerBody.HasBuff(DLC1Content.Buffs.EliteVoid))
                {
                    victimBody.AddTimedBuffAuthority(RoR2Content.Buffs.NullifyStack.buffIndex, voidtouchedNullifyBaseDuration * damageInfo.procCoefficient);
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
    }
}
