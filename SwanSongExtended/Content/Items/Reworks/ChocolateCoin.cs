using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;
using static SwanSongExtended.Modules.HitHooks;


namespace SwanSongExtended.Items
{
    class ChocolateCoin : ItemBase<ChocolateCoin>
    {

        public override string ConfigName => "Items : Chocolate Coin";
        GameObject chocolate;
        float gravRadius = 4f;
        int fruitChanceBase = 9;
        int goldBase = 1;
        int goldStack = 2;
        float healFraction = 0.00f;
        float healFlatBase = 5f;
        float healFlatStack = 5f;
        float chocolateLifetime = 10f;

        public override string ItemName => "Chocolate Coin";

        public override string ItemLangTokenName => "CHOCYCOIN";

        public override string ItemPickupDesc => "Chance on hit to spawn a chocolate coin for gold and healing.";

        public override string ItemFullDescription => "yum";

        public override string ItemLore => "don't eat the wrapping!";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.Utility };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GoldOnHurt/PickupRollOfPennies.prefab").WaitForCompletion();//Resources.Load<GameObject>("prefabs/pickupmodels/PickupGoldOnHurt");

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/GoldOnHurt/texRollOfPenniesIcon.png").WaitForCompletion();//Resources.Load<Sprite>("textures/itemicons/texGoldOnHurtIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Init()
        {
            CreateChocolate();
            base.Init();
        }
        public override void Hooks()
        {
            GetHitBehavior += ChocolateCoinOnHit;
        }

        private void CreateChocolate()
        {
            chocolate = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Tooth/HealPack.prefab").WaitForCompletion().InstantiateClone("Chocolate", true);

            TeamFilter teamFilter = chocolate.GetComponent<TeamFilter>();

            HealthPickup healthPickup = chocolate.GetComponentInChildren<HealthPickup>();
            if (healthPickup)
            {
                MoneyPickup chocolateMoney = healthPickup.gameObject.AddComponent<MoneyPickup>();
                chocolateMoney.baseGoldReward = 1;
                chocolateMoney.shouldScale = false;
                chocolateMoney.teamFilter = teamFilter;
            }

            GravitatePickup gravPickup = chocolate.GetComponentInChildren<GravitatePickup>();
            if (gravPickup != null)
            {
                gravPickup.gravitateAtFullHealth = true;
                Collider gravitateTrigger = gravPickup.gameObject.GetComponent<Collider>();
                if (gravitateTrigger.isTrigger)
                {
                    gravitateTrigger.transform.localScale = Vector3.one * gravRadius;
                }
            }
            else
            {
                Debug.Log("No gravitatepickup");
            }

            DestroyOnTimer destroyTimer = chocolate.GetComponentInChildren<DestroyOnTimer>();
            if (destroyTimer)
            {
                destroyTimer.duration = chocolateLifetime;
                BeginRapidlyActivatingAndDeactivating braad = chocolate.GetComponent<BeginRapidlyActivatingAndDeactivating>();
                if (braad)
                {
                    braad.delayBeforeBeginningBlinking = chocolateLifetime - 2f;
                }
            }

            ParticleSystemRenderer[] psrs = chocolate.GetComponentsInChildren<ParticleSystemRenderer>();
            for(int i = 0; i < psrs.Length; i++)
            {
                ParticleSystemRenderer psr = psrs[i];
                string name = psr.gameObject.name;
                Log.Error(name);
                Color32 color = Color.white;
                string matName = "";
                if (name == "Core")
                {
                    matName = "matCholocateTrail";
                    color = new Color32(62, 37, 0, 255);
                }
                if (name == "PulseGlow")
                {
                    matName = "matChocolateGlow";
                    color = new Color32(79, 46, 0, 255);
                }

                if(matName != "")
                {
                    Material mat = UnityEngine.Object.Instantiate(psr.material);
                    psr.material = mat;
                    mat.name = matName;
                    mat.DisableKeyword("VERTEXCOLOR");
                    mat.SetFloat("_VertexColorOn", 0);
                    mat.SetColor("_TintColor", color);
                }
            }

            /*Transform core = chocolate.transform.Find("Core");
            if (core)
            {
                Log.Error("uuuu");
                ParticleSystemRenderer psr = core.GetComponent<ParticleSystemRenderer>();
                if (psr)
                {
                    Log.Error("asdjjsdfjsd");
                    Material mat = UnityEngine.Object.Instantiate(psr.material);
                    mat.name = "matChocolateTrail";
                    mat.DisableKeyword("VERTEXCOLOR");
                    mat.SetFloat("_VertexColorOn", 0);
                    mat.SetColor("_TintColor", new Color32(62, 37, 0, 255));
                    psr.material = mat;
                }
            }
            else
            {
                Log.Error("No Core Glow");
            }
            Transform pulseGlow = chocolate.transform.Find("PulseGlow");
            if (pulseGlow)
            {
                ParticleSystemRenderer psr = pulseGlow.GetComponent<ParticleSystemRenderer>();
                if (psr)
                {
                    Material mat = UnityEngine.Object.Instantiate(psr.material);
                    mat.name = "matChocolateGlow";
                    mat.DisableKeyword("VERTEXCOLOR");
                    mat.SetFloat("_VertexColorOn", 0);
                    mat.SetColor("_TintColor", new Color32(79, 46, 0, 255));
                    psr.material = mat;
                }
            }*/

            Content.AddNetworkedObjectPrefab(chocolate);
        }

        private void ChocolateCoinOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (!NetworkServer.active)
                return;

            int itemCount = GetCount(attackerBody);
            if(itemCount <= 0)
            {
                return;
            }

            float procChance = fruitChanceBase * damageInfo.procCoefficient;// Util.ConvertAmplificationPercentageIntoReductionPercentage(fruitChanceBase * itemCount * damageInfo.procCoefficient);
            if(Util.CheckRoll(procChance, attackerBody.master))
            {
                GameObject chocolateInstance = UnityEngine.Object.Instantiate<GameObject>(chocolate, damageInfo.position + UnityEngine.Random.insideUnitSphere * victimBody.radius * 0.5f, UnityEngine.Random.rotation); //stolen from chef which was stolen from rex lmao
                TeamFilter chocolateInstanceTeamFilter = chocolateInstance.GetComponent<TeamFilter>();
                if (chocolateInstanceTeamFilter)
                {
                    chocolateInstanceTeamFilter.teamIndex = attackerBody.teamComponent.teamIndex;
                }
                HealthPickup chocolatePickup = chocolateInstance.GetComponentInChildren<HealthPickup>();
                if (chocolatePickup)
                {
                    chocolatePickup.fractionalHealing = healFraction;
                    chocolatePickup.flatHealing = healFlatBase + healFlatStack * (itemCount - 1);
                }
                MoneyPickup chocolateGold = chocolateInstance.GetComponent<MoneyPickup>();
                if (chocolateGold)
                {
                    chocolateGold.baseGoldReward = goldBase + goldStack * (itemCount - 1);
                }
                NetworkServer.Spawn(chocolateInstance);
            }
        }
    }
}
