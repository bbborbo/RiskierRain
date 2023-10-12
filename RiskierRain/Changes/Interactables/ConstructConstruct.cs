using BepInEx.Configuration;
using RiskierRain.Interactables;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRain;
using static RiskierRain.Secrets;

namespace RiskierRain.Interactables
{
    class ConstructConstruct : InteractableBase<ConstructConstruct>
    {
        public override string interactableName => "Decayed Construct";

        public override string interactableContext => "Kick the Construct";

        public override string interactableLangToken => "CONSTRUCT_CONSTRUCT";

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
            hasAddedInteractable = false;
            On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += ConstructConstructBehavior;
            On.RoR2.CombatDirector.CombatShrineActivation += ConstructShrineActivation;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);
            ConstructConstructSecrets(cards.interactableSpawnCard);
        }

        public static string baseUseMessage = "CONSTRUCT_CONSTRUCT_USE_MESSAGE";

        private void ConstructConstructSecrets(SpawnCard spawncard)
        {
            Vector3[] caveSpots = new Vector3[3];
            caveSpots[0] = new Vector3(23, -35, 65);
            caveSpots[1] = new Vector3(26, -34, 99);
            caveSpots[2] = new Vector3(28, -34, 36);
            SpawnSemiRandom("sulfurpools", spawncard, caveSpots);
            Vector3[] wallSpots = new Vector3[2];
            wallSpots[0] = new Vector3(173, 2, -154);
            wallSpots[1] = new Vector3(128, 0, -194);
            SpawnSemiRandom("sulfurpools", spawncard, wallSpots, 0.5f);

            SpawnSecret("foggyswamp", spawncard, new Vector3(258, -150, -170)); //0.3f?
        }

        private void ConstructConstructBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            
            if (self.displayNameToken != "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
                orig(self, activator);
                return;
            }
            self.gameObject.AddComponent<ConstructDirector>();
            GameObject obj = CombatEncounterHelper.MethodOne(self, activator, 200, 2);//this might be way too much well see :3

            orig(self, activator);
            self.available = false;
        }

        private void ConstructShrineActivation(On.RoR2.CombatDirector.orig_CombatShrineActivation orig, CombatDirector self, Interactor interactor, float monsterCredit, DirectorCard chosenDirectorCard)
        {
            ConstructDirector constructComponent = self.GetComponent<ConstructDirector>();
            if (constructComponent != null)
            {
                self.enabled = true;
                self.monsterCredit += monsterCredit;
                self.OverrideCurrentMonsterCard(DirectorCards.AlphaConstructNear);
                self.monsterSpawnTimer = 0f;
                SpawnCard a = chosenDirectorCard.spawnCard;
                if (a == null)
                {
                }
                GameObject b = a.prefab;
                if (b == null)
                {
                }
                CharacterMaster component = b.GetComponent<CharacterMaster>();
                if (component == null)
                {
                    return;
                }
                CharacterBody component2 = component.bodyPrefab.GetComponent<CharacterBody>();
                if (component2)
                {
                    Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                    {
                        subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
                        baseToken = baseUseMessage,
                        paramTokens = new string[]
                        {
                            component2.baseNameToken
                        }
                    });
                }
                return;
            }
            orig(self, interactor, monsterCredit, chosenDirectorCard);
        }
    }
}
