using BepInEx.Configuration;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.CharacterMasterNotificationQueue;

namespace SwanSongExtended.Items
{
    class GoldenEgg : ItemBase<GoldenEgg>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return Egg.GetEggConfig();
        }

        public static bool goldenEggDropsRecyclable = true;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Golden Egg";

        public override string ItemLangTokenName => "GOLDEN_EGG";

        public override string ItemPickupDesc => "Sometimes, dreams come true...";

        public override string ItemFullDescription => "At the start of the next stage, one golden egg will hatch into a void item.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidBoss;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.OnStageBeginEffect, ItemTag.CannotCopy };

        public override GameObject ItemModel => LoadDropPrefab("mdlGoldenEgg");

        public override Sprite ItemIcon => LoadItemIcon("texIconGoldenEgg");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            dropTable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtVoidChest.asset").WaitForCompletion();
            base.Init();
        }

        private Xoroshiro128Plus rng;
        public BasicPickupDropTable dropTable;
        public override void Hooks()
        {
            CharacterBody.onBodyStartGlobal += EggHatch;
        }

        private void EggHatch(CharacterBody body)
        {
            if (!NetworkServer.active)
                return;

            SpawnItem(body, body.coreTransform ?? body.transform);
        }

        void SpawnItem(CharacterBody body, Transform dropletOrigin)
        {
            int count = GetCount(body, true);
            if (count <= 0)
                return;

            UniquePickup pickupIndex = UniquePickup.none;
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            pickupIndex = dropTable.GeneratePickup(rng);

            CharacterMasterNotificationQueue.SendTransformNotification(body.master, ItemsDef.itemIndex, PickupCatalog.GetPickupDef(pickupIndex.pickupIndex).itemIndex, TransformationType.CloverVoid);

            body.inventory.RemoveItemPermanent(GoldenEgg.instance.ItemsDef);

            PickupDropletController.CreatePickupDroplet(pickupIndex, 
                dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), 
                dropletOrigin.forward * 10f, 
                isDuplicated: false,
                isRecycled: goldenEggDropsRecyclable == false
                );
        }
    }
}
