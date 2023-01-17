using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Items
{
    class Elixir2Consumed : ItemBase<Elixir2Consumed>
    {
        public override string ItemName => "Empty Flask";

        public override string ItemLangTokenName => "LEGALLYDISTINCTBOTTLE";

        public override string ItemPickupDesc => "An empty flask. Does nothing.";

        public override string ItemFullDescription => "An empty flask. Does nothing.";

        public override string ItemLore => "Nothing to see here.";

        public override ItemTier Tier => ItemTier.NoTier;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnStageBeginEffect };

        public override BalanceCategory Category => BalanceCategory.None;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterMaster.OnServerStageBegin += TryRegenerateElixir;
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

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
        private void TransformPotions(int count, CharacterMaster master)
        {
            Inventory inv = master.inventory;
            inv.RemoveItem(instance.ItemsDef, count);
            inv.GiveItem(Elixir2.instance.ItemsDef, count);

            CharacterMasterNotificationQueue.SendTransformNotification(
                master, instance.ItemsDef.itemIndex,
                Elixir2.instance.ItemsDef.itemIndex,
                CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);
        }
    }
}
