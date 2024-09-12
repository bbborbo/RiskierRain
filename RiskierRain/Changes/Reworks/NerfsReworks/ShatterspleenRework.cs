using BepInEx;
using RiskierRain.CoreModules;
using EntityStates.ImpBossMonster;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static int minSpikes = 3;
        public static int maxSpikes = 4;
        public static int baseSpikesPerBuff = 2;
        public static int stackSpikesPerBuff = 1;

        public static float minDamageCoefficient = 5f;
        public static float releaseSpeed = 2f;

        public static float spikeDamageCoefficient = 0.3f;
        public static float spikeProcCoefficient = 1.0f;
        public static int shatterspleenBleedChance = 20;
        public static GameObject impBleedSpikePrefab;
        void ReworkShatterspleen()
        {
            impBleedSpikePrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/ImpVoidspikeProjectile").InstantiateClone("ShatterspleenBleedSpike", true);

            ProjectileController pc = impBleedSpikePrefab.GetComponent<ProjectileController>();
            pc.procCoefficient = 1.0f;

            ProjectileImpactExplosion pie = impBleedSpikePrefab.GetComponent<ProjectileImpactExplosion>();
            pie.blastRadius = 4;

            Rigidbody rb = impBleedSpikePrefab.GetComponent<Rigidbody>();
            rb.useGravity = true;

            AntiGravityForce agf = impBleedSpikePrefab.AddComponent<AntiGravityForce>();
            agf.antiGravityCoefficient = -1f;
            agf.rb = rb;

            ProjectileDirectionalTargetFinder pdtt = impBleedSpikePrefab.AddComponent<ProjectileDirectionalTargetFinder>();
            pdtt.enabled = true;
            pdtt.lookRange = 50;
            pdtt.lookCone = 10;

            ProjectileSteerTowardTarget pstt = impBleedSpikePrefab.AddComponent<ProjectileSteerTowardTarget>();
            pstt.yAxisOnly = false;
            pstt.rotationSpeed = 50;

            CoreModules.Assets.projectilePrefabs.Add(impBleedSpikePrefab);

            IL.RoR2.GlobalEventManager.OnHitEnemy += RevokeShatterspleenBleedRights;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += RevokeShatterspleenDeathRights;
            GetStatCoefficients += RemoveShatterspleenCrit;
            On.RoR2.CharacterBody.RecalculateStats += ShatterspleenBleedChance;
            On.RoR2.GlobalEventManager.OnHitEnemy += NewShatterspleenFunctionality;
            On.RoR2.CharacterBody.RemoveBuff_BuffIndex += FireShatterspleenBleedSpike;

            LanguageAPI.Add("ITEM_BLEEDONHITANDEXPLODE_PICKUP", "Massive hits charge a volley of bleed spikes.");
            LanguageAPI.Add("ITEM_BLEEDONHITANDEXPLODE_DESC", 
                $"Gain {shatterspleenBleedChance}% bleed chance. " +
                $"Hits that deal <style=cIsDamage>more than {Tools.ConvertDecimal(minDamageCoefficient)} damage</style> also " +
                $"charge {minSpikes}-{maxSpikes} volleys of " +
                $"{baseSpikesPerBuff} void spikes (+{stackSpikesPerBuff} per stack). " +
                $"These spikes release {releaseSpeed} seconds at a time, " +
                $"dealing {Tools.ConvertDecimal(spikeDamageCoefficient)} BASE damage and bleeding enemies hit " +
                $"for {Tools.ConvertDecimal(2.4f * spikeProcCoefficient)} base damage.");
        }

        private void ShatterspleenBleedChance(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.inventory?.GetItemCount(RoR2Content.Items.BleedOnHitAndExplode) > 0)
            {
                self.bleedChance += shatterspleenBleedChance;
            }
        }

        private void RemoveShatterspleenCrit(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.inventory)
            {
                Inventory inv = sender.inventory;
                if (inv.GetItemCount(RoR2Content.Items.BleedOnHitAndExplode) > 0)
                {
                    args.critAdd -= 5;
                }
            }
        }

        private void FireShatterspleenBleedSpike(On.RoR2.CharacterBody.orig_RemoveBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
        {
            if (buffType == CoreModules.Assets.shatterspleenSpikeBuff.buffIndex)
            {
                int itemCount = self.inventory.GetItemCount(RoR2Content.Items.BleedOnHitAndExplode);
                if (itemCount > 0)
                {
                    Util.PlaySound(FireVoidspikes.attackSoundString, self.gameObject);
                    EffectManager.SimpleMuzzleFlash(FireVoidspikes.swipeEffectPrefab, self.gameObject, "Head", false);
                    Ray aimRay = new Ray(self.inputBank.aimOrigin, self.inputBank.aimDirection);

                    int spikeCount = baseSpikesPerBuff + stackSpikesPerBuff * (itemCount - 1);
                    Debug.Log(spikeCount);
                    for (int i = 0; i < spikeCount; i++)
                    {
                        float bonusYaw = 0;
                        float projectileSpeed = FireVoidspikes.projectileSpeed * 0.3f * (i + 1);

                        Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 0f, 1f, 1f, bonusYaw, 0);
                        ProjectileManager.instance.FireProjectile(impBleedSpikePrefab, aimRay.origin,
                            Util.QuaternionSafeLookRotation(forward), self.gameObject,
                            self.damage * spikeDamageCoefficient, 0f,
                            Util.CheckRoll(self.crit, self.master),
                            DamageColorIndex.Default, null, projectileSpeed);
                    }
                }
            }
            orig(self, buffType);
        }

        private void NewShatterspleenFunctionality(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.attacker && damageInfo.procCoefficient > 0f && !damageInfo.procChainMask.HasProc(ProcType.BleedOnHit))
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    int spleenCount = 0;
                    Inventory inventory = attackerBody.inventory;
                    if (inventory)
                    {
                        spleenCount = inventory.GetItemCount(RoR2Content.Items.BleedOnHitAndExplode);

                        if (victimBody != null && spleenCount > 0)
                        {
                            int targetBuffCount = UnityEngine.Random.Range(minSpikes, maxSpikes + 1);
                            int buffCount = attackerBody.GetBuffCount(CoreModules.Assets.shatterspleenSpikeBuff);
                            if (damageInfo.damage / attackerBody.damage >= minDamageCoefficient && targetBuffCount > buffCount)
                            {
                                damageInfo.procChainMask.AddProc(ProcType.BleedOnHit);
                                for (int i = buffCount; i < targetBuffCount; i++)
                                {
                                    attackerBody.AddTimedBuffAuthority(CoreModules.Assets.shatterspleenSpikeBuff.buffIndex, releaseSpeed * (i + 1));
                                }
                            }
                        }
                    }
                }
            }
            orig(self, damageInfo, victim);
        }

        private void RevokeShatterspleenBleedRights(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "BleedOnHitAndExplode"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Mul);
        }

        private void RevokeShatterspleenDeathRights(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "BleedOnHitAndExplode"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Mul);
        }
    }
}
