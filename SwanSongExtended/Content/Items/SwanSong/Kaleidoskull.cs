using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    public class Kaleidoskull : ItemBase<Kaleidoskull>
    {
        public override bool isEnabled => base.isEnabled;
        public static Dictionary<BuffDef, Action<DamageInfo, CharacterBody, CharacterBody>> buffsToProcs;
        public static int critChance = 5;
        public static int debuffCountBase = 1;
        public static int debuffCountStack = 1;
        public override string ItemName => "Kaliedoskull";

        public override string ItemLangTokenName => "KALEIDOSKULL";

        public override string ItemPickupDesc => "Critical Strikes inflict a random debuff. <style=cIsVoid>Corrupts all Predatory Instincts.</style>";

        public override string ItemFullDescription => $"Gain {DamageColor(critChance + "% critical chance")}. {DamageColor("Critical strikes")} randomly {DamageColor("Ignite")} or {UtilityColor("Frost")}. " +
            $"Additionally inflict {UtilityColor(debuffCountBase.ToString())} {StackText("+" + debuffCountStack)} random debuffs. {VoidColor("Corrupts all Predatory Instincts.")}";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.Utility };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            base.Init();
            buffsToProcs = new Dictionary<BuffDef, Action<DamageInfo, CharacterBody, CharacterBody>>();
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.ProcessHitEnemy += KaleidoskullCrit;
            On.RoR2.BuffCatalog.Init += KaleidoskullBuffStuff;
        }

        private void KaleidoskullBuffStuff(On.RoR2.BuffCatalog.orig_Init orig)
        {
            orig();
            buffsToProcs.Add(RoR2Content.Buffs.OnFire, InflictBurn);
            buffsToProcs.Add(DLC1Content.Buffs.StrongerBurn, InflictBurn);
            buffsToProcs.Add(RoR2Content.Buffs.Blight, (damageInfo, attacker, victim) =>
            {
                DotController.InflictDot(victim.gameObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.Blight, 5f * damageInfo.procCoefficient);
            });
            buffsToProcs.Add(RoR2Content.Buffs.Bleeding, (damageInfo, attacker, victim) =>
            {
                DotController.InflictDot(victim.gameObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.Bleed, 3f * damageInfo.procCoefficient);
            });
            buffsToProcs.Add(RoR2Content.Buffs.SuperBleed, (damageInfo, attacker, victim) =>
            {
                DotController.InflictDot(victim.gameObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.SuperBleed, 15f * damageInfo.procCoefficient);
            });
            buffsToProcs.Add(RoR2Content.Buffs.BeetleJuice, (damageInfo, attacker, victim) =>
            {
                victim.AddTimedBuff(RoR2Content.Buffs.BeetleJuice, 3f);
            });
            buffsToProcs.Add(RoR2Content.Buffs.NullifyStack, (damageInfo, attacker, victim) =>
            {
                victim.AddTimedBuff(RoR2Content.Buffs.NullifyStack, 8f);
            });
            buffsToProcs.Add(RoR2Content.Buffs.PulverizeBuildup, (damageInfo, attacker, victim) =>
            {
                victim.AddTimedBuff(RoR2Content.Buffs.PulverizeBuildup, 2f * damageInfo.procCoefficient);
            });
            buffsToProcs.Add(DLC2Content.Buffs.Frost, (damageInfo, attacker, victim) =>
            {
                victim.AddTimedBuff(DLC2Content.Buffs.Frost, 6f, 5);
            });
            buffsToProcs.Add(DLC2Content.Buffs.lunarruin, (damageInfo, attacker, victim) =>
            {
                victim.AddTimedBuff(DLC2Content.Buffs.lunarruin.buffIndex, 5f * damageInfo.procCoefficient);
                DotController.InflictDot(victim.gameObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.LunarRuin, 5f * damageInfo.procCoefficient);
            });
            buffsToProcs.Add(DLC1Content.Buffs.PermanentDebuff, (damageInfo, attacker, victim) =>
            {
                victim.AddBuff(DLC1Content.Buffs.PermanentDebuff);
            });
            buffsToProcs.Add(DLC1Content.Buffs.Fracture, (damageInfo, attacker, victim) =>
            {
                DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                DotController.InflictDot(victim.gameObject, damageInfo.attacker, damageInfo.inflictedHurtbox, DotController.DotIndex.Fracture, dotDef.interval);
            });

            void InflictBurn(DamageInfo damageInfo, CharacterBody attackerBody, CharacterBody victimBody)
            {
                float num4 = 0.5f;
                InflictDotInfo inflictDotInfo = new InflictDotInfo
                {
                    attackerObject = attackerBody.gameObject,
                    victimObject = victimBody.gameObject,
                    totalDamage = new float?(damageInfo.damage * num4),
                    damageMultiplier = 1f,
                    dotIndex = DotController.DotIndex.Burn,
                    maxStacksFromAttacker = null,
                    hitHurtBox = damageInfo.inflictedHurtbox
                };
                if (damageInfo.attacker.GetComponent<OilController>())
                {
                    ProjectileController component6 = damageInfo.attacker.GetComponent<ProjectileController>();
                    CharacterMaster component7 = component6.owner.GetComponent<CharacterMaster>();
                    if (component6 && component7)
                    {
                        StrengthenBurnUtils.CheckDotForUpgrade(component7.inventory, ref inflictDotInfo);
                    }
                }
                else
                {
                    StrengthenBurnUtils.CheckDotForUpgrade(attackerBody.inventory, ref inflictDotInfo);
                }
                DotController.InflictDot(ref inflictDotInfo);
            }
        }

        public override void PostInit()
        {
            base.PostInit();
            AddVoidItemRelationship(itemToCorruptGuid: RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_AttackSpeedOnCrit.AttackSpeedOnCrit_asset);
            //AddVoidItemRelationship(itemToCorruptGuid: RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_HealOnCrit.HealOnCrit_asset);
        }

        private void KaleidoskullCrit(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            if (NetworkServer.active == false)
                return;
            if (damageInfo.procCoefficient <= 0 || damageInfo.crit == false)
                return;

            if(damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody) && victim.TryGetComponent(out CharacterBody victimBody))
            {
                int stack = GetCount(attackerBody);
                if (stack <= 0)
                    return;
                int triggerCt = debuffCountBase + debuffCountStack * (stack - 1);
                List<BuffDef> available = new List<BuffDef>();
                bool isFrosted = false;
                bool isBurned = false;

                foreach(KeyValuePair<BuffDef, Action<DamageInfo, CharacterBody, CharacterBody>> kvp in buffsToProcs)
                {
                    int ct = victimBody.GetBuffCount(kvp.Key);
                    if (ct > 0)
                    {
                        if (kvp.Key.buffIndex == DLC2Content.Buffs.Frost.buffIndex)
                        {
                            isFrosted = true;
                            if (ct >= 5)
                                continue;
                        }
                        available.Add(kvp.Key);
                        if (kvp.Key.buffIndex == DLC1Content.Buffs.StrongerBurn.buffIndex || kvp.Key.buffIndex == RoR2Content.Buffs.OnFire.buffIndex)
                            isBurned = true;
                    }
                }

                if (isFrosted == false && (Util.CheckRoll(50, 0) || isBurned == true))
                {
                    buffsToProcs[DLC2Content.Buffs.Frost].Invoke(damageInfo, attackerBody, victimBody);
                }
                else if (isBurned == false)
                {
                    buffsToProcs[RoR2Content.Buffs.OnFire].Invoke(damageInfo, attackerBody, victimBody);
                }

                if (available.Count == 0)
                {
                    return;
                }
                for(int n = 0; n < triggerCt; n++)
                {
                    BuffDef buff = available[UnityEngine.Random.Range(0, available.Count)];
                    buffsToProcs[buff].Invoke(damageInfo, attackerBody, victimBody);
                }
            }
        }
    }
}
