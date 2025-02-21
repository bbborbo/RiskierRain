using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Items
{
    class VoidElixirConsumed : ItemBase<VoidElixirConsumed>
    {
        public override ExpansionDef RequiredExpansion => SotvExpansionDef();
        public override bool lockEnabled => true;
        static float armorBoost => VoidElixir.armorBuff;
        static float regenBoost => VoidElixir.regenBuff;
        public override string ItemName => "A";

        public override string ItemLangTokenName => "INFERNOPOTIONEMPTY";

        public override string ItemPickupDesc => "An empty flask. You feel iron-skinned.";

        public override string ItemFullDescription => "A";

        public override string ItemLore => "A";

        public override ItemTier Tier => ItemTier.NoTier;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnStageBeginEffect };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {

            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterMaster.OnServerStageBegin += TryRegenerateElixir;
            GetStatCoefficients += BerserkerBrewBuff;
        }

        private void BerserkerBrewBuff(CharacterBody sender, StatHookEventArgs args)
        {
            int count = GetCount(sender);
            if(count > 0)
            {
                args.armorAdd += armorBoost * count;
                args.baseRegenAdd += (regenBoost * count) * (1 + 0.2f * sender.level);
            }
        }

        private void TryRegenerateElixir(On.RoR2.CharacterMaster.orig_OnServerStageBegin orig, CharacterMaster self, Stage stage)
        {
            orig(self, stage);
            if (NetworkServer.active)
            {
                int count = GetCount(self);
                if (count > 0)
                {
                    TransformPotions(count, self);
                }
            }
        }
        private void TransformPotions(int count, CharacterMaster master)
        {
            Inventory inv = master.inventory;
            inv.RemoveItem(instance.ItemsDef, count);
            inv.GiveItem(VoidElixir.instance.ItemsDef, count);

            CharacterMasterNotificationQueue.SendTransformNotification(
                master, instance.ItemsDef.itemIndex,
                VoidElixir.instance.ItemsDef.itemIndex,
                CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);
        }
    }
}
