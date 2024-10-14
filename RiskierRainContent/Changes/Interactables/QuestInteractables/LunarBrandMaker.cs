using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace RiskierRainContent.Interactables
{
    class LunarBrandMaker : InteractableBase<LunarBrandMaker>
    {
        public override string InteractableName => "Strange Object";

        public override string InteractableContext => "Charge with Flame Orb";

        public override string InteractableLangToken => "LUNARBRANDMAKER";

        public override GameObject InteractableModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/lunarBrandMaker.prefab");

        public override string modelName => "mdlLunarBrandMaker";

        public override string prefabName => "lunarBrandMaker";

        public override bool ShouldCloneModel => false;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 50;

        public override int favoredWeight => 0;

        public override DirectorAPI.InteractableCategory category => DirectorAPI.InteractableCategory.Shrines;

        public override int spawnCost => 1;


        public override CostTypeIndex costTypeIndex => CostTypeIndex.None;

        public override int costAmount => 1;

        public override int interactableMinimumStageCompletions => 0;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => true;

        public override bool orientToFloor => false;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 2;

        public string[] validScenes = {
            "wispgraveyard",
            "dampcavesimple",
            "sulfurpools",
			//modded stages
            "drybasin"
        };

        public override void Init(ConfigFile config)
        {
            Debug.LogError("Lunar brand maker needs fixes");
            hasAddedInteractable = false;
            //On.RoR2.CampDirector.SelectCard += new On.RoR2.CampDirector.hook_SelectCard(VoidCampAddInteractable);
            On.RoR2.PurchaseInteraction.OnInteractionBegin += LunarBrandMakerBehavior;
            On.RoR2.ClassicStageInfo.RebuildCards += AddInteractable;
            CreateLang();
            CreateInteractable();
            var cards = CreateInteractableSpawnCard();
            customInteractable.CreateCustomInteractable(cards.interactableSpawnCard, cards.directorCard, validScenes);

            On.RoR2.CostTypeDef.PayCost += LunarBrandMakerPayCostHook;
        }
        CostTypeDef customCostType;

        private CostTypeDef.PayCostResults LunarBrandMakerPayCostHook(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
        {
            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (purchasedObject.GetComponent<GenericDisplayNameProvider>()?.displayToken == "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME" && activatorBody != null)
            {
                cost = 0;
                Inventory  activatorInventory = activatorBody.inventory;
                if (activatorInventory == null)
                {
                    return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
                }
                int flameOrbCount = activatorInventory.GetItemCount(Items.FlameOrb.instance.ItemsDef);
                if (flameOrbCount == 0)
                {
                    return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
                }
                activatorInventory.RemoveItem(Items.FlameOrb.instance.ItemsDef);
                activatorInventory.GiveItem(Items.LunarBrand.instance.ItemsDef);
                //CharacterMasterNotificationQueue.SendTransformNotification(activatorBody.master,
                //            Items.FlameOrb.instance.ItemsDef.itemIndex, Items.LunarBrand.instance.ItemsDef.itemIndex,
                //            CharacterMasterNotificationQueue.TransformationType.Default);
            }
            return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
        }

        private void LunarBrandMakerBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            orig(self, activator);
            if (self.displayNameToken == "2R4R_INTERACTABLE_" + this.InteractableLangToken + "_NAME")
            {
                Debug.Log("uhhh yeag");
                GameObject.Destroy(self.gameObject);
            }
        }

        public override UnityAction<Interactor> GetInteractionAction(PurchaseInteraction interaction)
        {
            return null;
        }
        //public Transform dropletOrigin;
    }
}
