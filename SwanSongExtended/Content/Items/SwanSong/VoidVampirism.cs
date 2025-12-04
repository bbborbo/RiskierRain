using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Items
{
    class VoidVampirism : ItemBase<VoidVampirism>
    {
        int vampireBleedChance = 10;

        int maxHealing = 2;
        int maxHealingStack = 1;
        float healthPerBleed = 0.2f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override string ItemName => "Bloodsucking Coralite";

        public override string ItemLangTokenName => "HEALFROMBLEEDINGENEMIES";

        public override string ItemPickupDesc => "Bleeding enemies heal you on hit. Corrupts all Leeching Seeds.";

        public override string ItemFullDescription => $"Gain <style=cIsHealth>{vampireBleedChance}% bleed chance</style>. " +
            $"Bleeding enemies heal you for <style=cIsDamage>+{healthPerBleed} health</style> when hit " +
            $"<style=cIsHealth>per stack of bleed</style>, up to a maximum of " +
            $"{maxHealing} <style=cStack>(+{maxHealingStack} per stack)</style> health. " +
            $"<style=cIsVoid>Corrupts all Leeching Seeds.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.Damage };
        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/coralite.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/voidvampirism.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += VampireBleedChance;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.HealthComponent.TakeDamageProcess += TakeMoreDamageWhileBurning;
            On.RoR2.GlobalEventManager.OnCharacterDeath += VampireOnKill;
        }

        private void VampireOnKill(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);
            CharacterBody enemyBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (enemyBody == null || attackerBody == null)
                return;
            Inventory attackerInventory = attackerBody.inventory;
            if (attackerInventory != null)
            {
                int itemCount = GetCount(attackerInventory);
                if (itemCount > 0)
                {
                    int currentBuffCount = enemyBody.GetBuffCount(RoR2Content.Buffs.Bleeding);
                    int maxHealingCount = (maxHealing + maxHealingStack * itemCount);

                    float healingToDo = MathF.Min((healthPerBleed * currentBuffCount), maxHealingCount);

                    attackerBody.AddTimedBuffAuthority(JunkContent.Buffs.MeatRegenBoost.buffIndex, healingToDo * 3);
                }
            }
        }

        private void TakeMoreDamageWhileBurning(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if (damageInfo.attacker != null && self && self.body)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                int count = GetCount(attackerBody);
                if (attackerBody != null && count > 0)
                {
                    CharacterBody victimBody = self.body;

                    int currentBuffCount = victimBody.GetBuffCount(RoR2Content.Buffs.Bleeding);
                    int maxHealingCount = (maxHealing + maxHealingStack * count);

                    float healingToDo = MathF.Min((healthPerBleed * currentBuffCount), maxHealingCount);

                    attackerBody.AddTimedBuffAuthority(JunkContent.Buffs.MeatRegenBoost.buffIndex, healingToDo);
                }
            }

            orig(self, damageInfo);
        }

        private void VampireBleedChance(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (GetCount(self) > 0)
            {
                self.bleedChance += vampireBleedChance;
            }
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Seed, //consumes leeching seed
                itemDef2 = VoidVampirism.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
