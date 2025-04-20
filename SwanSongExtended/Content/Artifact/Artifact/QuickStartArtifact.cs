using BepInEx.Configuration;
using SwanSongExtended.Equipment;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Artifacts
{
    class QuickStartArtifact : ArtifactBase<QuickStartArtifact>
    {
        #region config

        [AutoConfig("Wish Pickup Index", "2 is Common, 3 is Uncommon, 4 is Rare", 3)]
        public static int wishPickupIndex = 3;
        public static string ConvertPickupIndexToRarityName(int n)
        {
            string rarityName = "";
            switch (n)
            {
                case 2:
                    rarityName = "Common";
                    break;
                case 3:
                    rarityName = "Uncommon";
                    break;
                case 4:
                    rarityName = "Rare";
                    break;
                default:
                    break;
            }
            return rarityName;
        }
        public static string GetRarityName()
        {
            return ConvertPickupIndexToRarityName(wishPickupIndex);
        }
        #endregion

        public override string ArtifactName => "Initiative";

        public override string ArtifactDescription => $"Begin your run with an {GetRarityName()} item of your choice.";

        public override string ArtifactLangTokenName => "QUICKSTART";

        public override Sprite ArtifactSelectedIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override Sprite ArtifactDeselectedIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override void Hooks()
        {

        }

        public override void OnArtifactEnabledServer()
        {
            On.RoR2.CharacterBody.Start += GiveQuickStart;
        }

        public override void OnArtifactDisabledServer()
        {
            On.RoR2.CharacterBody.Start -= GiveQuickStart;
        }

        private void GiveQuickStart(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
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
        private static void OnPlayerCharacterBodyStartServer(CharacterBody characterBody)
        {
            Inventory inventory = characterBody.inventory;
            if (inventory != null)
            {
                //inventory.SetEquipmentIndex(QuickStartEquipment.instance.EquipDef.equipmentIndex);
                EquipmentState equipmentState = new EquipmentState(QuickStartEquipment.instance.EquipDef.equipmentIndex, Run.FixedTimeStamp.negativeInfinity, 1);
                inventory.SetEquipment(equipmentState, 0);
            }
        }
    }
}
