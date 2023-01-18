using BepInEx.Configuration;
using static RiskierRain.CoreModules.CoreModule;
using R2API;
//using On.RoR2;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;
using RoR2.EntitlementManagement;
using RoR2.Navigation;
using UnityEngine.Networking;
using System.Linq;
using RoR2.Hologram;

namespace RiskierRain.Interactables
{
	public abstract class InteractableBase<T> : InteractableBase where T : InteractableBase<T>
	{
		public static T instance { get; private set; }

		public InteractableBase()
		{
			if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
			instance = this as T;
		}

	}

	public abstract class InteractableBase
    {
		public abstract string interactableName { get; }
		public abstract string interactableContext { get; }
		public abstract string interactableLangToken { get; }
		public abstract GameObject interactableModel { get; }
		public abstract void Init(ConfigFile config);
		protected void CreateLang()
		{
			LanguageAPI.Add("VV_INTERACTABLE_" + this.interactableLangToken + "_NAME", this.interactableName);
			LanguageAPI.Add("VV_INTERACTABLE_" + this.interactableLangToken + "_CONTEXT", this.interactableContext);
		}

		public void CreateInteractable()
        {
			if (interactableModel == null)
			{
				Debug.Log("interactableModel null :(");
			}
			else
			{
				interactableBodyModelPrefab = this.interactableModel;
				interactableBodyModelPrefab.AddComponent<NetworkIdentity>();
				PurchaseInteraction purchaseInteraction = interactableBodyModelPrefab.AddComponent<PurchaseInteraction>();

				purchaseInteraction.displayNameToken = "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME";
				purchaseInteraction.contextToken = "2R4R_INTERACTABLE_" + this.interactableLangToken + "_CONTEXT";
				purchaseInteraction.costType = (CostTypeIndex)costTypeIndex;
				purchaseInteraction.automaticallyScaleCostWithDifficulty = automaticallyScaleCostWithDifficulty;
				purchaseInteraction.cost = costAmount;
				purchaseInteraction.available = true;
				purchaseInteraction.setUnavailableOnTeleporterActivated = true;
				purchaseInteraction.isShrine = isShrine;
				purchaseInteraction.isGoldShrine = false;

				PingInfoProvider pingInfoProvider = interactableBodyModelPrefab.AddComponent<PingInfoProvider>();
				//pingInfoProvider.pingIconOverride = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("texShrineIconOutlined");
				GenericDisplayNameProvider genericDisplayNameProvider = interactableBodyModelPrefab.AddComponent<GenericDisplayNameProvider>();
				genericDisplayNameProvider.displayToken = "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME";
				MeshCollider childMeshCollider = interactableBodyModelPrefab.GetComponentInChildren<MeshCollider>();
				if (childMeshCollider == null)
                {
					Debug.Log("child null");
                }
                else
				{
					GameObject childGameObject = childMeshCollider.gameObject;
					if (childGameObject == null)
                    {
						Debug.Log("childobject null");
                    }
                    else
                    {
						EntityLocator entityLocator = childGameObject.AddComponent<EntityLocator>();
						if (entityLocator == null)
						{
							Debug.Log("entitylocator null");
						}
						else
						{
							entityLocator.entity = interactableBodyModelPrefab;
							ModelLocator modelLocator = interactableBodyModelPrefab.AddComponent<ModelLocator>();
							modelLocator.modelTransform = interactableBodyModelPrefab.transform.Find("mdlVoidShrine");
							modelLocator.modelBaseTransform = modelLocator.modelTransform;
							modelLocator.dontDetatchFromParent = true;
							modelLocator.autoUpdateModelTransform = true;
							Highlight component = interactableBodyModelPrefab.GetComponent<Highlight>();
							component.targetRenderer = (from x in interactableBodyModelPrefab.GetComponentsInChildren<MeshRenderer>()
														where x.gameObject.name.Contains("VoidShrine")
														select x).First<MeshRenderer>();
							component.strength = 1f;
							component.highlightColor = Highlight.HighlightColor.interactive;
							HologramProjector hologramProjector = interactableBodyModelPrefab.AddComponent<HologramProjector>();
							hologramProjector.hologramPivot = interactableBodyModelPrefab.transform.Find("HologramPivot");
							hologramProjector.displayDistance = 10f;
							hologramProjector.disableHologramRotation = true;
							ChildLocator childLocator = interactableBodyModelPrefab.AddComponent<ChildLocator>();
							childLocator.transformPairs = new ChildLocator.NameTransformPair[]
							{
								new ChildLocator.NameTransformPair
								{
									name = "FireworkOrigin",
									transform = interactableBodyModelPrefab.transform.Find("FireworkEmitter")
								}
							};
							PrefabAPI.RegisterNetworkPrefab(interactableBodyModelPrefab);
						}
					}

				}
				
			}
		}
		public void CreateInteractableSpawnCard()
        {
			interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();

			interactableSpawnCard.directorCreditCost = spawnCost;
			interactableSpawnCard.occupyPosition = true;
			interactableSpawnCard.orientToFloor = orientToFloor;
			interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = skipSpawnWhenSacrificeArtifactEnabled;
			interactableSpawnCard.weightScalarWhenSacrificeArtifactEnabled = weightScalarWhenSacrificeArtifactEnabled;
			interactableSpawnCard.maxSpawnsPerStage = maxSpawnsPerStage;

			interactableDirectorCard = new DirectorCard
			{
				selectionWeight = normalWeight,
				spawnCard = interactableSpawnCard,
				minimumStageCompletions = interactableMinimumStageCompletions
			};
			//DirectorAPI.Helpers.AddNewInteractableToStage()
			DirectorAPI.Helpers.AddNewInteractableToStage(interactableDirectorCard, DirectorAPI.Helpers.GetInteractableCategory("shrines"),DirectorAPI.Stage.TitanicPlains, "");
			DirectorAPI.Helpers.AddNewInteractableToStage(interactableDirectorCard, DirectorAPI.Helpers.GetInteractableCategory("shrines"), DirectorAPI.Stage.DistantRoost, "");
			DirectorAPI.Helpers.AddNewInteractableToStage(interactableDirectorCard, DirectorAPI.Helpers.GetInteractableCategory("shrines"), DirectorAPI.Stage.SiphonedForest, "");


		}


