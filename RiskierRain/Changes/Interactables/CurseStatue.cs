using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRain.Items;


namespace RiskierRain.Interactables
{
    class CurseStatue : InteractableBase<CurseStatue>
    {
        public override string interactableName => "Curse Statue";

        public override string interactableContext => "Curse yourself";

        public override string interactableLangToken => "CURSE_STATUE";

        public override GameObject interactableModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlCurseStatue.prefab");
        public override string modelName => "mdlCurseStatue";

        public override string prefabName => "mdlCurseStatue";

        public override bool modelIsCloned => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 1000;

        public override int favoredWeight => 0;

        public override int category => 7;

        public override int spawnCost => 0;

        public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.None);

        public override int costTypeIndex => 0;

        public override int costAmount => 0;

        public override int interactableMinimumStageCompletions => 0;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => true;

        public override bool orientToFloor => true;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 3;

        public string[] validScenes = {
            "golemplains",
            "golemplains2",
            "blackbeach",
            "blackbeach2",
            "snowyforest",
            "foggyswamp",
            "goolake",
            "frozenwall",
            "wispgraveyard",
            "dampcavesimple",
            "shipgraveyard",
            "arena",
            "skymeadow",
            "artifactworld",
            "rootjungle",
            "ancientloft",
            "sulfurpools",
			//modded stages
			"slumberingsatellite",
            "forgottenhaven",
            "drybasin",
            "FBLScene"
        };

        public override void Init(ConfigFile config)
        {
            hasAddedInteractable = false;
            On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += CurseStatueBehavior;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        private void CurseStatueBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {//TODO: make it give a lunar coin the first time its used, and also make it a menu instead of what it is rn
            orig(self, activator);
            if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
                PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(Items.Helpers.EnemyHealthUp.instance.ItemsDef.itemIndex);

                dropletOrigin = self.gameObject.transform;
                PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 10f);
            }
        }
        public Transform dropletOrigin;
    }
}
