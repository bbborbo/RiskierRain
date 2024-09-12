using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Interactables;
using RiskierRainContent.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskierRainContent.Interactables
{
    class CombatShrineLunar : InteractableBase<CombatShrineLunar>
    {
        public static string baseUseMessage = "SHRINE_LUNARGALLERY_USE_MESSAGE";
        public override string interactableName => "Lunar Gallery";

        public override string interactableContext => "eat my transgendence nerd";

        public override string interactableLangToken => "LUNAR_GALLERY";

        public override GameObject interactableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/lunarGallery.prefab");

        public override bool modelIsCloned => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 10;

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

        public override string prefabName => "lunarGallery";

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Shrines;

        public override int favoredWeight => 25;

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
            On.RoR2.Run.BuildDropTable += GalleryItemPool;
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.GoldOnHit));
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.RepeatHeal));
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.AutoCastEquipment));
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.LunarPrimaryReplacement));
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.LunarUtilityReplacement));
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.LunarSpecialReplacement));
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.LunarTrinket));
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.FocusConvergence));
            RiskierRainContent.AIBlacklistSingleItem(nameof(RoR2Content.Items.MonstersOnShrineUse));
            RiskierRainContent.AIBlacklistSingleItem(nameof(DLC1Content.Items.LunarSun));
            RiskierRainContent.AIBlacklistSingleItem(nameof(DLC1Content.Items.RandomlyLunar));
        }

        private void GalleryItemPool(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);
            List<ItemIndex> list = new List<ItemIndex>();
            ItemIndex itemIndex = (ItemIndex)0;
            ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount;
            while (itemIndex < itemCount)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (itemDef.tier == ItemTier.Lunar && !itemDef.ContainsTag(ItemTag.AIBlacklist))
                {
                    list.Add(itemIndex);
                }
                itemIndex++;
            }
            itemPool = list.ToArray();
        }

        private void GalleryShrineActivation(On.RoR2.CombatDirector.orig_CombatShrineActivation orig, CombatDirector self, Interactor interactor, float monsterCredit, DirectorCard chosenDirectorCard)
        {
            GalleryDirector galleryComponent = self.GetComponent<GalleryDirector>();
            if(galleryComponent != null)
            {
                self.enabled = true;
                self.monsterCredit += monsterCredit;
                self.OverrideCurrentMonsterCard(chosenDirectorCard);
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
                    string nameToken = ItemCatalog.GetItemDef(itemToGive)?.nameToken;
                    Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                    {
                        subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
                        baseToken = baseUseMessage,
                        paramTokens = new string[]
                        {
                            component2.baseNameToken,
                            nameToken
                        }
                    });
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
                GalleryDirector component = self.gameObject.GetComponent<GalleryDirector>();
                if (inv == null)
                {
                    return value;
                }
                if (component == null)
                {
                    return value;
                }
                inv.GiveItem(itemToGive);
                inv.GiveItem(Items.Helpers.GalleryItemDrop.instance.ItemsDef);                
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
            GalleryDirector component = self.gameObject.AddComponent<GalleryDirector>();
            ChooseItem();
            GameObject obj = CombatEncounterHelper.MethodOne(self, activator, 40, 1);
            orig(self, activator);
            self.available = false;
        }
        GameObject enemySpawned;
        ItemIndex[] itemPool;
        ItemIndex itemToGive;
        public void ChooseItem()
        {
            int i = UnityEngine.Random.RandomRangeInt(0, itemPool.Length - 1);
            itemToGive = itemPool[i];
        }
    }

}
