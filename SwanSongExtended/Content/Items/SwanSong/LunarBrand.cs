using BepInEx.Configuration;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MoreStats.StatHooks;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Tools;
using SwanSongExtended.Modules;
using UnityEngine.AddressableAssets;
using static RoR2.DotController;
using static R2API.DotAPI;
using UnityEngine.Networking;
using static RainrotSharedUtils.Styling;

namespace SwanSongExtended.Items
{
    class LunarBrand : ItemBase<LunarBrand>
    {
        public static int brandBurnChance = 20;
        public override bool isEnabled => false; 
        public static BuffDef CauterizeBuff;
        public static int duration = 5;
        public static int durationStack = 5;
        public static int cauterizeArmor = 30;
        public static float cauterizeSlow = 0.5f;

        public static float cauterizeDamageInterval = 1f;
        public static float cauterizeDamageCoef = 9f;
        public static float cauterizeDamageStack = 4.5f;
        public static float cauterizeProcCoef = 2.0f;
        public static int burnThreshold = 5;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Starfire Brand";

        public override string ItemLangTokenName => "LUNARBRAND";

        public override string ItemPickupDesc => "Cauterize burning enemies, inflicting heavy damage " +
            "<style=cIsHealth>AND erasing all of their debuffs.</style>";

        public override string ItemFullDescription => $"Gain <style=cIsDamage>{brandBurnChance}%</style> chance to <style=cIsDamage>Ignite</style> on hit. " + 
            $"Inflicting <style=cIsDamage>{burnThreshold}</style> stacks of burn Cauterizes enemies " +
            $"for <style=cIsDamage>{duration}</style> seconds <style=cStack>(+{durationStack} per stack)</style>, " +
            $"dealing <style=cIsDamage>{Tools.ConvertDecimal(cauterizeDamageCoef)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(cauterizeDamageStack)} per stack)</style> damage. " +
            $"Additionally, Cauterize slows enemies by {cauterizeSlow.AsPercent()} and negates all healing, " +
            $"<style=cIsHealth>BUT... Cauterized enemies are cleansed of all debuffs and have +{cauterizeArmor} armor.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag [] { ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.Damage };
        public override GameObject ItemModel => LoadDropPrefab("mdlLunarBrand");

        public override Sprite ItemIcon => LoadItemIcon("texIconLunarBrand");


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            CauterizeBuff = Content.CreateAndAddBuff(
                "bdCauterize",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(), //replace me
                new Color(0.2f, 0f, 0.1f),
                true, false
                );
            CauterizeBuff.isDOT = true;
            base.Init();
        }

        public override void Hooks()
        {
            GetStatCoefficients += AddCauterizeStats;
            GetMoreStatCoefficients += AddBurnChance;
            On.RoR2.GlobalEventManager.ProcessHitEnemy += BrandOnHit;
            On.RoR2.HealthComponent.TakeDamageProcess += CauterizeBuffBehavior;
        }

        private void CauterizeBuffBehavior(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            int buffCount = self && self.body ? self.body.GetBuffCount(CauterizeBuff.buffIndex) : 0;
            if (buffCount <= 0)
            {
                orig(self, damageInfo);
                return;
            }

            //if (Util.CheckRoll(cauterizeBlockChance * Math.Min(buffCount, 6), 0f, null))
            //{
            //    EffectData effectData = new EffectData
            //    {
            //        origin = damageInfo.position,
            //        rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
            //    };
            //    EffectManager.SpawnEffect(HealthComponent./*private*/AssetReferences.bearEffectPrefab, effectData, true);
            //    //Util.PlaySound(StealthMode.enterStealthSound, self.gameObject);
            //    damageInfo.rejected = true;
            //}
            orig(self, damageInfo);
        }

        private void AddCauterizeStats(CharacterBody sender, StatHookEventArgs args)
        {
            int cauterizeCount = sender.GetBuffCount(CauterizeBuff);
            if (cauterizeCount > 0)
            {
                args.armorAdd += cauterizeArmor * cauterizeCount;
                args.moveSpeedReductionMultAdd += cauterizeSlow;
            }
        }

        private void AddBurnChance(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (GetCount(sender) > 0)
            {
                args.burnChanceOnHit += SwanSongPlugin.brandBurnChance;
            }
            int cauterizeCount = sender.GetBuffCount(CauterizeBuff);
            if (cauterizeCount > 0)
            {
                args.healingPercentIncreaseMult = 0;
            }
        }



        private void BrandOnHit(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            CharacterBody victimBody = victim ? victim.GetComponent<CharacterBody>() : null;
            CharacterBody attackerBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
            int itemCount = GetCount(attackerBody);
            if (itemCount > 0 && NetworkServer.active)
            {
                int burnCount = victimBody.GetBuffCount(RoR2Content.Buffs.OnFire);
                int strongBurnCount = victimBody.GetBuffCount(DLC1Content.Buffs.StrongerBurn);
                int cauterizeCount = victimBody.GetBuffCount(CauterizeBuff);
                int threshold = burnThreshold;
                int caut = 0;
                //if 1
                while (burnCount + strongBurnCount >= threshold * (cauterizeCount + 1))//cauterize when burn > 5, remove 5 burn too
                {
                    caut++;
                    threshold += burnThreshold;
                }
                if(caut > 0)
                {
                    Cauterize(attackerBody, damageInfo, victimBody, caut);//do the thing
                }
            }
        }

        private void Cauterize(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victim, int cauterizeCt)
        {
            CleanseSystem.CleanseBodyServer(victim, true, false, false, true, false, false);
            //DotController bleedDot = DotController.FindDotController(victim.gameObject);
            //Tools.ClearDotStacksForType(bleedDot, DotController.DotIndex.Bleed);
            DamageInfo cauterizeHit = new DamageInfo()
            {
                attacker = attackerBody.gameObject,
                crit = damageInfo.crit,
                damage = (cauterizeDamageCoef + cauterizeDamageStack) * attackerBody.damage * cauterizeCt,
                damageType = DamageType.BypassArmor | DamageType.BypassBlock,
                damageColorIndex = DamageColorIndex.Item,
                force = Vector3.zero,
                position = damageInfo.position,
                procChainMask = damageInfo.procChainMask,
                procCoefficient = cauterizeProcCoef
            };
            victim.healthComponent.TakeDamage(cauterizeHit); //deal damage
            GlobalEventManager.instance.OnHitEnemy(cauterizeHit, victim.gameObject);
            GlobalEventManager.instance.OnHitAll(cauterizeHit, victim.gameObject);
            while (cauterizeCt > 0)
            {
                cauterizeCt--;
                victim.AddTimedBuffAuthority(CauterizeBuff.buffIndex, duration + durationStack); //apply buff
            }

            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova"), new EffectData
            {
                origin = victim.corePosition,
                scale = victim.bestFitRadius + 4
            }, true);
        }
    }
}
