using BepInEx.Configuration;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SwanSongExtended.BurnStatHook;
using static SwanSongExtended.Modules.HitHooks;

namespace SwanSongExtended.Items
{
    class ChefReference : ItemBase<ChefReference>
    {
        public override string ConfigName => "Items : Chef Stache";
        GameObject meatChunk;
        int fruitChanceBase = 1;
        int fruitChanceStack = 1;
        int maxBurnStacksBase = 10;
        int maxBurnStacksStack = 3;
        int meatNuggets = 2;
        float healFraction = 0.00f;
        float healFlat = 14f;
        float chunkLifetime = 5f; //20

        public override string ItemName => "Chef \u2019Stache";

        public override string ItemLangTokenName => "CHEFITEM";

        public override string ItemPickupDesc => "Burning enemies drop chunks of healing meat.";

        public override string ItemFullDescription => 
            $"Gain <style=cIsDamage>{SwanSongPlugin.stacheBurnChance}% ignite chance</style>. " +
            $"Hitting burning enemies has a <style=cIsDamage>{fruitChanceBase}%</style> chance " +
            $"<style=cStack>(+{fruitChanceStack}% per stack)</style> to create {meatNuggets} " +
            $"<style=cIsHealing>healing nuggets</style> that restore <style=cIsHealing>{healFlat} HP</style>. " +
            $"Nugget chance increases <style=cIsDamage>per stack of burn</style>, " +
            $"up to <style=cIsDamage>{maxBurnStacksBase}</style> times.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlChefStache.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_CHEFITEM.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            CreateMeatChunk();
            base.Init();
        }

        public override void Hooks()
        {
            BurnStatCoefficient += AddBurnChance;
            GetHitBehavior += MeatOnHit;
        }
        private void CreateMeatChunk()
        {
            meatChunk = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/TreebotFruitPack.prefab").WaitForCompletion().InstantiateClone("MeatChunk", true);

            HealthPickup healthPickup = meatChunk.GetComponentInChildren<HealthPickup>();
            if (healthPickup)
            {
                healthPickup.fractionalHealing = healFraction;
                healthPickup.flatHealing = healFlat;
            }

            DestroyOnTimer destroyTimer = meatChunk.GetComponentInChildren<DestroyOnTimer>();
            destroyTimer.duration = chunkLifetime;

            Content.AddNetworkedObjectPrefab(meatChunk);
        }

        private void MeatOnHit(CharacterBody aBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            int itemCount = GetCount(aBody);
            int burnCount = SwanSongPlugin.GetBurnCount(victimBody);
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
                        GameObject meatInstance = UnityEngine.Object.Instantiate<GameObject>(meatChunk, damageInfo.position + UnityEngine.Random.insideUnitSphere * victimBody.radius * 0.5f, UnityEngine.Random.rotation);
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

        private void AddBurnChance(CharacterBody sender, BurnEventArgs args)
        {
            if(GetCount(sender) > 0)
            {
                args.burnChance += SwanSongPlugin.stacheBurnChance;
            }
        }
    }
}
