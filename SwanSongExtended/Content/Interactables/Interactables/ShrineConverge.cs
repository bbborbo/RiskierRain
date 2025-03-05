using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace SwanSongExtended.Interactables
{
    class ShrineConverge : InteractableBase<ShrineConverge>
    {
        public override string ConfigName => "Shrine of Convergence";
        public static string ShrineConvergeUseToken = "SHRINE_CONVERGE_USE_MESSAGE";
        public static string ShrineConvergeBeginToken = "SHRINE_CONVERGE_BEGIN_MESSAGE";

        public string ShrineConvergeUseMessage = "You have invited the challenge of Convergence..";
        public string ShrineConvergeUseMessage2P = "{0} has invited the challenge of Convergence..";
        public string ShrineConvergeBeginMessage = "Let the challenge of Convergence... begin!";

        public override string InteractableName => "Shrine of Convergence";

        public override string InteractableContext => "Pray to the Shrine of Convergence";

        public override string InteractableLangToken => "SHRINE_CONVERGE";

        public override GameObject InteractableModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineBoss/ShrineBoss.prefab").WaitForCompletion();

        public override string modelName => "mdlShrineBoss";

        public override string prefabName => "ShrineConvergence";

        public override bool ShouldCloneModel => true;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 12;

        public override int favoredWeight => 300000;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Shrines;

        public override int spawnCost => 15;

        public override CostTypeIndex costTypeIndex => CostTypeIndex.None;

        public override int costAmount => 0;

        public override int interactableMinimumStageCompletions => 3;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => true;

        public override bool isShrine => true;

        public override bool orientToFloor => true;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 1;

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
            "skymeadow",
            "rootjungle",
            "ancientloft",
            "sulfurpools",
            "lakes",
            "lakesnight",
            "village",
            "villagenight",
            "lemuriantemple",
            "habitat",
            "habitatfall",
            "helminthroost",
			//modded stages
			"slumberingsatellite",
            "forgottenhaven",
            "drybasin",
            "FBLScene"
        };
        public string[] favoredStages =
        {
            "lakes",
            "lakesnight",
            "goolake",
            "dampcavesimple",
            "skymeadow",
            "lemuriantemple",
            "habitat",
            "habitatfall"
        };

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            ShrineBossBehavior sbb = interaction.gameObject.GetComponent<ShrineBossBehavior>();
            if (sbb)
                GameObject.Destroy(sbb);
            ShrineConvergeBehavior scb = interaction.gameObject.AddComponent<ShrineConvergeBehavior>();
            scb.purchaseInteraction = interaction;
            return null;// scb.AddShrineStack;
        }

        public override void Init(ConfigFile config)
        {
            var cards = CreateInteractableSpawnCard();
            var favored = CreateInteractableSpawnCard(true);
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes, favored.interactableSpawnCard, favored.directorCard, favoredStages);
            InteractionComponent.currentInspectIndex = 0;
        }
        public override void Init()
        {
            LanguageAPI.Add(ShrineConvergeUseToken, ShrineConvergeUseMessage);
            LanguageAPI.Add(ShrineConvergeUseToken + "_2P", ShrineConvergeUseMessage2P);
            LanguageAPI.Add(ShrineConvergeBeginToken, ShrineConvergeBeginMessage);
            base.Init();
        }

        public override void Hooks()
        {
            throw new NotImplementedException();
        }
    }
    public class ShrineConvergeBehavior : ShrineBehavior
    {
        public PurchaseInteraction purchaseInteraction;
        bool purchased = false;
        Transform symbolTransform;
        void Start()
        {
            if (purchaseInteraction)
            {
                //purchaseInteraction.onPurchase = new PurchaseEvent();
                purchaseInteraction.onPurchase.AddListener(AddShrineStack);
            }
            Debug.Log("Shrine converge behavior");
            if(symbolTransform == null)
            {
                symbolTransform = this.transform.Find("Symbol");
            }
        }

        public void AddShrineStack(Interactor activator)
        {
            if (purchased)
                return;
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineBossBehavior::AddShrineStack(RoR2.Interactor)' called on client");
                return;
            }
            purchased = true;

            if (TeleporterInteraction.instance && TeleporterInteraction.instance.activationState <= TeleporterInteraction.ActivationState.IdleToCharging)
            {
                //TeleporterInteraction.instance.AddShrineStack();
                TeleporterInteraction.instance.gameObject.AddComponent<ConvergenceShrinkController>();
                BossGroup bossGroup = TeleporterInteraction.instance.bossGroup;
                bossGroup.bonusRewardCount += 2;
            }

            CharacterBody component = activator.GetComponent<CharacterBody>();
            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = component,
                baseToken = ShrineConverge.ShrineConvergeUseToken
            });

            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
            {
                origin = base.transform.position,
                rotation = Quaternion.identity,
                scale = 1f,
                color = new Color(0.94509804f, 0.90588236f, 0.7372549f)
            }, true);

            if (true)
            {
                if (this.symbolTransform)
                {
                    this.symbolTransform.gameObject.SetActive(false);
                }
                this.CallRpcSetPingable(false);
                purchaseInteraction.SetAvailable(false);
            }
        }
    }
    public class ConvergenceShrinkController : MonoBehaviour
    {
        bool hasSentMessage = false;
		private void Awake()
		{
			this.holdoutZoneController = base.GetComponent<HoldoutZoneController>();
		}

		private void OnEnable()
		{
			this.holdoutZoneController.calcRadius += this.ApplyRadius;
			//this.holdoutZoneController.calcColor += this.ApplyColor;
		}

		private void OnDisable()
		{
			//this.holdoutZoneController.calcColor -= this.ApplyColor;
			this.holdoutZoneController.calcRadius -= this.ApplyRadius;
		}

		private void ApplyRadius(ref float radius)
		{
            if (!hasSentMessage)
            {
                this.enabledTime = Run.FixedTimeStamp.now;
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = ShrineConverge.ShrineConvergeBeginToken
                });
                hasSentMessage = true;
            }
			radius /= convergenceRadiusDivisor;
		}

		private void ApplyColor(ref Color color)
		{
			color = Color.Lerp(color, convergenceMaterialColor, HoldoutZoneController.FocusConvergenceController.colorCurve.Evaluate(this.currentValue));
		}

		private void Update()
		{
			this.DoUpdate(Time.deltaTime);
		}

		private void DoUpdate(float deltaTime)
		{
            if (this.holdoutZoneController.enabled == false)
                return;
            float target = (this.enabledTime.timeSince < HoldoutZoneController.FocusConvergenceController.startupDelay) ? 1f : 0f;
			float num = Mathf.MoveTowards(this.currentValue, target, HoldoutZoneController.FocusConvergenceController.rampUpTime * deltaTime);
			if (this.currentValue <= 0f && num > 0f)
			{
				Util.PlaySound("Play_item_lunar_focusedConvergence", base.gameObject);
			}
			this.currentValue = num;
		}

		private static readonly float convergenceRadiusDivisor = 2f;

		private static readonly Color convergenceMaterialColor = new Color(0f, 3.9411764f, 5f, 1f);

		private float currentValue;

		private HoldoutZoneController holdoutZoneController;

		private Run.FixedTimeStamp enabledTime;
	}
}
