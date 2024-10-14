using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Interactables;
using RiskierRainContent.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using static RoR2.CombatDirector;

namespace RiskierRainContent.Interactables
{
    class CombatShrineLunar : InteractableBase<CombatShrineLunar>
    {
        public static string baseUseMessage = "SHRINE_LUNARGALLERY_USE_MESSAGE";
        public override string InteractableName => "Lunar Gallery";

        public override string InteractableContext => "eat my transgendence nerd";

        public override string InteractableLangToken => "LUNAR_GALLERY";

        public override GameObject InteractableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/lunarGallery.prefab");

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 10;

        public override int spawnCost => 20;

        public override CostTypeIndex costTypeIndex => CostTypeIndex.None;

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

            LanguageAPI.Add(baseUseMessage, "<style=cShrine>You have invoked <style=cIsLunar>{1}</style> in battle.</color>");
            LanguageAPI.Add(baseUseMessage + "_2P", "<style=cShrine>{0} has invoked <style=cIsLunar>{1}</style> in battle.</color>");
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            var favored = CreateInteractableSpawnCard(true);
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes, favored.interactableSpawnCard, favored.directorCard, favoredStages);
            
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
            RiskierRainContent.AIBlacklistSingleItem(nameof(DLC2Content.Items.OnLevelUpFreeUnlock));
            On.RoR2.Run.BuildDropTable += GalleryItemPool;
        }

        private void GalleryItemPool(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);
            itemPool = ItemCatalog.allItemDefs.Where(
                item => item.tier == ItemTier.Lunar
                && !item.ContainsTag(ItemTag.AIBlacklist)
                ).ToArray();
        }

        GameObject enemySpawned;
        ItemDef[] itemPool;
        internal ItemIndex itemToGive;
        public void ChooseItem()
        {
            int i = UnityEngine.Random.RandomRangeInt(0, itemPool.Length - 1);
            itemToGive = itemPool[i].itemIndex;
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            CombatSquad cs = interaction.gameObject.AddComponent<CombatSquad>();
            CombatDirector cd = interaction.gameObject.AddComponent<CombatDirector>();
            cd.expRewardCoefficient = 1f;
            cd.goldRewardCoefficient = 1f;
            cd.eliteBias = 1;
            cd.maximumNumberToSpawnBeforeSkipping = 6;
            cd.teamIndex = TeamIndex.Lunar;
            cd.fallBackToStageMonsterCards = true;
            cd.onSpawnedServer = new OnSpawnedServer();
            cd.onSpawnedServer.AddListener(OnGalleryDirectorSpawnServer);
            LunarCombatShrineBehavior lscb = interaction.gameObject.AddComponent<LunarCombatShrineBehavior>();
            lscb.baseMonsterCredit = 40;
            lscb.maxPurchaseCount = 1;
            lscb.monsterCreditCoefficientPerPurchase = 2;

            return lscb.OnInteractionBegin;

            void OnGalleryDirectorSpawnServer(GameObject masterObject)
            {
                CharacterMaster master = masterObject.GetComponent<CharacterMaster>();
                if(master != null)
                {
                    Inventory inv = master.GetBody()?.inventory;
                    if(inv != null)
                    {
                        inv.GiveItem(itemToGive);
                        inv.GiveItem(Items.Helpers.GalleryItemDrop.instance.ItemsDef);
                    }
                }
            }
        }

    }
    public class LunarCombatShrineBehavior : ShrineCombatBehavior
    {
        public void OnInteractionBegin(Interactor activator)
        {
            CombatShrineLunar.instance.ChooseItem();
            CharacterBody interactorBody = activator.GetComponent<CharacterBody>();
            if (interactorBody)
            {
                string nameToken = ItemCatalog.GetItemDef(CombatShrineLunar.instance.itemToGive)?.nameToken;
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = interactorBody,
                    baseToken = CombatShrineLunar.baseUseMessage,
                    paramTokens = new string[]
                    {
                        nameToken
                    }
                });
            }
            base.AddShrineStack(activator);
        }
    }
}
