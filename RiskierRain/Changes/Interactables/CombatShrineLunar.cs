using BepInEx.Configuration;
using R2API;
using RiskierRain.Interactables;
using RiskierRain.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskierRain.Interactables
{
    class CombatShrineLunar : InteractableBase<CombatShrineLunar>
    {
        public static string baseUseMessage = "SHRINE_LUNARGALLERY_USE_MESSAGE";
        public override string interactableName => "Lunar Gallery";

        public override string interactableContext => "eat my transgendence nerd";

        public override string interactableLangToken => "LUNAR_GALLERY";

        public override GameObject interactableModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlLunarGallery.prefab");

        public override bool modelIsCloned => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 200;

        public override int spawnCost => 20;

        public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.None);

        public override int costTypeIndex => 0;

        public override int costAmount => 0;

        public override int interactableMinimumStageCompletions => 0;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => true;

        public override bool orientToFloor => false;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 5;

        public override string modelName => "mdlLunarGallery"; 

        public override string prefabName => "mdlLunarGallery";

        public override int category => 4;

        public override int favoredWeight => 600;

        public string[] validScenes = {
            "golemplains",
            "golemplains2",
            //"blackbeach",
            //"blackbeach2",
            "snowyforest",
            "foggyswamp",
            "goolake",
            //"frozenwall",
            "wispgraveyard",
            "dampcavesimple",
            "shipgraveyard",
            "arena",
            //"skymeadow",
            "artifactworld",
            //"rootjungle",
            "ancientloft",
            "sulfurpools",
			//modded stages
			"slumberingsatellite",
            "forgottenhaven",
            "drybasin"
            //"FBLScene"
        };
        public string[] favoredStages =
        {
            "blackbeach",
            "blackbeach2",
            "frozenwall",
            "rootjungle",
            "skymeadow",
            //modded stages
            "FBLScene"
        };

        public override void Init(ConfigFile config)
        {
            hasAddedInteractable = false;
            On.RoR2.PurchaseInteraction.GetDisplayName += new On.RoR2.PurchaseInteraction.hook_GetDisplayName(InteractableName);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += LunarGalleryBehavior;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            On.RoR2.DirectorCore.TrySpawnObject += StoreEnemyAsVariable;
            On.RoR2.CombatDirector.Spawn += GiveEnemyItem;
            On.RoR2.CombatDirector.CombatShrineActivation += GalleryShrineActivation;
            LanguageAPI.Add(baseUseMessage, "<style=cShrine>You have summoned {1}s to fight <style=cIsLunar>with {2}</style>.</color>");
            LanguageAPI.Add(baseUseMessage + "_2P", "<style=cShrine>{0} has summoned {1}s to fight <style=cIsLunar>with {2}</style>.</color>");
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            var favored = CreateInteractableSpawnCard(true);
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes, favored.interactableSpawnCard, favored.directorCard, favoredStages);
            ConstructItemPool();
        }

        private void GalleryShrineActivation(On.RoR2.CombatDirector.orig_CombatShrineActivation orig, CombatDirector self, Interactor interactor, float monsterCredit, DirectorCard chosenDirectorCard)
        {
            Debug.Log("GSA1");
            RiskierRainCombatDirector galleryComponent = self.GetComponent<RiskierRainCombatDirector>();
            if(galleryComponent != null)
            {
                Debug.Log("GSA2");
                self.enabled = true;
                self.monsterCredit += monsterCredit;
                self.OverrideCurrentMonsterCard(chosenDirectorCard);
                self.monsterSpawnTimer = 0f;
                Debug.Log("GSA3");
                SpawnCard a = chosenDirectorCard.spawnCard;
                if (a == null)
                {
                    Debug.Log("null1");
                }
                GameObject b = a.prefab;
                if (b == null)
                {
                    Debug.Log("null2");
                }
                CharacterMaster component = b.GetComponent<CharacterMaster>();
                Debug.Log("GSA4");
                if (component == null)
                {
                    Debug.Log("null3");
                    return;
                }
                CharacterBody component2 = component.bodyPrefab.GetComponent<CharacterBody>();
                Debug.Log("GSA5");
                if (component2)
                {
                    Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                    {
                        subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
                        baseToken = baseUseMessage,
                        paramTokens = new string[]
                        {
                            component2.baseNameToken,
                            itemToGive.nameToken
                        }
                    });
                    Debug.Log("GSA6");
                }
                
                return;
            }
            orig(self, interactor, monsterCredit, chosenDirectorCard);
        }

        private bool GiveEnemyItem(On.RoR2.CombatDirector.orig_Spawn orig, CombatDirector self, SpawnCard spawnCard, EliteDef eliteDef, Transform spawnTarget, DirectorCore.MonsterSpawnDistance spawnDistance, bool preventOverhead, float valueMultiplier, DirectorPlacementRule.PlacementMode placementMode)
        {
            bool value = orig(self, spawnCard, eliteDef, spawnTarget, spawnDistance, preventOverhead, valueMultiplier, placementMode);
            if (value)
            {
                Inventory inv = enemySpawned.GetComponent<Inventory>();
                RiskierRainCombatDirector isGallery = self.gameObject.GetComponent<RiskierRainCombatDirector>();
                if (inv != null && isGallery != null)
                {
                    inv.GiveItem(itemToGive);
                    inv.GiveItem(Items.Helpers.GalleryItemDrop.instance.ItemsDef);
                }
            }
            return value;
        }

        private GameObject StoreEnemyAsVariable(On.RoR2.DirectorCore.orig_TrySpawnObject orig, DirectorCore self, DirectorSpawnRequest directorSpawnRequest)
        {
            enemySpawned = orig(self, directorSpawnRequest);            
            return enemySpawned;
        }

        private void LunarGalleryBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.displayNameToken != "2R4R_INTERACTABLE_" + this.interactableLangToken + "_NAME")
            {
                orig(self, activator);
                return;
            }
            self.gameObject.AddComponent<RiskierRainCombatDirector>();
            ChooseItem();
            GameObject obj = CombatEncounterHelper.MethodOne(self, activator);
            orig(self, activator);
            
            //Vector3 vector = Vector3.zero;
            //Quaternion rotation = Quaternion.identity;
            //Transform transform = self.gameObject.transform;
            //if (transform)
            //{
            //    vector = transform.position;
            //    rotation = transform.rotation;
            //}   
            //
            //GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Encounters/MonstersOnShrineUseEncounter");
            //
            //if (gameObject == null)
            //{
            //    return;
            //}
            //
            //GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, vector, Quaternion.identity);
            //NetworkServer.Spawn(gameObject2);
            //CombatDirector component6 = gameObject2.GetComponent<CombatDirector>();
            //component6.gameObject.AddComponent<GalleryDirector>();
            //
            //if (!(component6 && Stage.instance))
            //{
            //    return;
            //}
            //
            //float monsterCredit = 40f * Stage.instance.entryDifficultyCoefficient;
            //DirectorCard directorCard = component6.SelectMonsterCardForCombatShrine(monsterCredit);
            //
            //if (directorCard != null)
            //{
            //    ChooseItem();
            //    component6.CombatShrineActivation(activator, monsterCredit, directorCard);
            //    EffectData effectData = new EffectData
            //    {
            //        origin = vector,
            //        rotation = rotation
            //    };
            //
            //    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MonstersOnShrineUse"), effectData, true);
            //    return;
            //}
            //NetworkServer.Destroy(gameObject2);
        }
        GameObject enemySpawned;
        ItemDef[] itemPool = new ItemDef[10];
        ItemDef itemToGive;

        void ConstructItemPool()//probably this sucks fix later
        {
            itemPool[0] = LunarIncreaseCD.instance.ItemsDef;
            itemPool[1] = StarVeil.instance.ItemsDef;//busted
            itemPool[2] = RoR2Content.Items.LunarPrimaryReplacement;//pretty busted
            itemPool[3] = RoR2Content.Items.LunarSecondaryReplacement;//untested
            itemPool[4] = RoR2Content.Items.RandomDamageZone;
            itemPool[5] = RoR2Content.Items.LunarBadLuck;
            itemPool[6] = RoR2Content.Items.LunarDagger;
            itemPool[7] = DLC1Content.Items.HalfAttackSpeedHalfCooldowns;
            itemPool[8] = DLC1Content.Items.HalfSpeedDoubleHealth;
            itemPool[9] = DLC1Content.Items.LunarSun;//UNTESTED LMAOOOOOO
        }
        public void ChooseItem()
        {
            int i = UnityEngine.Random.RandomRangeInt(0, itemPool.Length - 1);
            itemToGive = itemPool[i];
        }
    }

}
