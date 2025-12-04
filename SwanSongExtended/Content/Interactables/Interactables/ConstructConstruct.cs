using BepInEx.Configuration;
using RiskierRainContent.Interactables;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRainContent;
using static SwanSongExtended.Secrets;
using R2API;
using SwanSongExtended.Modules;
using UnityEngine.Events;
using static RoR2.CombatDirector;
using SwanSongExtended.Components;
using SwanSongExtended.Items;

namespace SwanSongExtended.Interactables
{
    class ConstructConstruct : InteractableBase<ConstructConstruct>
    {
        public override string InteractableName => "Decayed Construct";

        public override string InteractableContext => "Kick the Construct";

        public override string InteractableLangToken => "CONSTRUCTCONSTRUCT";

        public override GameObject InteractableModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlConstructConstruct.prefab");

        public override string modelName => "mdlConstructConstruct";

        public override string prefabName => "constructConstruct";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 5;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Shrines;

        public override int spawnCost => 15;

        public override CostTypeIndex costTypeIndex => CostTypeIndex.None;

        public override int interactionCost => 0;
        public override SimpleInteractableData InteractableData => new SimpleInteractableData
            (
                minimumStageCompletions: 1,
                isShrine: true, 
                orientToFloor: false,
                sacrificeWeightScalar: 3,
                maxSpawnsPerStage: 3
            );
        public override string[] validScenes => new string[]
        {
            "foggyswamp",
            "dampcavesimple",
            "sulfurpools",
			//modded stages
            "drybasin"
        };
        public override string[] favoredScenes => null;
        public override void Init()
        {
            base.Init();
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            CombatSquad cs = interaction.gameObject.AddComponent<CombatSquad>();
            CombatDirector cd = interaction.gameObject.AddComponent<CombatDirector>();
            cd.expRewardCoefficient = 1f;
            cd.goldRewardCoefficient = 1f;
            cd.eliteBias = 2; // 
            cd.maximumNumberToSpawnBeforeSkipping = 6;
            cd.teamIndex = TeamIndex.Monster;
            cd.fallBackToStageMonsterCards = false;
            cd.onSpawnedServer = new OnSpawnedServer();
            cd.onSpawnedServer.AddPersistentListener(OnGalleryDirectorSpawnServer);
            cd.combatSquad = cs;
            ConstructCombatShrineBehavior ccsb = interaction.gameObject.AddComponent<ConstructCombatShrineBehavior>();
            ccsb.baseMonsterCredit = 200; // quote orange, "//this might be way too much well see :3"
            ccsb.maxPurchaseCount = 1;
            ccsb.monsterCreditCoefficientPerPurchase = 2;

            GameObject symbolTransform = new GameObject();
            symbolTransform.transform.parent = interaction.transform;
            ccsb.symbolTransform = symbolTransform.transform;

            return ccsb.OnInteractionBegin;

            void OnGalleryDirectorSpawnServer(GameObject masterObject)
            {
            }
        }
        private ExplicitPickupDropTable GenerateWeightedSelection()
        {
            ExplicitPickupDropTable dropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();

            List<ExplicitPickupDropTable.PickupDefEntry> pickupDefEntries = new List<ExplicitPickupDropTable.PickupDefEntry>();
            pickupDefEntries.Add(
                    new ExplicitPickupDropTable.PickupDefEntry
                    {
                        pickupDef = Wishbone.instance.ItemsDef,
                        pickupWeight = 1f
                    }
                );
            dropTable.pickupEntries = pickupDefEntries.ToArray();

            return dropTable;
        }

        public override void Hooks()
        {

        }

        public class ConstructCombatShrineBehavior : ShrineCombatBehavior
        {
            void OnEnable()
            {
               //PurchaseInteraction interaction = GetComponent<PurchaseInteraction>();
               //interaction.onPurchase.AddListener(OnInteractionBegin);
            }
            public void OnInteractionBegin(Interactor activator)
            {
                if (purchaseCount >= maxPurchaseCount)
                    return;
                chosenDirectorCard = DirectorCards.AlphaConstructNear;
                AddShrineStack(activator);
                purchaseInteraction.SetAvailable(false);
            }
        }
    }
}
