using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Artifacts 
{
    class FreeLunarArtifact : ArtifactBase<FreeLunarArtifact>
    {
        const int _FreeLunarBlacklist = (int)ItemTag.SacrificeBlacklist;
        public static ItemTag FreeLunarBlacklist => (ItemTag)_FreeLunarBlacklist;
        ItemDef[] itemPool;
        public override string ArtifactName => "Lunacy";

        public override string ArtifactDescription => "Begin each run with a random lunar. At the end of each stage, a blue portal always appears.";

        public override string ArtifactLangTokenName => "FREELUNAR";

        public override Sprite ArtifactSelectedIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override Sprite ArtifactDeselectedIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override void Init()
        {
            base.Init();

            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarPrimaryReplacement), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarSecondaryReplacement), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarUtilityReplacement), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarSpecialReplacement), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.RepeatHeal), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarTrinket), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(DLC1Content.Items.HalfAttackSpeedHalfCooldowns), FreeLunarBlacklist);
            SwanSongPlugin.BlacklistSingleItem(nameof(DLC1Content.Items.HalfSpeedDoubleHealth), FreeLunarBlacklist);
        }
        public override void Hooks()
        {
            On.RoR2.CharacterBody.Start += GiveQuickStart;
        }

        public override void OnArtifactEnabledServer()
        {

            itemPool = ItemCatalog.allItemDefs.Where(
                item => item.tier == ItemTier.Lunar
                && !item.ContainsTag(ItemTag.WorldUnique) && !item.ContainsTag(ItemTag.SacrificeBlacklist)
                ).ToArray();
        }

        public override void OnArtifactDisabledServer()
        {
            //On.RoR2.CharacterBody.Start -= GiveQuickStart;
        }

        private void GiveQuickStart(On.RoR2.CharacterBody.orig_Start orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef))
            {
                bool isStageone = Run.instance.stageClearCount == 0;
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
