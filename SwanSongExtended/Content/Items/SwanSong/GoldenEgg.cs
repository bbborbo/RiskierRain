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

namespace SwanSongExtended.Items
{
    class GoldenEgg : ItemBase<GoldenEgg>
    {
        public override bool lockEnabled => true;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Golden Egg";

        public override string ItemLangTokenName => "GOLDEN_EGG";

        public override string ItemPickupDesc => "There's something inside...";

        public override string ItemFullDescription => "At the start of the next stage, one golden egg will hatch into a void item.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidBoss;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.OnStageBeginEffect };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/goldenegg.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texGoldenEggIcon.png");

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
            On.RoR2.Stage.RespawnCharacter += EggHatch;
        }

        private void EggHatch(On.RoR2.Stage.orig_RespawnCharacter orig, Stage self, CharacterMaster characterMaster)
        {
            orig(self, characterMaster);
            if (NetworkServer.active)
            {
                int count = GetCount(characterMaster);
                if (count > 0)
                {
                    SpawnItem(characterMaster);
                    characterMaster.inventory.RemoveItem(GoldenEgg.instance.ItemsDef);
                }
            }
        }

        void SpawnItem(CharacterMaster characterMaster)
        {
            PickupIndex pickupIndex = PickupIndex.none;
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);

            pickupIndex = dropTable.GenerateDrop(rng);
            Transform dropletOrigin = characterMaster.bodyInstanceObject.transform;
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 10f);
        }
    }
}
