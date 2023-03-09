using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using HarmonyLib;
using On.RoR2.Items;

namespace RiskierRain.Items
{
    class Egg : ItemBase<Egg>
    {
        public int eggHealth = 2;
        public float eggRegen = 0.1f;
        public float eggDamage = 0.2f;


        public override string ItemName => "Egg";

        public override string ItemLangTokenName => "EGG";

        public override string ItemPickupDesc => "gain (stats). Start an egg hunt." +
            "<style=cIsVoid>Corrupts all Regenerating Scrap.</style>";

        public override string ItemFullDescription => "yeag";

        public override string ItemLore => "this egg is so fuckign yummuy";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };

        public override BalanceCategory Category => BalanceCategory.StateOfInteraction;

        public override GameObject ItemModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/egg.prefab");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += EggOnDeath;
            On.RoR2.GlobalEventManager.OnInteractionBegin += EggOnPurchase;//includes uhhhhhh the uh yea. (item pickups)
            //make interactable
            RecalculateStatsAPI.GetStatCoefficients += EggStats;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }

        private void EggStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = sender.inventory.GetItemCount(Egg.instance.ItemsDef.itemIndex);
            if (itemCount > 0)
            {
                args.baseHealthAdd += eggHealth * itemCount;
                args.baseRegenAdd += eggRegen * itemCount;
                args.baseDamageAdd += eggDamage * itemCount; 
            }
        }

        private void EggOnPurchase(On.RoR2.GlobalEventManager.orig_OnInteractionBegin orig, GlobalEventManager self, Interactor interactor, IInteractable interactable, GameObject interactableObject)
        {
            orig(self, interactor, interactable, interactableObject);

            GameObject interactorObject = interactor.gameObject;
            if (interactorObject == null) return;
            CharacterBody interactorBody = interactorObject.GetComponent<CharacterBody>();
            if (interactorBody == null) return;           
            if (interactorBody.inventory.GetItemCount(this.ItemsDef) > 0) //can proc on picking up items, if i decide to fix this look into interactionprocfilter i guess
            {
                int i = UnityEngine.Random.RandomRangeInt(0, 99);
                if (i <= 9) //5/100
                {
                    EggReward(interactableObject);
                };
            }         
        }


        private void EggOnDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);
            if (damageReport.attackerBody == null) return;
            if (damageReport.attackerBody.inventory == null) return;
            if (damageReport.attackerBody.inventory.GetItemCount(this.ItemsDef) > 0)
            {
                int i = UnityEngine.Random.RandomRangeInt(0, 99);
                if (i <= 2) //1/100
                {
                    CharacterBody victim = damageReport.victimBody;
                    EggReward(victim);
                }
            }
        }

        private void EggReward(CharacterBody body)
        {
            PickupIndex pickupIndex = PickupIndex.none;
            GenerateWeightedSelection();
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            pickupIndex = PickupDropTable.GenerateDropFromWeightedSelection(rng, weightedSelection);
            dropletOrigin = body.gameObject.transform;
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position, Vector3.zero);
        }
        private void EggReward(GameObject interactableObject)
        {
            PickupIndex pickupIndex = PickupIndex.none;
            GenerateWeightedSelection();
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            pickupIndex = PickupDropTable.GenerateDropFromWeightedSelection(rng, weightedSelection);
            dropletOrigin = interactableObject.transform;
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position + (dropletOrigin.forward * 3f) + (dropletOrigin.up * 3f), dropletOrigin.forward * 3f + dropletOrigin.up * 5f);
        }


        private void GenerateWeightedSelection()
        {
            weightedSelection = new WeightedSelection<PickupIndex>();
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(Egg.instance.ItemsDef.itemIndex), 1f);
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex("LunarCoin.Coin0"), 0.1f);//make golden egg later
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation1 = new ItemDef.Pair()
            {
                itemDef1 = DLC1Content.Items.RegeneratingScrap, //consumes regen scrap
                itemDef2 = Egg.instance.ItemsDef
            };
            ItemDef.Pair transformation2 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Infusion, //consumes infusion
                itemDef2 = Egg.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation1);
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation2);
            orig();
        }

        WeightedSelection<PickupIndex> weightedSelection;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
    }
}
