using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.AddressableAssets;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using UnityEngine;

namespace RainrotSharedUtils
{
    public static partial class Extensions
    {
        public static void AddAllRecipePermutations(this CraftableDef craftableDef, RecipeIngredient[] firstSlotIngredients, RecipeIngredient[] secondSlotIngredients)
        {
            List<Recipe> recipes = new List<Recipe>();

            foreach (RecipeIngredient first in firstSlotIngredients)
            {
                foreach (RecipeIngredient second in secondSlotIngredients)
                {
                    recipes.Add(CraftingUtils.MakeRecipe(first, second));
                }
            }

            if (craftableDef.recipes == null || craftableDef.recipes.Length == 0)
            {
                craftableDef.recipes = recipes.ToArray();
                return;
            }
            craftableDef.recipes = craftableDef.recipes.Concat(recipes).ToArray();
        }

        public static void TryLoadAndAddIngredient<T>(this List<RecipeIngredient> list, string path, string debugName = "") where T : UnityEngine.Object
        {
            if (CraftingUtils.LoadAsIngredient<T>(path, out RecipeIngredient ingredient))
                list.Add(ingredient);
            else
                Debug.LogError($"CraftingUtils: Ingredient [{debugName}] failed to load! This could cause serious failure!");
        }
    }
    public static class CraftingUtils
    {
        private static RecipeIngredient[] _VanillaBossKeys;
        public static RecipeIngredient[] VanillaBossKeys 
        {
            get 
            {
                if(_VanillaBossKeys == null || _VanillaBossKeys.Length == 0)
                {
                    List<RecipeIngredient> list = new List<RecipeIngredient>();
                    list.TryLoadAndAddIngredient<ItemDef>(RoR2_DLC3_Items_PowerPyramid.PowerPyramid_asset, "powerpyramid");
                    list.TryLoadAndAddIngredient<ItemDef>(RoR2_DLC3_Items_PowerCube.PowerCube_asset, "powercube");
                    //list.TryLoadAndAddIngredient<ItemDef>(RoR2_DLC3_Items_PowerOrbSphere.PowerOrbSphere_asset, "powerorb");
                    _VanillaBossKeys = list.ToArray();
                }

                return _VanillaBossKeys;
            }
            private set { _VanillaBossKeys = value; }
        }
        public static Recipe MakeRecipe(RecipeIngredient ingredientL, RecipeIngredient ingredientR)
        {
            Recipe newRecipe = new Recipe();
            newRecipe.ingredients = new RecipeIngredient[]
            {
                ingredientL,
                ingredientR
            };
            return newRecipe;
        }
        public static bool LoadAsIngredient<T>(string path, out RecipeIngredient ingredient) where T : UnityEngine.Object
        {
            ingredient = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            Type t = typeof(T);
            if (t != typeof(ItemDef) && t != typeof(EquipmentDef))
                return false;

            ingredient = GetRecipeIngredient(Addressables.LoadAssetAsync<T>(path).WaitForCompletion());
            return ingredient != null;
        }

        public static bool ItemToIngredient(ItemDef item, out RecipeIngredient ingredient)
        {
            ingredient = GetRecipeIngredient(item);
            return ingredient != null;
        }
        public static bool EquipmentToIngredient(EquipmentDef item, out RecipeIngredient ingredient)
        {
            ingredient = GetRecipeIngredient(item);
            if (ingredient == null)
                return false;
            ingredient.isLunar = item.isLunar;
            ingredient.isBoss = item.isBoss;
            return true;
        }
        public static RecipeIngredient GetRecipeIngredient(UnityEngine.Object pickup)
        {
            if(pickup == null || (pickup is not ItemDef && pickup is not EquipmentDef))
            {
                return null;
            }
            return new RecipeIngredient()
            {
                pickup = pickup,
                type = IngredientTypeIndex.AssetReference
            };
        }
    }
}
