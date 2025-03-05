using BepInEx.Configuration;
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
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using SwanSongExtended;

namespace SwanSongExtended.Interactables
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

	public abstract class InteractableBase : SharedBase
    {
		public override AssetBundle assetBundle => SwanSongPlugin.orangeAssetBundle;
        public abstract string InteractableName { get; }
		public abstract string InteractableContext { get; }
		public abstract string InteractableLangToken { get; }
		public abstract GameObject InteractableModel { get; }

		//janky solutions inbound!!! dorry :(
		public abstract string modelName { get; }
		public abstract string prefabName { get; }//???? fuck
		public abstract bool ShouldCloneModel { get; }
		public GameObject model;

		public CustomInteractable customInteractable = new CustomInteractable();

		public PurchaseInteraction InteractionComponent;

		public abstract float voidSeedWeight { get; }
		public abstract int normalWeight { get; }
		public abstract int favoredWeight { get; }
		public abstract DirectorAPI.InteractableCategory category { get; }
		public abstract int spawnCost { get; }
		public static GameObject interactableBodyModelPrefab;
		public static InteractableSpawnCard interactableSpawnCard;
		public abstract CostTypeIndex costTypeIndex { get; }
		public abstract int costAmount { get; }
		public static DirectorCard interactableDirectorCard;
		public bool hasAddedInteractable;

		public abstract int interactableMinimumStageCompletions { get; }
		public abstract bool automaticallyScaleCostWithDifficulty { get; }
		public abstract bool setUnavailableOnTeleporterActivated { get; }
		public abstract bool isShrine { get; }

		//public static float floorOffset;
		public abstract bool orientToFloor { get; }
		public abstract bool skipSpawnWhenSacrificeArtifactEnabled { get; }
		public abstract float weightScalarWhenSacrificeArtifactEnabled { get; }
		public abstract int maxSpawnsPerStage { get; }

		//stages to spawn on (help me)

		public abstract void Init(ConfigFile config);
		public override void Lang()
		{
			LanguageAPI.Add("2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME", this.InteractableName);
			LanguageAPI.Add("2R4R_INTERACTABLE_" + this.InteractableLangToken + "_CONTEXT", this.InteractableContext);
		}
		public abstract UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction);
		public void CreateInteractable()
        {
			if (InteractableModel == null)
			{
				Debug.Log("interactableModel null :(");
				return;
			}
			bool hajabaja = modelName == prefabName;
			if (!ShouldCloneModel)
            {
				model = InteractableModel;
            }
            else
            {
				model = InteractableModel.InstantiateClone(prefabName, true); 
            }
			interactableBodyModelPrefab = this.model;
			interactableBodyModelPrefab.AddComponent<NetworkIdentity>();
			PurchaseInteraction oldPurchaseInteraction = interactableBodyModelPrefab.GetComponent<PurchaseInteraction>();
			if (oldPurchaseInteraction != null)
            {
				GameObject.Destroy(oldPurchaseInteraction);
            }
			InteractionComponent = interactableBodyModelPrefab.AddComponent<PurchaseInteraction>();

			InteractionComponent.displayNameToken = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME";
			InteractionComponent.contextToken = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_CONTEXT";
			InteractionComponent.costType = (CostTypeIndex)costTypeIndex;
			InteractionComponent.automaticallyScaleCostWithDifficulty = automaticallyScaleCostWithDifficulty;
			InteractionComponent.cost = costAmount;
			InteractionComponent.available = true;
			InteractionComponent.setUnavailableOnTeleporterActivated = setUnavailableOnTeleporterActivated;
			InteractionComponent.isShrine = isShrine;
			InteractionComponent.isGoldShrine = false;
			InteractionComponent.onPurchase = new PurchaseEvent();
			UnityAction<Interactor> onPurchaseAction = GetInteractionAction(InteractionComponent);
			if(onPurchaseAction != null)
			{
				Debug.Log("adding purchase action for " + InteractableName);
				InteractionComponent.onPurchase.AddListener(onPurchaseAction);
			}

			PingInfoProvider pingInfoProvider = interactableBodyModelPrefab.GetComponent<PingInfoProvider>();
			if (pingInfoProvider == null)
			{
				pingInfoProvider = interactableBodyModelPrefab.AddComponent<PingInfoProvider>();
				pingInfoProvider.pingIconOverride = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texShrineIconOutlined.png").WaitForCompletion(); //only works for shrines? change later i guess
				
			}

			GenericDisplayNameProvider genericDisplayNameProvider = interactableBodyModelPrefab.GetComponent<GenericDisplayNameProvider>();
			if (genericDisplayNameProvider == null)
			{
				genericDisplayNameProvider = interactableBodyModelPrefab.AddComponent<GenericDisplayNameProvider>();
			}
			genericDisplayNameProvider.displayToken = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME";

			if (normalWeight > 0 || favoredWeight > 0)
			{
				On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
			}
			if(voidSeedWeight > 0)
			{
				On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
			}


			Collider childCollider = interactableBodyModelPrefab.GetComponentInChildren<Collider>();
			if (childCollider == null)
            {
				Debug.Log("child null");
				return;
            }
			EntityLocator entityLocator = interactableBodyModelPrefab.GetComponent<EntityLocator>();
			if (entityLocator == null)
			{
				Debug.Log("entitylocator null, adding component");
				entityLocator = interactableBodyModelPrefab.AddComponent<EntityLocator>();
			}
			if (entityLocator != null)
			{
				entityLocator.entity = interactableBodyModelPrefab;
				ModelLocator modelLocator = interactableBodyModelPrefab.GetComponent<ModelLocator>();
				if (modelLocator == null)
                {
					Debug.Log("modellocator null, adding component");
					modelLocator = interactableBodyModelPrefab.AddComponent<ModelLocator>();
					modelLocator.modelTransform = interactableBodyModelPrefab.transform.Find(modelName);//pawsible problem area? ()
					modelLocator.modelBaseTransform = modelLocator.modelTransform;
					modelLocator.dontDetatchFromParent = true;
					modelLocator.autoUpdateModelTransform = true;

					Highlight component = interactableBodyModelPrefab.GetComponent<Highlight>();
					if (component == null)
					{
						Debug.Log("highlight null, adding component");
						component = interactableBodyModelPrefab.AddComponent<Highlight>();
					}
					if (component != null)
					{

						component.targetRenderer = (from x in interactableBodyModelPrefab.GetComponentsInChildren<MeshRenderer>()
													where x.gameObject.name.Contains(modelName)
													select x).First<MeshRenderer>();

						component.strength = 1f;
						component.highlightColor = Highlight.HighlightColor.interactive;
					}
					HologramProjector hologramProjector = interactableBodyModelPrefab.GetComponent<HologramProjector>();
					if (hologramProjector == null)
					{
						Debug.Log("hologramProjector null, adding component");
						hologramProjector = interactableBodyModelPrefab.AddComponent<HologramProjector>();
					}
					if (hologramProjector != null)
					{
						hologramProjector.hologramPivot = interactableBodyModelPrefab.transform.Find("HologramPivot"); // this might be fucky
						hologramProjector.displayDistance = 10f;
						hologramProjector.disableHologramRotation = false;
					}
					ChildLocator childLocator = interactableBodyModelPrefab.GetComponent<ChildLocator>();

					if (childLocator == null)
					{
						Debug.Log("childLocator null, adding component");
						childLocator = interactableBodyModelPrefab.AddComponent<ChildLocator>();
					}
					if (childLocator != null)
					{
						childLocator.transformPairs = new ChildLocator.NameTransformPair[]
						{
							new ChildLocator.NameTransformPair
							{
								name = "FireworkOrigin",
								transform = interactableBodyModelPrefab.transform.Find("FireworkEmitter")
							}
						};
						Debug.Log("interactable registered");
					}
				}
			}
			PrefabAPI.RegisterNetworkPrefab(interactableBodyModelPrefab);
		}
		public (DirectorCard directorCard, InteractableSpawnCard interactableSpawnCard)  CreateInteractableSpawnCard()
        {
			interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();

			interactableSpawnCard.directorCreditCost = spawnCost;
			interactableSpawnCard.eliteRules = SpawnCard.EliteRules.Default;
			interactableSpawnCard.sendOverNetwork = true;
			interactableSpawnCard.occupyPosition = true;
			interactableSpawnCard.orientToFloor = orientToFloor;
			interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = skipSpawnWhenSacrificeArtifactEnabled;
			interactableSpawnCard.weightScalarWhenSacrificeArtifactEnabled = weightScalarWhenSacrificeArtifactEnabled;
			interactableSpawnCard.maxSpawnsPerStage = maxSpawnsPerStage;
			interactableSpawnCard.hullSize = HullClassification.Human;
			interactableSpawnCard.prefab = model;
			interactableSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
			interactableSpawnCard.name = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME";

			interactableDirectorCard = new DirectorCard
			{
				selectionWeight = normalWeight,
				spawnCard = interactableSpawnCard,
				preventOverhead = false,
				minimumStageCompletions = interactableMinimumStageCompletions
			};
			Debug.Log("Created spawncard for " + "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME" + "; " + interactableDirectorCard.spawnCard.name + ", " + interactableSpawnCard.name);

			return (interactableDirectorCard, interactableSpawnCard);
		}
		public (DirectorCard directorCard, InteractableSpawnCard interactableSpawnCard) CreateInteractableSpawnCard(bool isFavored)
		{
			interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();

			interactableSpawnCard.directorCreditCost = spawnCost;
			interactableSpawnCard.eliteRules = SpawnCard.EliteRules.Default;
			interactableSpawnCard.sendOverNetwork = true;
			interactableSpawnCard.occupyPosition = true;
			interactableSpawnCard.orientToFloor = orientToFloor;
			interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = skipSpawnWhenSacrificeArtifactEnabled;
			interactableSpawnCard.weightScalarWhenSacrificeArtifactEnabled = weightScalarWhenSacrificeArtifactEnabled;
			interactableSpawnCard.maxSpawnsPerStage = maxSpawnsPerStage;
			interactableSpawnCard.hullSize = HullClassification.Human;
			interactableSpawnCard.prefab = model;
			interactableSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
			interactableSpawnCard.name = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME";

			interactableDirectorCard = new DirectorCard
			{
				selectionWeight = favoredWeight,
				spawnCard = interactableSpawnCard,
				preventOverhead = false,
				minimumStageCompletions = interactableMinimumStageCompletions
			};
			Debug.Log("Created favored spawncard for" + "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME" + "; " + interactableDirectorCard + ", " + interactableSpawnCard);
			return (interactableDirectorCard, interactableSpawnCard);
		}

		public DirectorCard VoidCampAddInteractable(On.RoR2.CampDirector.orig_SelectCard orig, CampDirector self, WeightedSelection<DirectorCard> deck, int maxCost)
		{
			if (this.voidSeedWeight <= 0)
            {
				Debug.LogWarning($"weight was 0; {customInteractable.spawnCard.name}");
				return orig.Invoke(self, deck, maxCost);
            }
			this.hasAddedInteractable = false;
			bool flag = self.name == "Camp 1 - Void Monsters & Interactables";
			if (flag)
			{
				for (int i = deck.Count - 1; i >= 0; i--)
				{
					bool flag2 = deck.GetChoice(i).value.spawnCard.name == this.customInteractable.spawnCard.name;
					if (flag2)
					{
						this.hasAddedInteractable = true;
						break;
					}
				}
				bool flag3 = !this.hasAddedInteractable;
				if (flag3)
				{
					Debug.LogWarning($"added {interactableDirectorCard.spawnCard.name}/{customInteractable.directorCard} to void seed");
					deck.AddChoice(customInteractable.directorCard, this.voidSeedWeight);
				}
			}
			return orig.Invoke(self, deck, maxCost);
		}

		public void AddInteractable(On.RoR2.ClassicStageInfo.orig_RebuildCards orig, ClassicStageInfo self, 
			DirectorCardCategorySelection forcedMonsterCategory = null, DirectorCardCategorySelection forcedInteractableCategory = null)
		{
			orig(self, forcedMonsterCategory, forcedInteractableCategory);
			if (customInteractable.validScenes.ToList().Contains(SceneManager.GetActiveScene().name))
			{
				self.interactableCategories.AddCard((int)category, customInteractable.directorCard);
			}
			if (customInteractable.HasFavoredStages())
            {
				if (customInteractable.favoredScenes.ToList().Contains(SceneManager.GetActiveScene().name))
				{
					self.interactableCategories.AddCard((int)category, customInteractable.directorCardFavored);
				}
			}
		}
	}
	public class CustomInteractable
    {//foe the favored stage stuff this might suck dick idk
		public InteractableSpawnCard spawnCard;
		public DirectorCard directorCard;
		public InteractableSpawnCard spawnCardFavored;
		public DirectorCard directorCardFavored;
		public string[] validScenes;
		public string[] favoredScenes;
		public bool hasFavoredStages = false;
		public ExpansionDef requiredExpansionDef = null;
		public CustomInteractable()
        {

        }
		public CustomInteractable CreateCustomInteractable(InteractableSpawnCard spawnCard, DirectorCard directorCard, string[] validScenes)
        {
			this.spawnCard = spawnCard;
			this.directorCard = directorCard;
			this.validScenes = validScenes;
			return this;
        }
		public CustomInteractable CreateCustomInteractable(InteractableSpawnCard spawnCard, DirectorCard directorCard, string[] validScenes, InteractableSpawnCard spawnCardF, DirectorCard directorCardF, string[] favoredScenes)
		{
			this.spawnCard = spawnCard;
			this.directorCard = directorCard;
			this.validScenes = validScenes;
			this.spawnCardFavored = spawnCardF;
			this.directorCardFavored = directorCardF;
			this.favoredScenes = favoredScenes;
			hasFavoredStages = true;
			return this;
		}
		public bool HasFavoredStages()
        {
			return hasFavoredStages;
        }
	}
}
