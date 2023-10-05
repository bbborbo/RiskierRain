using BepInEx.Configuration;
using RiskierRain.Equipment;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Artifacts
{
    class QuickStartArtifact : ArtifactBase<QuickStartArtifact>
    {
        public override string ArtifactName => "Initiative";

        public override string ArtifactDescription => "Begin your run with an Uncommon item of your choice.";

        public override string ArtifactLangTokenName => "QUICKSTART";

        public override Sprite ArtifactSelectedIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override Sprite ArtifactDeselectedIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override BalanceCategory Category => BalanceCategory.StateOfDifficulty;

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateArtifact();
        }

        public override void OnEnabled()
        {
            On.RoR2.CharacterBody.Start += GiveQuickStart;
        }

        public override void OnDisabled()
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
