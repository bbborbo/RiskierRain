using BepInEx.Configuration;
using RiskierRain.Interactables;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Changes.Interactables
{
    class ConstructConstruct : InteractableBase<ConstructConstruct>
    {
        public override string interactableName => "Decayed Construct";

        public override string interactableContext => "Kick the Construct";

        public override string interactableLangToken => "CONSTRUCT_CONSCTRUCT";

        public override GameObject interactableModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/constructConstruct.prefab");

        public override string modelName => "mdlConstructConstruct";

        public override string prefabName => "constructConstruct";

        public override bool modelIsCloned => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 1;

        public override int favoredWeight => 0;

        public override int category => 4;

        public override int spawnCost => 15;

        public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.None);

        public override int costTypeIndex => 0;

        public override int costAmount => 0;

        public override int interactableMinimumStageCompletions => 1;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => true;

        public override bool orientToFloor => true;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 3;

        public string[] validScenes = {
            "foggyswamp",
            "dampcavesimple",
            "sulfurpools",
			//modded stages
            "drybasin"
            //"FBLScene"
        };

        public override void Init(ConfigFile config)
        {
            //hasAddedInteractable = false;
            //On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            //On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
            //On.RoR2.PurchaseInteraction.OnInteractionBegin += ConstructConstructBehavior;
            //On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            //CreateLang();
            //CreateInteractable();
            //var cards = CreateInteractableSpawnCard();
            //customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
        }

        private void ConstructConstructBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (self.displayNameToken != "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
                return;
            }

        }
    }
}
