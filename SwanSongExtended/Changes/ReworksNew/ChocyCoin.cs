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
using static MoreStats.OnHit;
using RoR2.Items;
using MonoMod.Cil;
using Mono.Cecil.Cil;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes
{
    public class ChocyCoin : ReworkBase<ChocyCoin>
    {
        public static GameObject chocolate;
        public static float chocolateGravRadius = 4f;
        public static int chocolateChanceBase = 9;
        public static int chocolateGoldRewardBase = 1;
        public static int chocolateGoldRewardStack = 2;
        public static float chocolateHealFraction = 0.00f;
        public static float chocolateHealFlatBase = 5f;
        public static float chocolateHealFlatStack = 5f;
        public static float chocolateLifetime = 10f;

        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_GoldOnHurt.GoldOnHurt_asset;

        public override string ItemName => $"Chocolate Coins";

        public override string ItemPickupDesc => $"Chance on hit to spawn a chocolate coin for gold and healing.";

        public override string ItemFullDesc =>
            $"On hit, gain a {UtilityColor($"{chocolateChanceBase}% chance")} " +
            $"to drop a chocolate coin that heals for " +
            $"{HealingColor(chocolateHealFlatBase.ToString() + " health")} {StackText($"+{chocolateHealFlatStack}")} " +
            $"plus {UtilityColor(chocolateGoldRewardBase.ToString() + " gold")} {StackText($"+{chocolateGoldRewardStack}")}. " +
            $"{UtilityColor("Scales over time")}.";

        public override void Init()
        {
            SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Tooth.HealPack_prefab, CreateChocolate);
            base.Init();
        }
        public override void Hooks()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += RemoveVanillaPenny;
        }
        private static void RemoveVanillaPenny(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.goldOnHurt)));
            if (!b)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(RemoveVanillaPenny));
                return;
            }
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }
        public static void CreateChocolate(GameObject healPack)
        {
            chocolate = healPack.InstantiateClone("Chocolate", true);

            TeamFilter teamFilter = chocolate.GetComponent<TeamFilter>();

            HealthPickup healthPickup = chocolate.GetComponentInChildren<HealthPickup>();
            if (healthPickup)
            {
                MoneyPickup chocolateMoney = healthPickup.gameObject.AddComponent<MoneyPickup>();
                chocolateMoney.baseGoldReward = 1;
                chocolateMoney.shouldScale = false; //we scale it manually
                chocolateMoney.teamFilter = teamFilter;
            }

            GravitatePickup gravPickup = chocolate.GetComponentInChildren<GravitatePickup>();
            if (gravPickup != null)
            {
                gravPickup.gravitateAtFullHealth = true;
                Collider gravitateTrigger = gravPickup.gameObject.GetComponent<Collider>();
                if (gravitateTrigger.isTrigger)
                {
                    gravitateTrigger.transform.localScale = Vector3.one * chocolateGravRadius;
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
            for (int i = 0; i < psrs.Length; i++)
            {
                ParticleSystemRenderer psr = psrs[i];
                string name = psr.gameObject.name;
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

                if (matName != "")
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
    }

    public class ChocyCoinItemBehavior : BaseItemBodyBehavior, IOnDamageDealtServerReceiver
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => DLC1Content.Items.GoldOnHurt;

        public void OnDamageDealtServer(DamageReport damageReport)
        {
            if (stack <= 0
                || damageReport.attackerBody == null
                || damageReport.attackerMaster == null
                || damageReport.victimBody == null)
            {
                return;
            }

            float procChance = ChocyCoin.chocolateChanceBase * damageReport.damageInfo.procCoefficient;// Util.ConvertAmplificationPercentageIntoReductionPercentage(fruitChanceBase * itemCount * damageInfo.procCoefficient);
            if (Util.CheckRoll(procChance, damageReport.attackerMaster))
            {
                GameObject chocolateInstance =
                    UnityEngine.Object.Instantiate<GameObject>(ChocyCoin.chocolate,
                    damageReport.damageInfo.position + UnityEngine.Random.insideUnitSphere * damageReport.victimBody.radius * 0.5f, UnityEngine.Random.rotation); //stolen from chef which was stolen from rex lmao
                TeamFilter chocolateInstanceTeamFilter = chocolateInstance.GetComponent<TeamFilter>();
                if (chocolateInstanceTeamFilter)
                {
                    chocolateInstanceTeamFilter.teamIndex = damageReport.attackerBody.teamComponent.teamIndex;
                }
                HealthPickup chocolatePickup = chocolateInstance.GetComponentInChildren<HealthPickup>();
                if (chocolatePickup)
                {
                    chocolatePickup.fractionalHealing = ChocyCoin.chocolateHealFraction;
                    chocolatePickup.flatHealing = ChocyCoin.chocolateHealFlatBase + ChocyCoin.chocolateHealFlatStack * (stack - 1);
                }
                MoneyPickup chocolateGold = chocolateInstance.GetComponent<MoneyPickup>();
                if (chocolateGold)
                {
                    int baseGold = ChocyCoin.chocolateGoldRewardBase + ChocyCoin.chocolateGoldRewardStack * (stack - 1);
                    chocolateGold.baseGoldReward = baseGold * Mathf.RoundToInt(damageReport.attackerBody.level);//Run.instance.GetDifficultyScaledCost(baseGold, Stage.instance.entryDifficultyCoefficient);
                }
                NetworkServer.Spawn(chocolateInstance);
            }
        }
    }
}
