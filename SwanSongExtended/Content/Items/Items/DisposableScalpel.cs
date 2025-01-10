using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using BossDropRework;
using static BossDropRework.BossDropReworkPlugin;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    class DisposableScalpel : ItemBase<DisposableScalpel>
    {
        public override string ConfigName => "Items : Scalpel";
        public static int bonusDropChance = 50;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDef;
        public override string ItemName => "Obsidian Scalpel";

        public override string ItemLangTokenName => "BOSSITEMCONSUMABLE";

        public override string ItemPickupDesc => "Powerful enemes have a much greater chance of dropping a trophy. Consumed on drop.";

        public override string ItemFullDescription => $"Every <style=cIsDamage>Champion</style> " +
            $"you kill has an <style=cIsUtility>additional {bonusDropChance}%</style> chance " +
            $"to drop a <style=cIsDamage>trophy</style>. " +
            $"Consumes <style=cIsUtility>1</style> stack when a Trophy drops.";

        public override string ItemLore =>
@"Order: Medical Scalpel (Obsidian)
Tracking Number: 91***********
Estimated Delivery: 09/30/2056
Shipping Method:  Priority/Fragile
Shipping Address: Mt Goliath, Mars
Shipping Details:

Custom made according to your specifications. Very sharp. Blade thickness is measured in planck lengths. This will definitely cut whatever you need it for.
Can’t speak for the durability, though. Try to get it right the first time.
And one more thing – when it breaks, it won’t wait ‘till your operation is finished. Don’t use it on anything you care about not damaging. Or killing.
You already knew all that, though. Can’t help but wonder what you keep ordering these things for.";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlScalpel.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_BOSSITEMCONSUMABLE.png");

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnKillEffect, ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            ShouldTricornFireAndBreak += ScalpelTricornSynergy;
            ModifyBossItemDropChance += ScalpelDropChance;
        }

        private void ScalpelTricornSynergy(CharacterBody attacker, CharacterBody victim, ref bool shouldFire)
        {
            if (GetCount(attacker) > 0)
            {
                shouldFire = false;
                victim.AddBuff(CommonAssets.bossHunterDebuffWithScalpel);
                ConsumeScalpel(attacker);
            }
        }

        private void ScalpelDropChance(CharacterBody victim, CharacterBody attacker, ref float dropChance)
        {
            if (victim.HasBuff(CommonAssets.bossHunterDebuffWithScalpel))
            {
                dropChance = 100;
            }
            else if (dropChance < 100 && GetCount(attacker) > 0)
            {
                if (Util.CheckRoll(bonusDropChance))
                {
                    dropChance = 100;
                    ConsumeScalpel(attacker);
                }
            }
        }
        public static void ConsumeScalpel(CharacterBody attackerBody)
        {
            attackerBody.inventory.RemoveItem(DisposableScalpel.instance.ItemsDef);
            attackerBody.inventory.GiveItem(BrokenScalpel.instance.ItemsDef);
            CharacterMasterNotificationQueue.PushItemTransformNotification(attackerBody.master,
                DisposableScalpel.instance.ItemsDef.itemIndex, BrokenScalpel.instance.ItemsDef.itemIndex,
                CharacterMasterNotificationQueue.TransformationType.Default);
        }
    }
}
