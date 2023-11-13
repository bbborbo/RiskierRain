using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using static BorboStatUtils.BorboStatUtils;
using static RiskierRainContent.CoreModules.StatHooks;
using RoR2.ExpansionManagement;

namespace RiskierRainContent.Items
{
    class GammaKnife : ItemBase<GammaKnife>
    {
        public static BuffDef gammaKnifeTemporaryBuff;
        public static int gammaKnifeMaxBuffs = 9;
        public static float attackSpeedBonus = 0.04f;
        public static float cdrBonus = 0.04f;
        public static float luckBonusDuration = 9;
        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Gamma Knife";

        public override string ItemLangTokenName => "GAMMAKNIFE";

        public override string ItemPickupDesc => "Killing champions permanently increases attack speed and temporarily increases Luck. Corrupts all Obsidian Scalpels.";

        public override string ItemFullDescription => $"Killing a <style=cIsDamage>Champion</style> increases your <style=cIsUtility>Luck</style> by " +
            $"<style=cIsUtility>1</style> <style=cStack>(+1 per stack)</style> for <style=cIsUtility>{luckBonusDuration} seconds</style> " +
            $"AND <style=cIsHealth>permanently</style> increases your <style=cIsDamage>attack speed</style> " +
            $"by <style=cIsDamage>{Tools.ConvertDecimal(attackSpeedBonus)}</style> and reduces your " +
            $"<style=cIsDamage>cooldowns</style> by <style=cIsDamage>{Tools.ConvertDecimal(cdrBonus)}</style>, " +
            $"up to <style=cIsUtility>{gammaKnifeMaxBuffs}</style> <style=cStack>(+{gammaKnifeMaxBuffs} per stack)</style> times. " +
            $"<style=cIsVoid>Corrupts all Obsidian Scalpels.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.OnKillEffect, ItemTag.Damage };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            On.RoR2.GlobalEventManager.OnCharacterDeath += GammaKnifeOnKill;
            On.RoR2.CharacterBody.RecalculateStats += GammaKnifeCdr;
            GetStatCoefficients += GammaKnifeStatBoosts;
            ModifyLuckStat += GammaKnifeLuck;
        }

        private void GammaKnifeCdr(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            Inventory inventory = self.inventory;
            if (inventory)
            {
                int itemCount = GetCount(inventory);
                int permanentBuffCount = inventory.GetItemCount(GammaKnifeStatBoost.instance.ItemsDef);
                if (itemCount > 0 && permanentBuffCount > 0)
                {
                    float cdrBoost = Mathf.Pow(1 - cdrBonus, permanentBuffCount);

                    SkillLocator skillLocator = self.skillLocator;
                    if (skillLocator != null)
                    {
                        ApplyCooldownScale(skillLocator.primary, cdrBoost);
                        ApplyCooldownScale(skillLocator.secondary, cdrBoost);
                        ApplyCooldownScale(skillLocator.utility, cdrBoost);
                        ApplyCooldownScale(skillLocator.special, cdrBoost);
                    }
                }
            }
        }

        private void GammaKnifeLuck(CharacterBody sender, ref float luck)
        {
            bool hasBuff = sender.HasBuff(gammaKnifeTemporaryBuff);
            if (hasBuff)
            {
                luck += GetCount(sender);
            }
        }

        private void GammaKnifeStatBoosts(CharacterBody sender, StatHookEventArgs args)
        {
            Inventory inventory = sender.inventory;
            if (inventory)
            {
                int itemCount = GetCount(inventory);
                int permanentBuffCount = inventory.GetItemCount(GammaKnifeStatBoost.instance.ItemsDef);
                if (itemCount > 0 && permanentBuffCount > 0)
                {
                    args.baseAttackSpeedAdd += attackSpeedBonus * permanentBuffCount;
                }
            }
        }

        private void GammaKnifeOnKill(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);

            CharacterBody enemyBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (enemyBody == null || attackerBody == null)
                return;
            if (!enemyBody.isChampion)
                return;
            if (enemyBody.healthComponent.alive)
                return;

            Inventory attackerInventory = attackerBody.inventory;
            if(attackerInventory != null)
            {
                int itemCount = GetCount(attackerInventory);
                if (itemCount > 0)
                {
                    float buffDuration = luckBonusDuration;// * itemCount;
                    attackerBody.AddTimedBuffAuthority(gammaKnifeTemporaryBuff.buffIndex, buffDuration);

                    int permanentBuffCount = attackerInventory.GetItemCount(GammaKnifeStatBoost.instance.ItemsDef);
                    if(permanentBuffCount < gammaKnifeMaxBuffs * itemCount)
                    {
                        attackerInventory.GiveItem(GammaKnifeStatBoost.instance.ItemsDef);
                        if(!GammaKnifeStatBoost.shouldBeInvisible)
                            CharacterMasterNotificationQueue.PushItemNotification(attackerBody.master, GammaKnifeStatBoost.instance.ItemsDef.itemIndex);
                    }
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateBuff();
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void CreateBuff()
        {
            gammaKnifeTemporaryBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                gammaKnifeTemporaryBuff.name = "GammaKnifeStatBuff";
                gammaKnifeTemporaryBuff.buffColor = Color.green;
                gammaKnifeTemporaryBuff.canStack = false;
                gammaKnifeTemporaryBuff.isDebuff = false;
                gammaKnifeTemporaryBuff.iconSprite = Resources.Load<Sprite>("textures/bufficons/texBuffMedkitHealIcon");
            }
            Assets.buffDefs.Add(gammaKnifeTemporaryBuff);
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = DisposableScalpel.instance.ItemsDef, //consumes ignition tank
                itemDef2 = GammaKnife.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
