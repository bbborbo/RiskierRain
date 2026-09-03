using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using BossDropRework;
using static BossDropRework.BossDropReworkPlugin;
using SwanSongExtended.Modules;
using System.Runtime.CompilerServices;
using RainrotSharedUtils;
using System.Linq;

namespace SwanSongExtended.Items
{
    class DisposableScalpel : ItemBase<DisposableScalpel>
    {
        public static bool GetScalpelConfig()
        {
            return SwanSongPlugin.GetConfigBool(true, "Items : Scalpel", "Also enables Gamma Knife");
        }
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return DisposableScalpel.GetScalpelConfig();
        }
        public static ItemDef brokenItemDef;
        public override string ConfigName => "Items : Scalpel";
        public static int bonusDropChance = 50;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Obsidian Scalpel";

        public override string ItemLangTokenName => "BOSSITEMCONSUMABLE";

        public override string ItemPickupDesc => "Large monsters have a much greater chance of dropping a trophy. Consumed on drop.";

        public override string ItemFullDescription => $"Killing enemies who are capable of dropping <style=cIsDamage>trophy items</style> " +
            $"yields an additional <style=cIsUtility>{bonusDropChance}%</style> chance " +
            $"to drop the <style=cIsDamage>trophy</style>. " +
            $"Consumed on successful use.";

        public override string ItemLore =>
@"Order: Medical Scalpel (Obsidian)
Tracking Number: 91***********
Estimated Delivery: 09/30/2056
Shipping Method:  Priority/Fragile
Shipping Address: Mt Goliath, Mars
Shipping Details:

Custom made according to your specifications. Very sharp. Blade thickness is measured in planck lengths. This will definitely cut whatever you need it for.
Can’t speak for the durability, though. Try to get it right the first time.
And one more thing – when it breaks, it won’t wait ‘till your operation is finished. Don’t use it on anything you care about not damaging. Or killing.
You already knew all that, though. Can’t help but wonder what you keep ordering these things for.";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => LoadDropPrefab("mdlDisposableScalpel");

        public override Sprite ItemIcon => LoadItemIcon("texIconDisposableScalpel");

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnKillEffect, ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            brokenItemDef = CreateNewUntieredItem("BROKENSCALPEL",
                LoadItemIcon("texIconDisposableScalpelUsed"));
            DoLangForItem(brokenItemDef, "Broken Scalpel", "The blade has shattered into innumerous pieces.", "It is no longer usable.");
            base.Init();
        }
        public override void PostInit()
        {
            base.PostInit();
            RecipeIngredient brokenScalpel = CraftingUtils.GetRecipeIngredient(brokenItemDef);

            CraftableDef craftScalpel = ScriptableObject.CreateInstance<CraftableDef>();
            craftScalpel.name = "CRAFTABLE_" + this.ItemLangTokenName;
            craftScalpel.pickup = this.ItemsDef;
            craftScalpel.itemIndex = this.ItemsDef.itemIndex;

            CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Scrap.ScrapYellow_asset,
                out RecipeIngredient yellowScrap);

            Recipe repair = CraftingUtils.MakeRecipe(brokenScalpel, yellowScrap);

            CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_TreasureCache.TreasureCache_asset,
                out RecipeIngredient rustedKey);
            CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BleedOnHit.BleedOnHit_asset,
                out RecipeIngredient triTipDagger);

            Recipe craft = CraftingUtils.MakeRecipe(rustedKey, triTipDagger);

            craftScalpel.recipes = new Recipe[] { repair, craft };
            Content.AddCraftableDef(craftScalpel);

            return;
            SwanSongPlugin.LoadAsync<CraftableDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Recipes.cdFireballsOnHit_asset, (craftMerf) =>
            {
                CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ElementalRings.FireRing_asset,
                    out RecipeIngredient kjaro);

                craftMerf.recipes = craftMerf.recipes.Append(CraftingUtils.MakeRecipe(brokenScalpel, kjaro)).ToArray();
            });
            SwanSongPlugin.LoadAsync<CraftableDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Recipes.cdLightningStrikeOnHit_asset, (craftCherf) =>
            {
                CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Lightning.Lightning_asset,
                    out RecipeIngredient capacitor);
                CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ShockNearby.ShockNearby_asset,
                    out RecipeIngredient tesla);

                craftCherf.AddAllRecipePermutations(new RecipeIngredient[] { brokenScalpel }, new RecipeIngredient[] { tesla, capacitor });
            });
        }

        public override void Hooks()
        {
            if (SwanSongPlugin.isBossDropLoaded)
                DoFruityBossDropHooks();
            else
            {
                GlobalEventManager.onCharacterDeathGlobal += ScalpelOnKillHetero;
            }
        }

        #region hetero
        private void ScalpelOnKillHetero(DamageReport damageReport)
        {
            CharacterBody characterBody = damageReport.attackerBody;
            if (characterBody != null && damageReport.victimBody != null && damageReport.victimBody.TryGetComponent(out DeathRewards deathRewards))
			{
                if (deathRewards.bossDropTable == null || deathRewards.bossDropTable.GetPickupCount() <= 0)
                    return;
				Vector3 vector = damageReport.victimBody.corePosition;
				Vector3 normalized = (vector - damageReport.attackerBody.corePosition).normalized;
                if (GetScalpelProc(characterBody))
                {
                    UniquePickup drop = deathRewards.bossDropTable.GeneratePickup(Run.instance.bossRewardRng);
                    PickupDropletController.CreatePickupDroplet(drop, vector, normalized * 15f, false);
                }
			}
		}
        #endregion
        #region fruity
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void DoFruityBossDropHooks()
        {
            ShouldTricornFireAndBreak += ScalpelTricornSynergy;
            ModifyBossItemDropChance += ScalpelDropChance;
        }

        private void ScalpelTricornSynergy(CharacterBody attacker, CharacterBody victim, ref bool shouldFire)
        {
            if (GetCount(attacker) > 0)
            {
                shouldFire = false;
                ConsumeScalpel(attacker);
            }
        }

        private void ScalpelDropChance(CharacterBody victim, CharacterBody attacker, ref float dropChance)
        {
			if(dropChance > 0 && dropChance < 100 && GetScalpelProc(attacker))
			    dropChance = 100;
        }
        #endregion

        public bool GetScalpelProc(CharacterBody attackerBody)
		{
			if (GetCount(attackerBody) > 0)
			{
				if (Util.CheckRoll(bonusDropChance))
				{
					ConsumeScalpel(attackerBody);
					return true;
				}
			}
			return false;
		}
        public static void ConsumeScalpel(CharacterBody attackerBody)
        {
            Inventory.ItemTransformation.TryTransformResult tryTransformResult;
            new Inventory.ItemTransformation
            {
                originalItemIndex = instance.ItemsDef.itemIndex,
                newItemIndex = brokenItemDef.itemIndex,
                maxToTransform = 1,
                transformationType = (ItemTransformationTypeIndex)CharacterMasterNotificationQueue.TransformationType.Default
            }.TryTransform(attackerBody.inventory, out tryTransformResult);
        }
    }
}
