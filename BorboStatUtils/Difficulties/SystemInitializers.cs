using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2.Artifacts;
using RoR2.ContentManagement;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RainrotSharedUtils.Difficulties.DifficultyUtilsModule;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace RainrotSharedUtils.Difficulties
{
    public class SystemInitializers
    {
        [SystemInitializer(typeof(CombatDirector))]
        private static void FixEliteSpawn()
        {
            //taking a note from dot scow's elite api fix and forcing the async queue to complete. this is so dumb it rocks lol
            _ = Addressables.LoadAssetAsync<AssetCollection>("28c1ff73384cbbc469eefc6c7ef399dd").WaitForCompletion();
            foreach (CombatDirector.EliteTierDef etd in CombatDirector.eliteTiers) //EliteAPI.VanillaEliteTiers)//
            {
                if (etd.eliteTypes == null || etd.eliteTypes.Length == 0)
                    continue;

                if (etd.eliteTypes.Contains(RoR2Content.Elites.Poison) || etd.eliteTypes.Contains(RoR2Content.Elites.Haunted))
                {
                    //Debug.LogError("Rare Elite");
                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                        rules == SpawnCard.EliteRules.Default
                        && IsPastMinimumStage(true);
                    continue;
                }

                if (etd.eliteTypes.Contains(DLC2Content.Elites.Aurelionite))
                {
                    //Debug.LogError("Uncommon Elite");
                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                        IsPastMinimumStage(false) &&
                        CombatDirector.NotEliteOnlyArtifactActive() && rules == SpawnCard.EliteRules.Default;
                    continue;
                }
                else if (etd.eliteTypes.Contains(RoR2Content.Elites.Fire))
                {
                    //Debug.LogError("Common Elite");
                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                        CombatDirector.NotEliteOnlyArtifactActive() && rules == SpawnCard.EliteRules.Default;
                    continue;
                }

                if (etd.eliteTypes.Contains(DLC2Content.Elites.AurelioniteHonor))
                {
                    //Debug.LogError("Uncommon Elite (Honor)");
                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                        IsPastMinimumStage(false) &&
                        (CombatDirector.IsEliteOnlyArtifactActive() ||
                        (rules == SpawnCard.EliteRules.Default && GetForceNextSpawnAsElite()));
                    continue;
                }
                else if (etd.eliteTypes.Contains(RoR2Content.Elites.FireHonor))
                {
                    //Debug.LogError("Common Elite (Honor)");
                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                        (CombatDirector.IsEliteOnlyArtifactActive() ||
                        (rules == SpawnCard.EliteRules.Default && GetForceNextSpawnAsElite()));
                    continue;
                }
            }

            bool GetForceNextSpawnAsElite()
            {
                return false;// forceNextSpawnAsElite;
            }
            bool IsPastMinimumStage(bool isTier2)
            {
                int minStage = isTier2 ? 4 : 2;

                if (UseDifficultyStats && ValidateCachedDifficultyStats())
                {
                    minStage = (isTier2 ? cachedDifficultyStats.tier2EliteStage : cachedDifficultyStats.tier1AndHalfEliteStage) - 1;
                }
                return Run.instance.stageClearCount >= minStage;
            }
        }
    }
}
