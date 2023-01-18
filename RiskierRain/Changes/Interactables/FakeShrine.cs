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

namespace RiskierRain.Changes.Interactables
{
	class FakeShrine : InteractableBase
	{
		public override float voidSeedWeight => 1;
		public override int normalWeight => 50;
		public override int spawnCost => 20;
		//public static GameObject interactableBodyModelPrefab;
		//public static InteractableSpawnCard interactableSpawnCard;
		//public static CostTypeDef costTypeDef;
		//public static int costTypeIndex;
		public override int costAmount => 1;
		//public static DirectorCard interactableDirectorCard;
		//public override CostTypeDef costTypeDef => ;CostTypeCatalog
		public override int costTypeIndex => 9; //lunaritemorequipment
		public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarItemOrEquipment);

        public override int interactableMinimumStageCompletions => 0;
		public override bool automaticallyScaleCostWithDifficulty => false;
		public override bool setUnavailableOnTeleporterActivated => true;
		public override bool isShrine => true;

		//public static float floorOffset;
		public override bool orientToFloor => true;
		public override bool skipSpawnWhenSacrificeArtifactEnabled => false;
		public override float weightScalarWhenSacrificeArtifactEnabled => 1;
		public override int maxSpawnsPerStage => 5;

        public override string interactableName => "Shrine Mimic";

        public override string interactableContext => "Trade";

        public override string interactableLangToken => "FAKE_SHRINE";

        public override GameObject interactableModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChance.prefab").WaitForCompletion(); //RoR2/Base/ShrineChance/ShrineChance.prefab

		public override void Init(ConfigFile config)
        {
			hasAddedInteractable = false;
			On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
			On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
			On.RoR2.PurchaseInteraction.OnInteractionBegin += FakeShrineBehavior;
			CreateLang();
			CreateInteractable();
			CreateInteractableSpawnCard();
        }

		public void Start()
		{
			if (NetworkServer.active)
			{
				this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
				CreateDropTable();
			}
		}

		private void FakeShrineBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
			if (self.displayNameToken == "VV_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
				PickupIndex pickupIndex = PickupIndex.none;
                if (dropTable)
                {
					pickupIndex = PickupDropTable.GenerateDropFromWeightedSelection(rng, weightedSelection);
					dropletOrigin = self.gameObject.transform;
					PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position, dropletOrigin.forward * 20f);

				}
                else
                {
					Debug.Log("droptable null");
                }
				//interactableModel.GetComponent<ShopTerminalBehavior>().DropPickup()
			}
        }

        public void CreateDropTable()
		{ 
			if (DLC1Content.Items.CloverVoid == null)
            {
				Debug.Log("benthic null?");
            }
            else
            {
				if (DLC1Content.Items.CloverVoid.itemIndex == null)
                {
					Debug.Log("itemindex null?");
                }
                else
                {
					if (weightedSelection == null)
                    {
						Debug.Log("what the fuck dude");
                    }
                    else
                    {
						weightedSelection.AddChoice(PickupCatalog.itemIndexToPickupIndex[(int)DLC1Content.Items.CloverVoid.itemIndex], 1);

					}
				}
            }
        }
		public PickupDropTable dropTable;
		public WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>();
		private Xoroshiro128Plus rng;
		public Transform dropletOrigin;

	}
}
