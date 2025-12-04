using BepInEx.Configuration;
using R2API;
using RoR2;
using SwanSongExtended.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using static RoR2.CombatDirector;

namespace SwanSongExtended.Interactables
{
    class CombatShrineLunar : InteractableBase<CombatShrineLunar>
    {
        public static string baseUseMessage = "SHRINE_LUNARGALLERY_USE_MESSAGE";
        public override string InteractableName => "Lunar Gallery";

        public override string InteractableContext => "eat my transgendence nerd";

        public override string InteractableLangToken => "LUNAR_GALLERY";

        public override GameObject InteractableModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlLunarGallery.prefab");

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 10;

        public override int spawnCost => 20;

        public override CostTypeIndex costTypeIndex => CostTypeIndex.None;


        public override int interactionCost => 0;

        public override SimpleInteractableData InteractableData => new SimpleInteractableData
            (
                isShrine: true,
                orientToFloor: false,
                maxSpawnsPerStage: 2
            );

        public override string modelName => "mdlLunarGallery"; 

        public override string prefabName => "lunarGallery";

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Shrines;

        public override int favoredWeight => 25;

        public override string[] validScenes => new string[]
        {
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
        public override string[] favoredScenes => new string[]
        {
            "blackbeach",
            "blackbeach2",
            "frozenwall",
            "rootjungle",
            "skymeadow",
            //modded stages
            "FBLScene"
        };

        public override void Init()
        {
            LanguageAPI.Add(baseUseMessage, "<style=cShrine>You have invoked <style=cIsLunar>{1}</style> in battle.</color>");
            LanguageAPI.Add(baseUseMessage + "_2P", "<style=cShrine>{0} has invoked <style=cIsLunar>{1}</style> in battle.</color>");
            base.Init();

            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.GoldOnHit));
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.RepeatHeal));
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.AutoCastEquipment));
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarPrimaryReplacement));
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarUtilityReplacement));
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarSpecialReplacement));
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.LunarTrinket));
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.FocusConvergence));
            SwanSongPlugin.BlacklistSingleItem(nameof(RoR2Content.Items.MonstersOnShrineUse));

            Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_LunarSun.LunarSun_asset).Completed += (ctx) =>
                SwanSongPlugin.BlacklistSingleItem(ctx.Result);
            Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC1_RandomlyLunar.RandomlyLunar_asset).Completed += (ctx) =>
                SwanSongPlugin.BlacklistSingleItem(ctx.Result);
            Addressables.LoadAssetAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.RoR2_DLC2_Items_OnLevelUpFreeUnlock.OnLevelUpFreeUnlock_asset).Completed += (ctx) =>
                SwanSongPlugin.BlacklistSingleItem(ctx.Result);
        }

        public override void Hooks()
        {
            On.RoR2.Run.BuildDropTable += GalleryItemPool;
        }

        private void GalleryItemPool(On.RoR2.Run.orig_BuildDropTable orig, Run self)
        {
            orig(self);
            itemPool = ItemCatalog.allItemDefs.Where(
                item => item.tier == ItemTier.Lunar
                && !item.ContainsTag(ItemTag.WorldUnique) && !item.ContainsTag(ItemTag.AIBlacklist)
                ).ToArray();
        }

        GameObject enemySpawned;
        ItemDef[] itemPool;
        public static ItemIndex itemToGive;
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
            cd.combatSquad = cs;
            LunarCombatShrineBehavior lscb = interaction.gameObject.AddComponent<LunarCombatShrineBehavior>();
            lscb.baseMonsterCredit = 40;
            lscb.maxPurchaseCount = 1;
            lscb.monsterCreditCoefficientPerPurchase = 2;

            GameObject symbolTransform = new GameObject();
            symbolTransform.transform.parent = interaction.transform;
            lscb.symbolTransform = symbolTransform.transform;

            return lscb.OnInteractionBegin;
        }
    }
    public class LunarCombatShrineBehavior : ShrineCombatBehavior
    {
        public void OnInteractionBegin(Interactor activator)
        {
            if (purchaseCount >= maxPurchaseCount)
                return;
            combatDirector.onSpawnedServer.AddListener(OnGalleryDirectorSpawnServer);
            CombatShrineLunar.instance.ChooseItem();
            CharacterBody interactorBody = activator.GetComponent<CharacterBody>();
            if (interactorBody)
            {
                string nameToken = ItemCatalog.GetItemDef(CombatShrineLunar.itemToGive)?.nameToken;
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
            //combatDirector.onSpawnedServer.RemoveListener(OnGalleryDirectorSpawnServer);
            purchaseInteraction.SetAvailable(false);

            void OnGalleryDirectorSpawnServer(GameObject masterObject)
            {
                CharacterMaster master = masterObject.GetComponent<CharacterMaster>();
                OnGallerySquadSpawnServer(master);
            }
            void OnGallerySquadSpawnServer(CharacterMaster master)
            {
                if (master != null)
                {
                    Inventory inv = master.inventory;
                    if (inv != null)
                    {
                        inv.GiveItem(CombatShrineLunar.itemToGive);
                        inv.GiveItem(GalleryItemDrop.instance.ItemsDef);
                    }
                }
            }
        }
    }
}
