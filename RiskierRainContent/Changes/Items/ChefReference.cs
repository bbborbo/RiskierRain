using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RiskierRainContent.BurnStatHook;
using static RiskierRainContent.CoreModules.StatHooks;

namespace RiskierRainContent.Items
{
    class ChefReference : ItemBase<ChefReference>
    {
        GameObject meatChunk;
        int fruitChanceBase = 2;
        int fruitChanceStack = 2;
        int maxBurnStacksBase = 8;
        int maxBurnStacksStack = 3;
        int meatNuggets = 2;
        float healFraction = 0.00f;
        float healFlat = 8f;
        float chunkLifetime = 8f; //20

        public override string ItemName => "Chef \u2019Stache";

        public override string ItemLangTokenName => "CHEFITEM";

        public override string ItemPickupDesc => "Burning enemies drop chunks of healing meat.";

        public override string ItemFullDescription => 
            $"Gain <style=cIsDamage>{RiskierRainContent.stacheBurnChance}% ignite chance</style>. " +
            $"Hitting burning enemies has a <style=cIsDamage>{fruitChanceBase}%</style> chance " +
            $"<style=cStack>(+{fruitChanceStack}% per stack)</style> to create {meatNuggets} " +
            $"<style=cIsHealing>healing nuggets</style> that restore <style=cIsHealing>{healFlat} HP</style>. " +
            $"Nugget chance increases <style=cIsDamage>per stack of burn</style>, " +
            $"up to <style=cIsDamage>{maxBurnStacksBase}</style> times.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlChefStache.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_CHEFITEM.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            BurnStatCoefficient += AddBurnChance;
            GetHitBehavior += MeatOnHit;
        }

        private void MeatOnHit(CharacterBody aBody, DamageInfo damageInfo, GameObject victim)
        {
            CharacterBody vBody = victim.GetComponent<CharacterBody>();
            if (vBody)
            {
                int itemCount = GetCount(aBody);
                int burnCount = RiskierRainContent.GetBurnCount(vBody);
                if(itemCount > 0 && burnCount > 0)
                {
                    float procChancePerBurn = fruitChanceBase + fruitChanceStack * (itemCount - 1);
                    float totalProcChance = procChancePerBurn * Mathf.Min(burnCount, maxBurnStacksBase);
                    float endProcChance = Util.ConvertAmplificationPercentageIntoReductionPercentage(totalProcChance) * damageInfo.procCoefficient;
                    //Debug.LogWarning("Chef Meat Chance: " + endProcChance);

                    if(Util.CheckRoll(endProcChance, aBody.master))
                    {
                        //this is literally just rex fruit code
                        /*EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TreebotFruitDeathEffect.prefab"), new EffectData
                        {
                            origin = vBody.transform.position,
                            rotation = UnityEngine.Random.rotation
                        }, true);*/
                        for (int i = 0; i < meatNuggets; i++)
                        {
                            GameObject meatInstance = UnityEngine.Object.Instantiate<GameObject>(meatChunk, damageInfo.position + UnityEngine.Random.insideUnitSphere * vBody.radius * 0.5f, UnityEngine.Random.rotation);
                            TeamFilter meatTeamFilter = meatInstance.GetComponent<TeamFilter>();
                            if (meatTeamFilter)
                            {
                                meatTeamFilter.teamIndex = aBody.teamComponent.teamIndex;
                            }
                            meatInstance.GetComponentInChildren<HealthPickup>();
                            meatInstance.transform.localScale = new Vector3(1f, 1f, 1f);
                            NetworkServer.Spawn(meatInstance);
                        }
                    }
                }
            }
        }

        private void AddBurnChance(CharacterBody sender, BurnEventArgs args)
        {
            if(GetCount(sender) > 0)
            {
                args.burnChance += RiskierRainContent.stacheBurnChance;
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
            CreateMeatChunk();
        }

        private void CreateMeatChunk()
        {
            meatChunk = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/TreebotFruitPack.prefab").WaitForCompletion().InstantiateClone("MeatChunk", true);

            HealthPickup healthPickup = meatChunk.GetComponentInChildren<HealthPickup>();
            if(healthPickup)
            {
                healthPickup.fractionalHealing = healFraction;
                healthPickup.flatHealing = healFlat;
            }

            DestroyOnTimer destroyTimer = meatChunk.GetComponentInChildren<DestroyOnTimer>();
            destroyTimer.duration = chunkLifetime;

            Assets.networkedObjectPrefabs.Add(meatChunk);
        }
    }
}
