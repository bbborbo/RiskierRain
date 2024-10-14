using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent;
using RiskierRainContent.Interactables;
using BepInEx.Configuration;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using System.Linq;
//using UnityEngine.SceneManagement;
using static RoR2.PickupDropTable;
using UnityEngine.Events;
using RiskierRainContent.Changes.Components;

namespace RiskierRainContent.Interactables
{
	class FakeShrine : InteractableBase<FakeShrine>
	{
		public override float voidSeedWeight => 0.3f;
		public override int normalWeight => 15;
		public override int spawnCost => 20;
		public override int costAmount => 1;
		public override CostTypeIndex costTypeIndex => CostTypeIndex.LunarItemOrEquipment; //lunaritemorequipment

        public override int interactableMinimumStageCompletions => 1;
		public override bool automaticallyScaleCostWithDifficulty => false;
		public override bool setUnavailableOnTeleporterActivated => true;
		public override bool isShrine => true;

		//public static float floorOffset;
		public override bool orientToFloor => true;
		public override bool skipSpawnWhenSacrificeArtifactEnabled => false;
		public override float weightScalarWhenSacrificeArtifactEnabled => 1;
		public override int maxSpawnsPerStage => 1;

        public override string InteractableName => "Shrine Mimic";

        public override string InteractableContext => "Trade with shrine mimic";//make this good later

        public override string InteractableLangToken => "FAKE_SHRINE";

        public override GameObject InteractableModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChance.prefab").WaitForCompletion();
		public BasicPickupDropTable dropTable;
		//public GameObject voidChest;

        public override bool ShouldCloneModel => true;

        public override string modelName => "mdlShrineChance";

        public override string prefabName => "ShrineChance";

		public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Shrines;

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
			On.RoR2.PurchaseInteraction.OnInteractionBegin += FakeShrineBehavior;
			CreateLang();
			CreateInteractable();
			var cards = CreateInteractableSpawnCard();
			customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
			dropTable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtVoidChest.asset").WaitForCompletion();
        }

		private void FakeShrineBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
			if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME")
            {                
				PickupIndex pickupIndex = PickupIndex.none;
				this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);

				pickupIndex = dropTable.GenerateDrop(rng);
				dropletOrigin = self.gameObject.transform;
				PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 10f);
				self.SetAvailable(false);								
			}
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
			ShrineChanceBehavior shrineBehavior = interaction.gameObject.GetComponent<ShrineChanceBehavior>();
			//shrineBehavior.dropTable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtVoidChest.asset").WaitForCompletion();
			//shrineBehavior.
			GameObject.Destroy(shrineBehavior);
			FakeShrineBehavior chestBehavior = interaction.gameObject.AddComponent<FakeShrineBehavior>();
			chestBehavior.dropTable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtVoidChest.asset").WaitForCompletion();

			return chestBehavior.OnInteractionBegin;
        }

        private Xoroshiro128Plus rng;
		public Transform dropletOrigin;
	}

	public class FakeShrineBehavior : CustomChestBehavior
    {
        public override void OnInteractionBegin(Interactor activator)
        {
            base.OnInteractionBegin(activator);
			Transform symbolTransform = this.transform.Find("Symbol");
			if(symbolTransform != null)
				symbolTransform.gameObject.SetActive(false);

			CallRpcSetPingable(false);
		}
		protected static void InvokeRpcRpcSetPingable(NetworkBehaviour obj, NetworkReader reader)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcSetPingable called on server.");
				return;
			}
		   ((FakeShrineBehavior)obj).RpcSetPingable(reader.ReadBoolean());
		}

		public void CallRpcSetPingable(bool value)
		{
			if (!NetworkServer.active)
			{
				Debug.LogError("RPC Function RpcSetPingable called on client.");
				return;
			}
			NetworkWriter networkWriter = new NetworkWriter();
			networkWriter.Write(0);
			networkWriter.Write((short)((ushort)2));
			networkWriter.WritePackedUInt32((uint)ShrineBehavior.kRpcRpcSetPingable);
			networkWriter.Write(base.GetComponent<NetworkIdentity>().netId);
			networkWriter.Write(value);
			this.SendRPCInternal(networkWriter, 0, "RpcSetPingable");
		}
		[ClientRpc]
		protected virtual void RpcSetPingable(bool value)
		{
			NetworkIdentity component = base.GetComponent<NetworkIdentity>();
			if (component)
			{
				component.isPingable = value;
			}
		}
	}
}
