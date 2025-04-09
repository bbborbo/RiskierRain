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
		public GameObject interactablePrefab;

		public CustomInteractable customInteractable = new CustomInteractable();
		public abstract DirectorAPI.InteractableCategory category { get; }

		public PurchaseInteraction InteractionComponent;

		public abstract CostTypeIndex costTypeIndex { get; }
		public abstract float voidSeedWeight { get; }
		public abstract int normalWeight { get; }
		public abstract int favoredWeight { get; }
		public abstract int spawnCost { get; }
		public abstract int interactionCost { get; }
		public abstract string[] validScenes { get; }
		public abstract string[] favoredScenes { get; }
		public static InteractableSpawnCard interactableSpawnCard;
		public static DirectorCard interactableDirectorCard;
		public abstract SimpleInteractableData InteractableData { get; }
        public override void Init()
        {
            base.Init();
			CreateInteractablePrefab();
			CreateInteractable();
		}

        private void CreateInteractable()
		{
			var cards = CreateInteractableSpawnCard();
			if (favoredScenes == null || favoredScenes.Length <= 0)
			{
				customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
			}
			else
			{
				var favored = CreateInteractableSpawnCard(true);
				customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes, favored.interactableSpawnCard, favored.directorCard, favoredScenes);
			}
		}

        public override void Lang()
		{
			LanguageAPI.Add("2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME", this.InteractableName);
			LanguageAPI.Add("2R4R_INTERACTABLE_" + this.InteractableLangToken + "_CONTEXT", this.InteractableContext);
		}
		public abstract UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction);
		public virtual Sprite GetPingIcon()
        {
			return Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texShrineIconOutlined.png").WaitForCompletion();

		}
		public void CreateInteractablePrefab()
        {
			if (InteractableModel == null)
			{
				Debug.Log("interactableModel null :(");
				return;
			}
			if (!ShouldCloneModel)
            {
				interactablePrefab = InteractableModel;
            }
            else
            {
				interactablePrefab = InteractableModel.InstantiateClone(prefabName, true); 
            }
			interactablePrefab.AddComponent<NetworkIdentity>();
			PurchaseInteraction oldPurchaseInteraction = interactablePrefab.GetComponent<PurchaseInteraction>();
			if (oldPurchaseInteraction != null)
            {
				GameObject.Destroy(oldPurchaseInteraction);
			}
			InteractionComponent = interactablePrefab.AddComponent<PurchaseInteraction>();

			InteractionComponent.displayNameToken = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME";
			InteractionComponent.contextToken = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_CONTEXT";
			InteractionComponent.costType = (CostTypeIndex)costTypeIndex;
			InteractionComponent.automaticallyScaleCostWithDifficulty = InteractableData.automaticallyScaleCostWithDifficulty;
			InteractionComponent.cost = interactionCost;
			InteractionComponent.available = true;
			InteractionComponent.SetAvailable(true);
			InteractionComponent.setUnavailableOnTeleporterActivated = InteractableData.unavailableDuringTeleporter;
			InteractionComponent.isShrine = InteractableData.isShrine;
			InteractionComponent.isGoldShrine = false;
			InteractionComponent.onPurchase = new PurchaseEvent();
			InteractionComponent.saleStarCompatible = InteractableData.saleStarCompatible;
			UnityAction<Interactor> onPurchaseAction = GetInteractionAction(InteractionComponent);
			if(onPurchaseAction != null)
			{
				Debug.Log("adding purchase action for " + InteractableName);
				InteractionComponent.onPurchase.AddListener(onPurchaseAction);
			}

			PingInfoProvider pingInfoProvider = interactablePrefab.GetComponent<PingInfoProvider>();
			if (pingInfoProvider == null)
			{
				pingInfoProvider = interactablePrefab.AddComponent<PingInfoProvider>();
				pingInfoProvider.pingIconOverride = GetPingIcon();
				
			}

			GenericDisplayNameProvider genericDisplayNameProvider = interactablePrefab.GetComponent<GenericDisplayNameProvider>();
			if (genericDisplayNameProvider == null)
			{
				genericDisplayNameProvider = interactablePrefab.AddComponent<GenericDisplayNameProvider>();
			}
			genericDisplayNameProvider.displayToken = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME";

			if (normalWeight > 0 || favoredWeight > 0)
			{
				On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
			}
			if(voidSeedWeight > 0)
			{
				On.RoR2.CampDirector.SelectCard += VoidCampAddInteractable;
			}


			Collider childCollider = interactablePrefab.GetComponentInChildren<Collider>();
			if (childCollider == null)
            {
				Debug.Log("child null");
				return;
            }
			EntityLocator entityLocator = interactablePrefab.GetComponent<EntityLocator>();
			if (entityLocator == null)
			{
				Debug.Log("entitylocator null, adding component");
				entityLocator = interactablePrefab.AddComponent<EntityLocator>();
			}
			if (entityLocator != null)
			{
				entityLocator.entity = interactablePrefab;
				ModelLocator modelLocator = interactablePrefab.GetComponent<ModelLocator>();
				if (modelLocator == null)
                {
					Debug.Log("modellocator null, adding component");
					modelLocator = interactablePrefab.AddComponent<ModelLocator>();
					modelLocator.modelTransform = interactablePrefab.transform.Find(modelName);//pawsible problem area? ()
					modelLocator.modelBaseTransform = modelLocator.modelTransform;
					modelLocator.dontDetatchFromParent = true;
					modelLocator.autoUpdateModelTransform = true;

					Highlight component = interactablePrefab.GetComponent<Highlight>();
					if (component == null)
					{
						Debug.Log("highlight null, adding component");
						component = interactablePrefab.AddComponent<Highlight>();
					}
					if (component != null)
					{

						component.targetRenderer = (from x in interactablePrefab.GetComponentsInChildren<MeshRenderer>()
													where x.gameObject.name.Contains(modelName)
													select x).First<MeshRenderer>();

						component.strength = 1f;
						component.highlightColor = Highlight.HighlightColor.interactive;
					}
					HologramProjector hologramProjector = interactablePrefab.GetComponent<HologramProjector>();
					if (hologramProjector == null)
					{
						Debug.Log("hologramProjector null, adding component");
						hologramProjector = interactablePrefab.AddComponent<HologramProjector>();
					}
					if (hologramProjector != null)
					{
						hologramProjector.hologramPivot = interactablePrefab.transform.Find("HologramPivot"); // this might be fucky
						hologramProjector.displayDistance = 10f;
						hologramProjector.disableHologramRotation = false;
					}
					ChildLocator childLocator = interactablePrefab.GetComponent<ChildLocator>();

					if (childLocator == null)
					{
						Debug.Log("childLocator null, adding component");
						childLocator = interactablePrefab.AddComponent<ChildLocator>();
					}
					if (childLocator != null)
					{
						childLocator.transformPairs = new ChildLocator.NameTransformPair[]
						{
							new ChildLocator.NameTransformPair
							{
								name = "FireworkOrigin",
								transform = interactablePrefab.transform.Find("FireworkEmitter")
							}
						};
						Debug.Log("interactable registered");
					}
				}
			}
			PrefabAPI.RegisterNetworkPrefab(interactablePrefab);
		}
		public (DirectorCard directorCard, InteractableSpawnCard interactableSpawnCard)  CreateInteractableSpawnCard(bool isFavored = false)
        {
			interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
			interactableSpawnCard.directorCreditCost = spawnCost;
			interactableSpawnCard.eliteRules = SpawnCard.EliteRules.Default;
			interactableSpawnCard.sendOverNetwork = true;
			interactableSpawnCard.occupyPosition = true;
			interactableSpawnCard.orientToFloor = InteractableData.orientToFloor;
			interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = InteractableData.sacrificeWeightScalar <= 0;
			interactableSpawnCard.weightScalarWhenSacrificeArtifactEnabled = InteractableData.sacrificeWeightScalar;
			interactableSpawnCard.maxSpawnsPerStage = InteractableData.maxSpawnsPerStage;
			interactableSpawnCard.hullSize = HullClassification.Human;
			interactableSpawnCard.prefab = interactablePrefab;
			interactableSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
			interactableSpawnCard.name = "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME";

			interactableDirectorCard = new DirectorCard
			{
				selectionWeight = isFavored ? favoredWeight : normalWeight,
				spawnCard = interactableSpawnCard,
				preventOverhead = false,
				minimumStageCompletions = InteractableData.minimumStageCompletions
			};
			Debug.Log("Created spawncard for " + "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME" + "; " + interactableDirectorCard.spawnCard.name + ", " + interactableSpawnCard.name);

			return (interactableDirectorCard, interactableSpawnCard);
		}

		public DirectorCard VoidCampAddInteractable(On.RoR2.CampDirector.orig_SelectCard orig, CampDirector self, WeightedSelection<DirectorCard> deck, int maxCost)
		{
			if (this.voidSeedWeight <= 0)
            {
				Debug.LogWarning($"weight was 0; {customInteractable.spawnCard.name}");
				return orig(self, deck, maxCost);
            }
			bool hasAddedInteractable = false;
			if (self.name == "Camp 1 - Void Monsters & Interactables")
			{
				for (int i = deck.Count - 1; i >= 0; i--)
				{
					if (deck.GetChoice(i).value.spawnCard.name == this.customInteractable.spawnCard.name)
					{
						hasAddedInteractable = true;
						break;
					}
				}
				if (!hasAddedInteractable)
				{
					Debug.LogWarning($"added {interactableDirectorCard.spawnCard.name}/{customInteractable.directorCard} to void seed");
					deck.AddChoice(customInteractable.directorCard, this.voidSeedWeight);
				}
			}
			return orig(self, deck, maxCost);
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
	public class SimpleInteractableData
	{
		public SimpleInteractableData(bool addToHackBlacklist = false, bool automaticallyScaleCostWithDifficulty = false, 
			bool unavailableDuringTeleporter = true, bool isShrine = false, bool orientToFloor = true, bool saleStarCompatible = false,
			int minimumStageCompletions = 0, int sacrificeWeightScalar = 1, int maxSpawnsPerStage = -1)
		{
			this.addToHackBlacklist = addToHackBlacklist;
			this.automaticallyScaleCostWithDifficulty = automaticallyScaleCostWithDifficulty;
			this.unavailableDuringTeleporter = unavailableDuringTeleporter;
			this.isShrine = isShrine;
			this.orientToFloor = orientToFloor;
			this.saleStarCompatible = saleStarCompatible;
			this.minimumStageCompletions = minimumStageCompletions;
			this.sacrificeWeightScalar = sacrificeWeightScalar;
			this.maxSpawnsPerStage = maxSpawnsPerStage;
		}

		internal bool	addToHackBlacklist;
		internal bool	automaticallyScaleCostWithDifficulty;
		internal bool	unavailableDuringTeleporter;
		internal bool	isShrine;
		internal bool	orientToFloor;
		internal bool	saleStarCompatible;
		/// <summary>
		/// Set to 0 to disable this interactable with sacrifice
		/// </summary>
		internal float sacrificeWeightScalar;
		/// <summary>
		/// Set to 0 to spawn on any stage
		/// </summary>
		internal int minimumStageCompletions;
		internal int maxSpawnsPerStage;
	}
	public class CustomInteractable
    {
		//foe the favored stage stuff this might suck dick idk
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
