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
        ItemDef[] itemPool;
        public override string ArtifactName => "Lunacy";

        public override string ArtifactDescription => "Begin each run with a random lunar. At the end of each stage, a blue portal always appears.";

        public override string ArtifactLangTokenName => "FREELUNAR";

        public override Sprite ArtifactSelectedIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override Sprite ArtifactDeselectedIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override void Hooks()
        {

        }

        public override void OnArtifactEnabledServer()
        {
            On.RoR2.CharacterBody.Start += GiveQuickStart;

            itemPool = ItemCatalog.allItemDefs.Where(
                item => item.tier == ItemTier.Lunar
                /*&& !item.ContainsTag(ItemTag.AIBlacklist)*/
                ).ToArray();
        }

        public override void OnArtifactDisabledServer()
        {
            On.RoR2.CharacterBody.Start -= GiveQuickStart;
        }

        private void GiveQuickStart(On.RoR2.CharacterBody.orig_Start orig, RoR2.CharacterBody self)
        {
            orig(self);
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