		public CharacterBody LastActivator;
		public PurchaseInteraction PurchaseInteraction;

		public abstract float voidSeedWeight { get; }
		public abstract int normalWeight { get; }
		public abstract int spawnCost { get; }
		public static GameObject interactableBodyModelPrefab;
		public static InteractableSpawnCard interactableSpawnCard;
		public abstract CostTypeDef costTypeDef { get; }
		public abstract int costTypeIndex { get; }
		public abstract int costAmount { get; }
		public static DirectorCard interactableDirectorCard;
		public bool hasAddedInteractable;

		public abstract  int interactableMinimumStageCompletions { get; }
		public abstract bool automaticallyScaleCostWithDifficulty { get; }
		public abstract bool setUnavailableOnTeleporterActivated { get; }
		public abstract bool isShrine { get; }

		//public static float floorOffset;
		public abstract bool orientToFloor { get; }
		public abstract bool skipSpawnWhenSacrificeArtifactEnabled { get; }
		public abstract float weightScalarWhenSacrificeArtifactEnabled { get; }
		public abstract int maxSpawnsPerStage { get; }

		//stages to spawn on (help me)



		public string InteractableName(On.RoR2.PurchaseInteraction.orig_GetDisplayName orig, PurchaseInteraction self)
		{
			bool flag = self.displayNameToken == "VV_INTERACTABLE_" + this.interactableLangToken + "_NAME";
			string result;
			if (flag)
			{
				result = this.interactableName;
			}
			else
			{
				result = orig.Invoke(self);
			}
			return result;
		}

		public DirectorCard VoidCampAddInteractable(On.RoR2.CampDirector.orig_SelectCard orig, CampDirector self, WeightedSelection<DirectorCard> deck, int maxCost)
		{
			this.hasAddedInteractable = false;
			bool flag = self.name == "Camp 1 - Void Monsters & Interactables";
			if (flag)
			{
				for (int i = deck.Count - 1; i >= 0; i--)
				{
					bool flag2 = deck.GetChoice(i).value.spawnCard.name == "iscVoidPortalInteractable";
					if (flag2)
					{
						this.hasAddedInteractable = true;
						break;
					}
				}
				bool flag3 = !this.hasAddedInteractable;
				if (flag3)
				{
					deck.AddChoice(interactableDirectorCard, this.voidSeedWeight);
				}
			}
			return orig.Invoke(self, deck, maxCost);
		}
	}
}
