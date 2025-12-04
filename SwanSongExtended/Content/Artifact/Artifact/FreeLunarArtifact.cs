using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SwanSongExtended.Artifacts 
{
    class FreeLunarArtifact : ArtifactBase<FreeLunarArtifact>
    {
        const int _FreeLunarBlacklist = (int)ItemTag.SacrificeBlacklist;
        public static ItemTag FreeLunarBlacklist => (ItemTag)_FreeLunarBlacklist;
        ItemDef[] itemPool;
        public override string ArtifactName => "the Zealot";

        public override string ArtifactDescription => "Begin each run with a random lunar. At the end of each stage, a blue portal always appears.";

        public override string ArtifactLangTokenName => "FREELUNAR";

        public override Sprite ArtifactSelectedIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/freelunar.png");

        public override Sprite ArtifactDeselectedIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/freelunaroff.png");

        public override void Init()
        {
            base.Init();

            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarPrimaryReplacement), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarSecondaryReplacement), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarUtilityReplacement), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarSpecialReplacement), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.RepeatHeal), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarTrinket), FreeLunarBlacklist);
            Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_LunarSun.LunarSun_asset).Completed += (ctx) =>
                SwanSongPlugin.BlacklistSingleItem(ctx.Result, FreeLunarBlacklist);
            Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_HalfAttackSpeedHalfCooldowns.HalfAttackSpeedHalfCooldowns_asset).Completed += (ctx) =>
                SwanSongPlugin.BlacklistSingleItem(ctx.Result, FreeLunarBlacklist);
            Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_HalfSpeedDoubleHealth.HalfSpeedDoubleHealth_asset).Completed += (ctx) =>
                SwanSongPlugin.BlacklistSingleItem(ctx.Result, FreeLunarBlacklist);
        }
        public override void Hooks()
        {
            On.RoR2.CharacterBody.Start += GiveQuickStart;
            On.RoR2.TeleporterInteraction.Start += OnTeleporterStart;
            On.RoR2.Run.OnServerTeleporterPlaced += OnTeleporterPlaced;
        }

        private void OnTeleporterStart(On.RoR2.TeleporterInteraction.orig_Start orig, TeleporterInteraction self)
        {
            orig(self);
            if (IsArtifactEnabled())
            {
                self.shouldAttemptToSpawnShopPortal = true;
                foreach (PortalStatueBehavior portalStatueBehavior in GameObject.FindObjectsOfType<PortalStatueBehavior>())
                {
                    PurchaseInteraction purchaseInteraction;
                    if (portalStatueBehavior.portalType == PortalStatueBehavior.PortalType.Shop && portalStatueBehavior.TryGetComponent<PurchaseInteraction>(out purchaseInteraction))
                    {
                        purchaseInteraction.Networkavailable = false;
                        portalStatueBehavior.CallRpcSetPingable(portalStatueBehavior.gameObject, false);
                    }
                }
            }
        }

        public override void OnArtifactEnabledServer()
        {
            itemPool = ItemCatalog.allItemDefs.Where(
                item => item.tier == ItemTier.Lunar
                && !item.ContainsTag(ItemTag.WorldUnique) && !item.ContainsTag(ItemTag.SacrificeBlacklist)
                ).ToArray();
        }

        private void OnTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced orig, Run self, SceneDirector sceneDirector, GameObject teleporter)
        {
            orig(self, sceneDirector, teleporter);
            if (IsArtifactEnabled() && teleporter.TryGetComponent(out TeleporterInteraction tp))
            {
                tp.shouldAttemptToSpawnShopPortal = true;
                foreach (PortalStatueBehavior portalStatueBehavior in GameObject.FindObjectsOfType<PortalStatueBehavior>())
                {
                    PurchaseInteraction purchaseInteraction;
                    if (portalStatueBehavior.portalType == PortalStatueBehavior.PortalType.Shop && portalStatueBehavior.TryGetComponent<PurchaseInteraction>(out purchaseInteraction))
                    {
                        purchaseInteraction.Networkavailable = false;
                        portalStatueBehavior.CallRpcSetPingable(portalStatueBehavior.gameObject, false);
                    }
                }
            }
        }

        public override void OnArtifactDisabledServer()
        {
            //On.RoR2.CharacterBody.Start -= GiveQuickStart;
        }

        private void GiveQuickStart(On.RoR2.CharacterBody.orig_Start orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (IsArtifactEnabled() && NetworkServer.active && Run.instance)
            {
                bool isStageone = Run.instance.stageClearCount == 0 && Run.instance.GetRunStopwatch() <= 20;
                if (!isStageone)
                {
                    return;
                }
                if (self.isPlayerControlled)
                {
                    OnPlayerCharacterBodyStartServer(self);
                }
            }
        }

        private void OnPlayerCharacterBodyStartServer(CharacterBody characterBody)
        {
            Inventory inventory = characterBody.inventory;
            if (inventory != null)
            {
                int i = UnityEngine.Random.RandomRangeInt(0, itemPool.Length - 1);
                ItemIndex itemToGive = itemPool[i].itemIndex;
                inventory.GiveItem(itemToGive);
            }
        }
    }
}
