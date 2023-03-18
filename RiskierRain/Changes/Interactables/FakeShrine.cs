using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain;
using RiskierRain.Interactables;
using BepInEx.Configuration;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using System.Linq;
//using UnityEngine.SceneManagement;
using static RoR2.PickupDropTable;

namespace RiskierRain.Changes.Interactables
{
	class FakeShrine : InteractableBase<FakeShrine>
	{
		public override float voidSeedWeight => 0.2f;
		public override int normalWeight => 15;
		public override int spawnCost => 20;
		public override int costAmount => 1;
		public override int costTypeIndex => 9; //lunaritemorequipment
		public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarItemOrEquipment);

        public override int interactableMinimumStageCompletions => 1;
		public override bool automaticallyScaleCostWithDifficulty => false;
		public override bool setUnavailableOnTeleporterActivated => true;
		public override bool isShrine => true;

		//public static float floorOffset;
		public override bool orientToFloor => true;
		public override bool skipSpawnWhenSacrificeArtifactEnabled => false;
		public override float weightScalarWhenSacrificeArtifactEnabled => 1;
		public override int maxSpawnsPerStage => 1;

        public override string interactableName => "Shrine Mimic";

        public override string interactableContext => "Trade with shrine mimic";//make this good later

        public override string interactableLangToken => "FAKE_SHRINE";

        public override GameObject interactableModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChance.prefab").WaitForCompletion();
		public BasicPickupDropTable dropTable;
		//public GameObject voidChest;

        public override bool modelIsCloned => true;

        public override string modelName => "mdlShrineChance";

        public override string prefabName => "ShrineChance";

        public override int category => 4;

        public override int favoredWeight => 0;

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
			On.RoR2.PurchaseInteraction.OnInteractionBegin += FakeShrineBehavior;
			On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
			CreateLang();
			CreateInteractable();
			var cards = CreateInteractableSpawnCard();
			customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
			dropTable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtVoidChest.asset").WaitForCompletion();
        }

		private void FakeShrineBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
			if (self.displayNameToken == this.interactableLangToken)
            {                
				PickupIndex pickupIndex = PickupIndex.none;
				this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);

				pickupIndex = dropTable.GenerateDrop(rng);
				dropletOrigin = self.gameObject.transform;
				PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 10f);
				self.SetAvailable(false);								
			}
        }
		private Xoroshiro128Plus rng;
		public Transform dropletOrigin;
	}
}
