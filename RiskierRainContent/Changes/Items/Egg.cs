using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Interactables;
using RiskierRainContent.Items;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using HarmonyLib;
using On.RoR2.Items;
using RoR2.ExpansionManagement;

namespace RiskierRainContent.Items
{
    class Egg : ItemBase<Egg>
    {
        public int eggHealth = 4;
        public float eggRegen = 0.2f;
        public float eggDamage = 0.2f;


        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Egg";

        public override string ItemLangTokenName => "EGG";

        public override string ItemPickupDesc => "Slightly increase health, regeneration, and damage. Start an egg hunt. " +
            "<style=cIsVoid>Corrupts most edible and animal matter.</style>";

        public override string ItemFullDescription => $"Gain <style = cIsHealing>{eggHealth} max health, {eggRegen} hp/s regeneration,</style> and <style = cIsDamage>{eggDamage} damage</style>. <style = cIsUtility>Start an egg hunt.</style> " +
            "<style = cIsVoid> Corrupts all Infusion, Bison Steak, and Alien Heads.</style>";

        public override string ItemLore => "this egg is so fuckign yummuy";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.OnStageBeginEffect, ItemTag.OnKillEffect, ItemTag.InteractableRelated, ItemTag.WorldUnique };

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/egg.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texEggIcon.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += EggOnDeath;
            On.RoR2.GlobalEventManager.OnInteractionBegin += EggOnPurchase;//includes uhhhhhh the uh yea. (item pickups)
            On.RoR2.SceneDirector.PopulateScene += HideEggs;
            RecalculateStatsAPI.GetStatCoefficients += EggStats;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }

        private void HideEggs(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self)
        {
            orig(self);
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            int num = 0;
            using (IEnumerator<CharacterMaster> enumerator = CharacterMaster.readOnlyInstancesList.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.inventory.GetItemCount(this.ItemsDef) > 0)
                    {
                        num += 3;
                    }
                }
            }
            for (int j = 0; j < num; j++)
            {
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(EggPile.instance.customInteractable.spawnCard, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Random
                }, rng));
            }
        }

        private void EggStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
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
                if (i <= 6) //5/100
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
                if (i <= 3) //1/100
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
        public void EggReward(GameObject interactableObject)
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
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(GoldenEgg.instance.ItemsDef.itemIndex), 0.1f);
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation2 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Infusion, //consumes infusion
                itemDef2 = Egg.instance.ItemsDef
            };
            ItemDef.Pair transformation3 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.FlatHealth, //consumes meat
                itemDef2 = Egg.instance.ItemsDef
            };
            ItemDef.Pair transformation4 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.AlienHead, //consumes gah
                itemDef2 = Egg.instance.ItemsDef
            };
            /*ItemDef.Pair transformation5 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Seed, //consumes gah
                itemDef2 = Egg.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                 = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation5);*/
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation2);
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                 = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation3);
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem]
                 = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation4);
            orig();
        }

        WeightedSelection<PickupIndex> weightedSelection;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
    }
}
