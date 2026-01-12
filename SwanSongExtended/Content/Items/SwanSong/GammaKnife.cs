using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using static MoreStats.OnHit;
using static MoreStats.StatHooks;
using static SwanSongExtended.Modules.Language.Styling;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    class GammaKnife : ItemBase<GammaKnife>
    {
        public static ItemDef statBoostItemDef;
        public static bool hideStatBoost = false;
        public static BuffDef gammaKnifeTemporaryBuff;
        public static int gammaKnifeMaxBuffs = 9;
        public static float attackSpeedBonus = 0.04f;
        public static float cdrBonus = 0.05f;
        public static int armorBonus = 5;
        public static float luckBonusDuration = 9;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Gamma Knife";

        public override string ItemLangTokenName => "GAMMAKNIFE";

        public override string ItemPickupDesc => $"Killing champions permanently increases attack speed and temporarily increases Luck. " +
            $"{VoidColor("Corrupts all Obsidian Scalpels.")}";

        public override string ItemFullDescription => $"Killing a <style=cIsDamage>Champion</style> increases your <style=cIsDamage>Critical Strike chance</style> by " +
            $"<style=cIsDamage>100%</style> for <style=cIsDamage>{luckBonusDuration}</style> seconds " +
            $"AND <style=cIsHealth>permanently</style> increases your <style=cIsUtility>armor</style> " +
            $"by <style=cIsUtility>{armorBonus}</style> and reduces all " +
            $"<style=cIsDamage>ability cooldowns</style> by <style=cIsDamage>{Tools.ConvertDecimal(-cdrBonus)}</style>, " +
            $"up to <style=cIsUtility>{gammaKnifeMaxBuffs}</style> <style=cStack>(+{gammaKnifeMaxBuffs} per stack)</style> times. " +
            $"<style=cIsVoid>Corrupts all Obsidian Scalpels.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.OnKillEffect, ItemTag.Damage };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlGammaKnife.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/gammaknife.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            statBoostItemDef = CreateNewUntieredItem("GAMMAKNIFESTATBOOST", assetBundle.LoadAsset<Sprite>("Assets/Icons/gammaknifeused.png"), isHidden: hideStatBoost);
            string fullDesc = $"<style=cIsHealth>Permanently</style> increases your <style=cIsDamage>attack speed</style> " +
            $"by <style=cIsDamage>{Tools.ConvertDecimal(attackSpeedBonus)}</style> and reduces your " +
            $"<style=cIsDamage>cooldowns</style> by <style=cIsDamage>{Tools.ConvertDecimal(cdrBonus)}</style> per stack.";
            DoLangForItem(statBoostItemDef, "Fake-Soul Butter", "Cut the skin and bend the truth...", fullDesc);

            base.Init();
            gammaKnifeTemporaryBuff = Content.CreateAndAddBuff("bdGammaKnifeBoost",
                LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffMedkitHealIcon"),
                Color.green,
                false, false
                );
        }

        public override void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            GlobalEventManager.onCharacterDeathGlobal += GammaKnifeOnKill;
            GetStatCoefficients += GammaKnifeStatBoosts;
        }

        private void GammaKnifeOnKill(DamageReport damageReport)
        {
            CharacterBody enemyBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (enemyBody == null || attackerBody == null)
                return;
            if (!enemyBody.isChampion)
                return;
            if (enemyBody.healthComponent.alive)
                return;

            Inventory attackerInventory = attackerBody.inventory;
            if (attackerInventory != null)
            {
                int itemCount = GetCount(attackerInventory);
                if (itemCount > 0)
                {
                    float buffDuration = luckBonusDuration;// * itemCount;
                    attackerBody.AddTimedBuffAuthority(gammaKnifeTemporaryBuff.buffIndex, buffDuration);

                    int permanentBuffCount = attackerInventory.GetItemCountEffective(statBoostItemDef);
                    if (permanentBuffCount < gammaKnifeMaxBuffs * itemCount)
                    {
                        attackerInventory.GiveItemPermanent(statBoostItemDef);
                        if (!statBoostItemDef.hidden)
                            CharacterMasterNotificationQueue.PushItemNotification(attackerBody.master, statBoostItemDef.itemIndex);
                    }
                }
            }
        }

        private void GammaKnifeStatBoosts(CharacterBody sender, StatHookEventArgs args)
        {
            Inventory inventory = sender.inventory;
            if (inventory)
            {
                int itemCount = GetCount(sender);
                int permanentBuffCount = inventory.GetItemCountEffective(statBoostItemDef);
                if (itemCount > 0 && permanentBuffCount > 0)
                {
                    args.baseAttackSpeedAdd += attackSpeedBonus * permanentBuffCount;
                    float cdrBoost = Mathf.Pow(1 - cdrBonus, permanentBuffCount);
                    args.allSkills.cooldownMultiplier *= cdrBoost;
                }
            }

            if (sender.HasBuff(gammaKnifeTemporaryBuff))
            {
                args.critAdd += 100;
            }
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = DisposableScalpel.instance.ItemsDef, //consumes ignition tank
                itemDef2 = GammaKnife.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = 
                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
