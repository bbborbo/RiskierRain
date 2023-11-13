using BepInEx.Configuration;
using static RiskierRainContent.CoreModules.CoreModule;
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

namespace RiskierRainContent.Interactables
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

		//janky solutions inbound!!! dorry :(
		public abstract string modelName { get; }
		public abstract string prefabName { get; }//???? fuck
		public abstract bool modelIsCloned { get; }
		public GameObject model;

		public CustomInteractable customInteractable = new CustomInteractable();
		public abstract void Init(ConfigFile config);
		protected void CreateLang()
		{
			LanguageAPI.Add("2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME", this.interactableName);
			LanguageAPI.Add("2R4R_INTERACTABLE_" + this.interactableLangToken + "_CONTEXT", this.interactableContext);
		}

		public void CreateInteractable()
        {
			if (interactableModel == null)
			{
				Debug.Log("interactableModel null :(");
				return;
			}
			bool hajabaja = modelName == prefabName;
			if (!modelIsCloned)
            {
				model = interactableModel;
            }
            else
            {
				model = interactableModel.InstantiateClone("model", true); 
            }
			interactableBodyModelPrefab = this.model;
			interactableBodyModelPrefab.AddComponent<NetworkIdentity>();
			PurchaseInteraction oldPurchaseInteraction = interactableBodyModelPrefab.GetComponent<PurchaseInteraction>();
			if (oldPurchaseInteraction != null)
            {
				GameObject.Destroy(oldPurchaseInteraction);
            }
			PurchaseInteraction purchaseInteraction = interactableBodyModelPrefab.AddComponent<PurchaseInteraction>();

			purchaseInteraction.displayNameToken = "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME";
			purchaseInteraction.contextToken = "2R4R_INTERACTABLE_" + this.interactableLangToken + "_CONTEXT";
			purchaseInteraction.costType = (CostTypeIndex)costTypeIndex;
			purchaseInteraction.automaticallyScaleCostWithDifficulty = automaticallyScaleCostWithDifficulty;
			purchaseInteraction.cost = costAmount;
			purchaseInteraction.available = true;
			purchaseInteraction.setUnavailableOnTeleporterActivated = setUnavailableOnTeleporterActivated;
			purchaseInteraction.isShrine = isShrine;
			purchaseInteraction.isGoldShrine = false;

			PingInfoProvider pingInfoProvider = interactableBodyModelPrefab.AddComponent<PingInfoProvider>();
			pingInfoProvider.pingIconOverride = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texShrineIconOutlined.png").WaitForCompletion(); //only works for shrines? change later i guess
			GenericDisplayNameProvider genericDisplayNameProvider = interactableBodyModelPrefab.AddComponent<GenericDisplayNameProvider>();
			genericDisplayNameProvider.displayToken = "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME";
			Collider childCollider = interactableBodyModelPrefab.GetComponentInChildren<Collider>();

			if (childCollider == null)
            {
				Debug.Log("child null");
				return;
            }
			GameObject childGameObject = childCollider.gameObject;
			if (childGameObject == null)
            {
				Debug.Log("childobject null");
				return;
            }
			EntityLocator entityLocator = childGameObject.GetComponent<EntityLocator>();
			if (entityLocator == null)
			{
				Debug.Log("entitylocator null, adding component");
				entityLocator = childGameObject.AddComponent<EntityLocator>();
			}
			if (entityLocator != null)
			{
				entityLocator.entity = interactableBodyModelPrefab;
				ModelLocator modelLocator = interactableBodyModelPrefab.GetComponent<ModelLocator>();
				if (modelLocator == null)
                {
					Debug.Log("modellocator null, adding component");
					modelLocator = interactableBodyModelPrefab.AddComponent<ModelLocator>();
                }
				if (modelLocator != null)
                {
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
						PrefabAPI.RegisterNetworkPrefab(interactableBodyModelPrefab);
						Debug.Log("interactable registered");
					}								
				}							
			}
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
			interactableSpawnCard.name = "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME";

			interactableDirectorCard = new DirectorCard
			{
				selectionWeight = normalWeight,
				spawnCard = interactableSpawnCard,
				preventOverhead = false,
				minimumStageCompletions = interactableMinimumStageCompletions
			};
			Debug.Log("Created spawncard for " + "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME" + "; " + interactableDirectorCard + ", " + interactableSpawnCard);
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
			interactableSpawnCard.name = "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME";

			interactableDirectorCard = new DirectorCard
			{
				selectionWeight = favoredWeight,
				spawnCard = interactableSpawnCard,
				preventOverhead = false,
				minimumStageCompletions = interactableMinimumStageCompletions
			};
			Debug.Log("Created favored spawncard for" + "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME" + "; " + interactableDirectorCard + ", " + interactableSpawnCard);
			return (interactableDirectorCard, interactableSpawnCard);
		}


		public CharacterBody LastActivator;
		public PurchaseInteraction PurchaseInteraction;

		public abstract float voidSeedWeight { get; }
		public abstract int normalWeight { get; }
		public abstract int favoredWeight { get; }
		public abstract DirectorAPI.InteractableCategory category { get; }
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
			string result;
			if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME" )
			{
				result = this.interactableName;
			}
			else
			{
				Debug.Log("uh oh");
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
					deck.AddChoice(interactableDirectorCard, this.voidSeedWeight);
				}
			}
			return orig.Invoke(self, deck, maxCost);
		}

		public void AddInteractable(On.RoR2.ClassicStageInfo.orig_RebuildCards orig, ClassicStageInfo self)
		{
			orig(self);
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
