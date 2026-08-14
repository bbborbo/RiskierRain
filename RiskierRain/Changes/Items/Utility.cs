using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using MoreStats;
using RoR2.Projectile;
using UnityEngine.AddressableAssets;

namespace RiskierRain.Changes
{
    public static partial class ItemChanges
    {
        #region Chance Doll
        public static int chanceDollChanceBase = 30; //40
        public static int chanceDollChanceStack = 30; //10
        public static void ChangeChanceDoll()
        {
            LoadAsync<BasicPickupDropTable>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_ExtraShrineItem.dtChanceDoll_asset, (dropTable) =>
            {
                dropTable.tier2Weight = 0.65f;//0.79f
                dropTable.tier3Weight = 0.30f;//0.20f
                dropTable.bossWeight = 0.05f;//0.01f
            });
            IL.RoR2.ShrineChanceBehavior.AddShrineStack += ChanceDollActivationChance;
            Stage.onStageStartGlobal += ChanceDollShrineSpawn;

            LanguageAPI.Add("ITEM_EXTRASHRINEITEM_PICKUP", "Gain a chance for higher rarity items from Shrines of Chance.");
            LanguageAPI.Add("ITEM_EXTRASHRINEITEM_DESC",
                $"On Shrine of Chance success, " +
                $"<style=cIsUtility>{chanceDollChanceBase}%</style> " +
                $"<style=cStack>(+{chanceDollChanceStack}% per stack)</style> " +
                $"chance to get higher rarity items.");
        }

        private static void ChanceDollActivationChance(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int dollCountLoc = 5;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Items", nameof(DLC2Content.Items.ExtraShrineItem)),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out dollCountLoc))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(out _),
                x => x.MatchLdloc(dollCountLoc),
                x => x.MatchLdcI4(out _)
                );

