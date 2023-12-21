using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Items;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRainContent.BurnStatHook;
using static RiskierRainContent.Tools;

namespace RiskierRainContent.Items
{
    class LunarBrand : ItemBase<LunarBrand>
    {
        public static BuffDef CauterizeBuff;
        public static int duration = 5;
        public static int durationStack = 5;
        public static int cauterizeArmor = 20;

        public static float cauterizeDamageCoef = 4f;
        public static float cauterizeDamageStack = 2f;
        public static float cauterizeProcCoef = 0f;
        public static int burnThreshold = 5;

        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Starfire Brand";

        public override string ItemLangTokenName => "LUNARBRAND";

        public override string ItemPickupDesc => "Cauterize burning enemies, inflicting heavy damage " +
            "<style=cIsHealth>AND increasing their armor, rendering them invulnerable to Bleed.</style>";

        public override string ItemFullDescription => $"Gain <style=cIsDamage>{RiskierRainContent.brandBurnChance}% ignite chance</style>. " + 
            $"Inflicting <style=cIsDamage>{burnThreshold}</style> stacks of burn Cauterizes enemies " +
            $"for <style=cIsDamage>{duration}</style> seconds <style=cStack>(+{durationStack} per stack)</style>, " +
            $"dealing <style=cIsDamage>{Tools.ConvertDecimal(cauterizeDamageCoef)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(cauterizeDamageStack)} per stack)</style> damage through armor. " +
            $"<style=cIsHealth>Cauterized enemies are invulnerable to Bleed and have +{cauterizeArmor} armor.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag [] { ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.Damage };
        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlLunarBrand.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_LUNARBRAND.png");


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            BurnStatCoefficient += AddBurnChance;
            On.RoR2.GlobalEventManager.OnHitEnemy += BrandOnHit;
            On.RoR2.CharacterBody.RecalculateStats += CauterizeBuffBehavior;
        }

        private void CauterizeBuffBehavior(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            self.armor += cauterizeArmor * self.GetBuffCount(CauterizeBuff);
        }

        private void BrandOnHit(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            CharacterBody victimBody = victim ? victim.GetComponent<CharacterBody>() : null;
            CharacterBody attackerBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
            int itemCount = GetCount(attackerBody);
            if (itemCount > 0)
            {
                int burnCount = victimBody.GetBuffCount(RoR2Content.Buffs.OnFire);
                int strongBurnCount = victimBody.GetBuffCount(DLC1Content.Buffs.StrongerBurn);
                int cauterizeCount = victimBody.GetBuffCount(CauterizeBuff);
                int threshold = burnThreshold;
                //if 1
                while (burnCount + strongBurnCount >= threshold * (cauterizeCount + 1))//cauterize when burn > 5, remove 5 burn too
                {
                    Debug.Log("cauterize!!");
                    int i = 0;
                    while(i < threshold)
                    {
                        if (burnCount > 0)
                        {
                            victimBody.healthComponent.body.RemoveOldestTimedBuff(RoR2Content.Buffs.OnFire);
                            burnCount--;
                            i++;
                            Debug.Log("burn = " + burnCount);
                        }
                        else if (strongBurnCount > 0)
                        {
                            victimBody.healthComponent.body.RemoveOldestTimedBuff(DLC1Content.Buffs.StrongerBurn);
                            strongBurnCount--;
                            i++;
                            Debug.Log("superburn" + strongBurnCount);
                        }
                        else
                            break;
                    }

                    Cauterize(attackerBody, damageInfo, victimBody);//do the thing
                    threshold += burnThreshold;
                    Debug.Log("threshold = " + threshold);
                }
            }
        }

        private void Cauterize(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victim)
        {
            DamageInfo cauterizeHit = new DamageInfo()
            {
                attacker = attackerBody.gameObject,
                crit = damageInfo.crit,
                damage = (cauterizeDamageCoef + cauterizeDamageStack) * attackerBody.damage,
                damageType = DamageType.BypassArmor | DamageType.BypassBlock,
                damageColorIndex = DamageColorIndex.Item,
                force = Vector3.zero,
                position = victim.transform.position,
                procChainMask = damageInfo.procChainMask,
                procCoefficient = cauterizeProcCoef
            };
            victim.healthComponent.TakeDamage(cauterizeHit); //deal damage
            DotController bleedDot = DotController.FindDotController(victim.gameObject);
            Tools.ClearDotStacksForType(bleedDot, DotController.DotIndex.Bleed);
            victim.AddTimedBuffAuthority(CauterizeBuff.buffIndex, duration + durationStack); //apply buff
        }

        private void AddBurnChance(CharacterBody sender, BurnEventArgs args)
        {
            if (GetCount(sender) > 0)
            {
                args.burnChance += RiskierRainContent.brandBurnChance;
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        private void CreateBuff()
        {
            CauterizeBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                CauterizeBuff.name = "cauterize";
                CauterizeBuff.buffColor = new Color(0f, 0f, 0f);
                CauterizeBuff.canStack = true;
                CauterizeBuff.isDebuff = false;
                CauterizeBuff.iconSprite = Assets.mainAssetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png");
            };
            Assets.buffDefs.Add(CauterizeBuff);
        }

        
    }
}
