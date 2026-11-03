using static R2API.RecalculateStatsAPI;
using static RainrotSharedUtils.Styling;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using RoR2.Items;
using System.Linq;
using RoR2BepInExPack.GameAssetPathsBetter;
using RoR2BepInExPack.Utilities;
using static SwanSongExtended.Items.Photograph;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class Photograph : ItemBase<Photograph>
    {
        public class CharacterPrintCount
        {
            public void Add()
            {
                if (NetworkServer.active == false)
                    return;
                printCount++;
            }
            public void Reset()
            {
                if (NetworkServer.active == false)
                    return;
                printCount = 0;
            }
            public int printCount { get; private set; }
        }
        public static FixedConditionalWeakTable<CharacterMaster, CharacterPrintCount> characterPrintCounts = new FixedConditionalWeakTable<CharacterMaster, CharacterPrintCount>();
        public static CharacterPrintCount GetPrintCountFromMaster(CharacterMaster body)
        {
            if (body == null)
                return null;
            return characterPrintCounts.GetOrCreateValue(body);
        }
        public override bool CanBeRental => false;
        public static WeightedSelection<string> printerSpawncardPaths;
        public static bool includeDeploys = true;

        public static BuffDef photographCritBuff;
        public static float photographCritFreeBase = 0f;
        public static float photographCritFreeStack = 0f;
        public static float photographCritBase = 10f;
        public static float photographCritStack = 5f;
        public static int photographMaxPrintsBase = 3;
        public static int photographMaxPrintsStack = 0;
        public override string ItemName => "Photograph";

        public override string ItemLangTokenName => "PHOTOGRAPH";

        public override string ItemPickupDesc => "Printing items increases critical strike chance and damage. Resets at the start of each stage.";

        public override string ItemFullDescription => $"Spending items at any printer increases " +
            $"{DamageColor("critical strike chance")} and {DamageColor("critical strike damage")} " +
            $"by {DamageColor($"+{photographCritBase}%")} {StackText($"+{photographCritStack}%")}, " +
            $"up to {UtilityColor($"{photographMaxPrintsBase} times")} {StackText($"+{photographMaxPrintsStack}")}. " +
            $"Resets at the start of each stage.";

        public override string ItemLore => $"Did you get your photos printed?\n\n\"Bogos binted?\"\n\nHuh?\n\n\"Download GreenAlienHead\"";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.DevotionBlacklist, ItemTag.InteractableRelated, ItemTag.AIBlacklist };

        public override GameObject ItemModel => LoadDropPrefab("mdlPhotograph");

        public override Sprite ItemIcon => LoadItemIcon("texIconPhotograph");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Init()
        {
            photographCritBuff = Content.CreateAndAddBuff(
                "bdPhotographCrit",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion(),
                Color.magenta,
                true,
                false,
                BuffDef.StackingDisplayMethod.Percentage
                );
            printerSpawncardPaths = new WeightedSelection<string>();
            printerSpawncardPaths.AddChoice(RoR2_Base_Duplicator.iscDuplicator_asset, 8);
            printerSpawncardPaths.AddChoice(RoR2_Base_DuplicatorLarge.iscDuplicatorLarge_asset, 4);
            printerSpawncardPaths.AddChoice(RoR2_Base_DuplicatorMilitary.iscDuplicatorMilitary_asset, 2);
            base.Init();
        }

        public override void Hooks()
        {
            //On.RoR2.SceneDirector.PopulateScene += SceneDirector_PopulateScene;
            RoR2.Stage.onStageStartGlobal += PhotographPrinterSpawn;
            RoR2.Stage.onServerStageComplete += PhotographPrintCountReset;
            //RoR2.SceneDirector.onPostPopulateSceneServer += PhotographForcedPrinter;
            On.RoR2.Items.MultiShopCardUtils.OnNonMoneyPurchase += PhotographOnNonMoneyPurchase;
            GetStatCoefficients += PhotographCritBonus;
        }

        private void PhotographPrintCountReset(Stage obj)
        {
            foreach(CharacterMaster master in CharacterMaster.instancesList)
            {
                CharacterPrintCount cpc = GetPrintCountFromMaster(master);
                    if(cpc != null) cpc.Reset();
            }
        }

        #region free printer
        private void PhotographForcedPrinter(SceneDirector self)
        {
            if (!Run.instance)
                return;

            SceneDef currentScene = Stage.instance.sceneDef;
            if (currentScene.preventStageAdvanceCounter
                || currentScene.sceneType == SceneType.Intermission
                || currentScene.sceneType == SceneType.Cutscene
                || currentScene.sceneType == SceneType.UntimedStage
                || currentScene.sceneType == SceneType.Junk)
                return;

            int itemCount = Util.GetItemCountForTeam(TeamIndex.Player, instance.ItemsDef.itemIndex, true, true);
            if (itemCount <= 0)
                return;

            Xoroshiro128Plus rng = Run.instance.stageRng;
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                placementMode =
                    SceneInfo.instance && SceneInfo.instance.approximateMapBoundMesh
                        ? DirectorPlacementRule.PlacementMode.RandomNormalized
                        : DirectorPlacementRule.PlacementMode.Random
            };

            string path = printerSpawncardPaths.Evaluate(rng.nextNormalizedFloat);
            Log.Debug("Photograph spawning printer: " + path);
            InteractableSpawnCard spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

            GameObject pillarObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
            //if (pillarObject)
            //{
            //    createdPillarObjects.Add(pillarObject);
            //    pillarTypeSpawnCount[pillarIndex]++;
            //}
        }

        private void SceneDirector_PopulateScene(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            orig(self);

            if (!Run.instance)
                return;

            SceneDef currentScene = Stage.instance.sceneDef;
            if (currentScene.preventStageAdvanceCounter
                || currentScene.sceneType == SceneType.Intermission
                || currentScene.sceneType == SceneType.Cutscene
                || currentScene.sceneType == SceneType.UntimedStage
                || currentScene.sceneType == SceneType.Junk)
                return;

            int itemCount = Util.GetItemCountForTeam(TeamIndex.Player, instance.ItemsDef.itemIndex, true, true);
            if (itemCount <= 0)
                return;

            Xoroshiro128Plus rng = Run.instance.stageRng;
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                placementMode =
                    SceneInfo.instance && SceneInfo.instance.approximateMapBoundMesh
                        ? DirectorPlacementRule.PlacementMode.RandomNormalized
                        : DirectorPlacementRule.PlacementMode.Random
            };

            string path = printerSpawncardPaths.Evaluate(rng.nextNormalizedFloat);
            Log.Debug("Photograph spawning printer: " + path);
            InteractableSpawnCard spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

            GameObject pillarObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
            //if (pillarObject)
            //{
            //    createdPillarObjects.Add(pillarObject);
            //    pillarTypeSpawnCount[pillarIndex]++;
            //}
        }

        private void PhotographPrinterSpawn(Stage currentStage)
        {
            if (!Run.instance || !NetworkServer.active)
                return;

            SceneDef currentScene = currentStage.sceneDef;
            if (currentScene.sceneType == SceneType.Intermission
                || currentScene.sceneType == SceneType.Cutscene
                || currentScene.sceneType == SceneType.Junk)
                return;

            int itemCount = Util.GetItemCountForTeam(TeamIndex.Player, instance.ItemsDef.itemIndex, true, true);
            if (itemCount <= 0)
                return;

            Xoroshiro128Plus rng = Run.instance.stageRng;
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                placementMode = 
                    SceneInfo.instance && SceneInfo.instance.approximateMapBoundMesh 
                        ? DirectorPlacementRule.PlacementMode.RandomNormalized 
                        : DirectorPlacementRule.PlacementMode.Random
            };

            string path = printerSpawncardPaths.Evaluate(rng.nextNormalizedFloat);
            InteractableSpawnCard spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

            GameObject pillarObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
            Debug.Log($"(photograph) printer {pillarObject.name} spawned at " +
                $"[{pillarObject.transform.position.x}, {pillarObject.transform.position.y}, {pillarObject.transform.position.z}] ");
            //if (pillarObject)
            //{
            //    createdPillarObjects.Add(pillarObject);
            //    pillarTypeSpawnCount[pillarIndex]++;
            //}
        }
        #endregion

        private void PhotographCritBonus(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                args.critAdd += photographCritFreeBase + (photographCritFreeStack * (itemCount - 1));
                args.critDamageMultAdd += photographCritFreeBase + (photographCritFreeStack * (itemCount - 1));
            }
            int buffCount = sender.GetBuffCount(photographCritBuff);
            if (buffCount > 0)
            {
                args.critAdd += buffCount;
                args.critDamageMultAdd += buffCount * 0.01f;
            }
        }

        private void PhotographOnNonMoneyPurchase(On.RoR2.Items.MultiShopCardUtils.orig_OnNonMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            PhotographOnPrinterPurchase(context);
            orig(context);
        }

        private void PhotographOnPrinterPurchase(CostTypeDef.PayCostContext context)
        {
            if (context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.WhiteItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.GreenItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.RedItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.BossItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarItemOrEquipment)
                    )
                return;

            CharacterPrintCount cpc = GetPrintCountFromMaster(context.activatorMaster);
            if(cpc != null)
            {
                cpc.Add();
                if(context.activatorBody && context.activatorBody.TryGetComponent(out BogosBinted bogos))
                {
                    bogos.RefreshCritBonus(cpc.printCount);
                }
            }

            //int itemCount = GetCount(context.activatorInventory);
            //if (itemCount <= 0)
            //    return;
            //
            //float critBonus = photographCritBase + (photographCritStack * (itemCount - 1));
            //int maxTimes = photographMaxPrintsBase + (photographMaxPrintsStack * (itemCount - 1));
            //int maxBuff = maxTimes * Mathf.FloorToInt(critBonus);
            //int buffCount = context.activatorBody.GetBuffCount(photographCritBuff);
            //if (buffCount >= maxBuff)
            //    return;
            //critBonus = Mathf.Min(critBonus, maxBuff - buffCount);
            //
            //for (int i = 0; i < critBonus; i++)
            //{
            //    context.activatorBody.AddBuff(photographCritBuff);
            //}
        }
    }
    public class BogosBinted : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => Photograph.instance?.ItemsDef ?? null;
        CharacterMaster master;
        bool isMinion = false;
        float refreshTimer = 0;
        const float refreshInterval = 10;
        void OnEnable()
        {
            master = body.master;
            if(Photograph.includeDeploys == true && body.isPlayerControlled == false && master.TryGetComponent(out Deployable deployable))
            {
                isMinion = true;
                master = deployable.ownerMaster;
            }
        }
        void OnDisable()
        {
            RefreshCritBonus();
        }

        int GetMaxPrints()
        {
            if (Photograph.photographMaxPrintsStack == 0)
                return Photograph.photographMaxPrintsBase;
            return (int)ItemBase.GetStackValue(Photograph.photographMaxPrintsBase, Photograph.photographMaxPrintsStack, stack);
        }
        public override void OnInventoryRefresh()
        {
            base.OnInventoryRefresh();
            if(stack == 0)
            {
                RefreshCritBonus();
                return;
            }
            GetPrintCountAndRefresh();
        }

        void FixedUpdate()
        {
            if (isMinion == false)
                return;
            if(refreshTimer > 0)
            {
                refreshTimer -= Time.fixedDeltaTime;
                return;
            }
            refreshTimer = refreshInterval;
            GetPrintCountAndRefresh();
        }

        public void GetPrintCountAndRefresh()
        {
            CharacterPrintCount cpc = GetPrintCountFromMaster(master);
            if (cpc != null)
            {
                int prints = cpc.printCount;
                RefreshCritBonus(prints);
            }
        }

        public void RefreshCritBonus(int prints = 0)
        {
            if (prints == 0)
            {
                body.SetBuffCount(Photograph.photographCritBuff.buffIndex, 0);
                return;
            }
            int count = (int)ItemBase.GetStackValue(Photograph.photographCritBase, Photograph.photographCritStack, stack);
            body.SetBuffCount(Photograph.photographCritBuff.buffIndex, count * prints);
        }
    }

}