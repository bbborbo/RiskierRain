using BepInEx.Configuration;
using SwanSongExtended.Equipment;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SwanSongExtended.Modules;
using UnityEngine.Networking;

namespace SwanSongExtended.Artifacts
{
    class QuickStartArtifact : ArtifactBase<QuickStartArtifact>
    {
        #region config

        [AutoConfig("Wish Pickup Index", "0 is Common, 1 is Uncommon, 2 is Rare", 1)]
        public static int wishPickupIndex = 1;
        public static string ConvertPickupIndexToRarityName(int n)
        {
            string rarityName = "";
            switch (n)
            {
                case 0:
                    rarityName = "Common";
                    break;
                case 1:
                    rarityName = "Uncommon";
                    break;
                case 2:
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

        public override string ArtifactName => "the Stillborn";

        public override string ArtifactDescription => $"Begin your run with an {GetRarityName()} item of your choice.";

        public override string ArtifactLangTokenName => "QUICKSTART";

        public override Sprite ArtifactSelectedIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/quickstart.png");

        public override Sprite ArtifactDeselectedIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/quickstartoff.png");

        public override void Hooks()
        {
            On.RoR2.CharacterBody.Start += GiveQuickStart;
        }

        public override void OnArtifactEnabledServer()
        {
        }

        public override void OnArtifactDisabledServer()
        {
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