            if (!b)
            {
                DebugBreakpoint(nameof(ChanceDollActivationChance), 1);
                return;
            }
            c.Next.Operand = chanceDollChanceBase;
            c.Index += 2;
            c.Next.Operand = chanceDollChanceStack;

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchAdd(),
                x => x.MatchConvR4()
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(ChanceDollActivationChance), 2);
                return;
            }
            c.EmitDelegate<Func<float, float>>(Util.ConvertAmplificationPercentageIntoReductionPercentage);
        }

        private static void ChanceDollShrineSpawn(Stage currentStage)
        {
            if (!Run.instance || !NetworkServer.active)
                return;

            if (SceneInfo.instance.countsAsStage == false)
                return;

            SceneDef currentScene = currentStage.sceneDef;
            //if (currentScene.allowItemsToSpawnObjects == false)
            //    return;
            if (currentScene.preventStageAdvanceCounter
                || currentScene.sceneType == SceneType.Intermission
                || currentScene.sceneType == SceneType.Cutscene
                || currentScene.sceneType == SceneType.Junk)
                return;

            int itemCount = Util.GetItemCountForTeam(TeamIndex.Player, DLC2Content.Items.ExtraShrineItem.itemIndex, true, true);
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

            string path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShrineChance.iscShrineChance_asset;//printerSpawncardPaths.Evaluate(rng.nextNormalizedFloat);
            if (currentScene.baseSceneName == "goolake"
                || currentScene.baseSceneName == "ironalluvium")
                path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShrineChance.iscShrineChanceSandy_asset;
            else if (currentScene.baseSceneName == "snowyforest"
                || currentScene.baseSceneName == "nest"
                || currentScene.baseSceneName == "frozenwall")
                path = RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShrineChance.iscShrineChanceSnowy_asset;
            InteractableSpawnCard spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(path).WaitForCompletion();
            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

            GameObject pillarObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
            if (pillarObject == null)
                return;
            if (pillarObject.TryGetComponent(out PurchaseInteraction purchaseInteraction))
            {
                purchaseInteraction.automaticallyScaleCostWithDifficulty = false;
                purchaseInteraction.Networkcost = Run.instance.GetDifficultyScaledCost(purchaseInteraction.cost, Stage.instance.entryDifficultyCoefficient);
            }
            Debug.Log($"(chance doll) chance shrine spawned at " +
                $"[{pillarObject.transform.position.x}, {pillarObject.transform.position.y}, {pillarObject.transform.position.z}] ");
        }
        #endregion
        #region sale star

        public static void ChangeSaleStar()
        {
            LanguageAPI.Add("ITEM_LOWERPRICEDCHESTS_PICKUP", "First chest bought yields an additional reward. Usable once per stage.");
            LanguageAPI.Add("ITEM_LOWERPRICEDCHESTS_DESC",
                $"Gain <style=cIsUtility>1</style> <style=cStack>(+1 per stack)</style> extra item on the first chest opened per stage.");

            IL.RoR2.PurchaseInteraction.OnInteractionBegin += SaleStarOnInteraction;
            IL.RoR2.ChestBehavior.BaseItemDrop += SaleStarItemDrop;
        }

        private static void SaleStarItemDrop(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int droppedCountloc = 3;
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<ChestBehavior>(nameof(ChestBehavior.Roll)),
                x => x.MatchLdloc(out droppedCountloc)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(SaleStarItemDrop));
                return;
            }

            c.Emit(OpCodes.Ldloc, droppedCountloc);
            c.EmitDelegate<Func<ChestBehavior, int, ChestBehavior>>((chest, droppedIndex) =>
            {
                //max drop count is used to tell how many items the chest would have dropped
                if (droppedIndex + 1 /*next drop*/ >= chest.maxDropCount && chest.maxDropCount > chest.dropCount)
                {
                    chest.maxDropCount = chest.dropCount;
                    chest.dropTable = Addressables.LoadAssetAsync<PickupDropTable>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShrineChance.dtShrineChance_asset).WaitForCompletion();
                }
                return chest;
            });
        }

        private static void SaleStarOnInteraction(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int totalTransformedLoc = 13;
            ILLabel skipLabel = c.DefineLabel();
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<PurchaseInteraction>(nameof(PurchaseInteraction.saleStarCompatible)))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt("RoR2.Inventory/ItemTransformation/TryTransformResult", "get_totalTransformed"),
                x => x.MatchStloc(out totalTransformedLoc)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(SaleStarOnInteraction), 1);
                return;
            }

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(out _),
                x => x.MatchStloc(out _),
                x => x.MatchBr(out skipLabel))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdloc(totalTransformedLoc)
                );
            if (b2)
            {
                c.Emit(OpCodes.Br, skipLabel);
            }
            else
            {
                DebugBreakpoint(nameof(SaleStarOnInteraction), 2);
            }

            bool b3 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<ChestBehavior>(nameof(ChestBehavior.dropCount))
                );
            if (!b3)
            {
                DebugBreakpoint(nameof(SaleStarOnInteraction), 3);
                return;
            }

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldloc, totalTransformedLoc);
            c.EmitDelegate<Func<ChestBehavior, int, int>>((chest, totalTransformed) =>
            {
                chest.maxDropCount = chest.dropCount;
                return chest.dropCount + totalTransformed;
            });


            bool b4 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<RouletteChestController>(nameof(RouletteChestController.dropCount))
                );
            if (!b4)
            {
                DebugBreakpoint(nameof(SaleStarOnInteraction), 4);
                return;
            }

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldloc, totalTransformedLoc);
            c.EmitDelegate<Func<RouletteChestController, int, int>>((chest, totalTransformed) =>
            {
                return chest.dropCount + totalTransformed;
            });
        }
        #endregion

        #region fuel cell
        public const float fuelCellCooldownMultiplier = 1f;//no cdr! //0.85f
        public static string fuelCellEquipCdr = Tools.ConvertDecimal(1 - fuelCellCooldownMultiplier);
        public static int fuelCellStock = 1; //1f
        public static void ChangeFuelCell()
        {
            //RetierItemAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_EquipmentMagazine.EquipmentMagazine_asset, ItemTier.Tier3, FixFuelCellIcon);
            //RetierItemAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_EquipmentMagazineVoid.EquipmentMagazineVoid_asset, ItemTier.VoidTier3);
            void FixFuelCellIcon(ItemDef itemDef)
            {
                Sprite sprite = CoreModules.Assets.retierAssetBundle.LoadAsset<Sprite>("Assets/Icons/Fuel_Cell.png");
                if (sprite)
                    itemDef.pickupIconSprite = sprite;
            }

            IL.RoR2.Inventory.CalculateEquipmentCooldownScale += FuelCellCdr;
            IL.RoR2.Inventory.GetEquipmentSlotMaxCharges += FuelCellStock;
            IL.RoR2.Inventory.UpdateEquipment += FuelCellStock;

            LanguageAPI.Add("ITEM_EQUIPMENTMAGAZINE_PICKUP",
                $"Hold an additional equipment charge.");
            LanguageAPI.Add("ITEM_EQUIPMENTMAGAZINE_DESC",
                $"Hold {fuelCellStock} <style=cIsUtility>additional equipment charges</style> <style=cStack>(+{fuelCellStock} per stack)</style>.");
        }


        private static void FuelCellStock(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "EquipmentMagazine"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            c.Emit(OpCodes.Ldc_I4, fuelCellStock);
            c.Emit(OpCodes.Mul);
        }

        private static void FuelCellCdr(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int fuelCell = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "EquipmentMagazine")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out fuelCell)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdloc(fuelCell)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, fuelCellCooldownMultiplier);
        }
        #endregion
        #region warbanner

        public static float warbannerRadiusBase = 18f;
        public static float warbannerRadiusStack = 2f;
        public static float warbannerRegenBase = 1f; //0
        public static float warbannerRegenStack = 1f; //0
        public static float warbannerSpeedBase = 0.3f; //0.3f
        public static float warbannerSpeedStack = 0.15f; //00.3f
        public static void ChangeWarbanner()
        {
            LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_WardOnLevel.bdWarbanner_asset, (buffDef) =>
            {
                buffDef.isHidden = true;
            });
            IL.RoR2.Items.WardOnLevelManager.OnCharacterLevelUp += WarbannerRadiusStacking;
            IL.RoR2.CharacterBody.RecalculateStats += WarbannerStatsScaleWithStacks;
            GetStatCoefficients += WarbannerBonusStats;

            LanguageAPI.Add("ITEM_WARDONLEVEL_PICKUP",
                $"Drop a warbanner on level up and during boss events. Strengthens all allies."
                );
            LanguageAPI.Add("ITEM_WARDONLEVEL_DESC",
                $"On <style=cIsUtility>level up</style> and during <style=cIsUtility>boss events</style>, " +
                $"drop a banner that strengthens all allies " +
                $"within <style=cIsUtility>{warbannerRadiusBase}m</style> <style=cStack>(+{warbannerRadiusStack}m per stack)</style>. " +
                $"Raise <style=cIsDamage>attack</style> and <style=cIsUtility>movement speed</style> " +
                $"by <style=cIsDamage>{warbannerSpeedBase.AsPercent()}</style> <style=cStack>(+{warbannerSpeedStack.AsPercent()} per stack)</style>. " +
                $"Also increases <style=cIsHealing>base health regeneration</style> by " +
                $"<style=cIsHealing>+{warbannerRegenBase} hp/s</style> <style=cStack>(+{warbannerRegenStack} hp/s per stack)</style>."
                );
        }

        private static void WarbannerRadiusStacking(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<BuffWard>("set_Networkradius"))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdcR4(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(WarbannerRadiusStacking));
                return;
            }

            c.Next.Operand = warbannerRadiusBase - warbannerRadiusStack;
            c.Index++;
            c.Next.Operand = warbannerRadiusStack;
        }

        private static void WarbannerBonusStats(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(RoR2Content.Buffs.Warbanner))
            {
                int warbannerCount = 0;
                if (sender.inventory)
                    warbannerCount = sender.inventory.GetItemCountEffective(RoR2Content.Items.WardOnLevel);

                args.baseRegenAdd += warbannerRegenBase + warbannerRegenStack * Mathf.Max(0, warbannerCount - 1);
            }
        }

        private static void WarbannerStatsScaleWithStacks(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int matches = 0;
            while (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", nameof(RoR2Content.Buffs.Warbanner)))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchLdcR4(out _))
                )
            {
                c.Prev.Operand = warbannerSpeedBase;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, CharacterBody, float>>((idc, body) =>
                {
                    if (!body.inventory)
                        return warbannerSpeedBase;
                    int count = body.inventory.GetItemCountEffective(RoR2Content.Items.WardOnLevel);
                    return warbannerSpeedBase + warbannerSpeedStack * Mathf.Max(0, count - 1);
                });
                matches++;
            }
            Debug.LogError("2r4r Warbanner stats matches: [" + matches + "/2]");
        }
        #endregion

        #region bottled chaos

        public const float chaosCooldownMultiplier = 0.67f;
        public static string chaosEquipCdr = Tools.ConvertDecimal(1 - chaosCooldownMultiplier);
        public static void ChangeBottledChaos()
        {
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += BottledChaosCdr;
            LanguageAPI.Add("ITEM_RANDOMEQUIPMENTTRIGGER_DESC",
                $"Trigger a <style=cIsDamage>random equipment</style> effect <style=cIsDamage>1</style> <style=cStack>(+1 per stack)</style> time(s). " +
                $"<style=cIsUtility>Reduce equipment cooldown</style> by " +
                $"<style=cIsUtility>{chaosEquipCdr}</style> <style=cStack>(+{chaosEquipCdr} per stack)</style>.");
        }

        private static float BottledChaosCdr(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            float scale = orig(self);
            int chaosCount = self.GetItemCountEffective(DLC1Content.Items.RandomEquipmentTrigger);
            if (chaosCount > 0)
                scale *= Mathf.Pow(chaosCooldownMultiplier, chaosCount);
            return scale;
        }
        #endregion

        #region chronobauble
        public static void ChangeChronobauble()
        {
            //On.RoR2.GlobalEventManager.OnHitEnemy += ChronobaubleChain
        }

        private static void ChronobaubleChain(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
