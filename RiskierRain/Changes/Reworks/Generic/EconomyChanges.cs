using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RiskierRain.CoreModules;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.GivePickupsOnStart;
using static R2API.RecalculateStatsAPI;
using R2API;
using RiskierRain.Components;
using static R2API.DirectorAPI;
using System.Linq;
using On.EntityStates.CaptainSupplyDrop;
using SwanSongExtended;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Stage = RoR2.Stage;
using RainrotSharedUtils.Shelters;
using RainrotSharedUtils.Difficulties;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {

        #region interactable shit
        //code belonds to r2api
        public static void ChangeInteractableWeightForPool(DccsPool pool, string interactableNameLowered, int newWeight, int maxPerStage = -1)
        {
            //Debug.Log($"Changing {interactableNameLowered} card weight!");
            if (pool)
            {
                Helpers.ForEachPoolEntryInDccsPool(pool, (poolEntry) =>
                {
                    for (int i = 0; i < poolEntry.dccs.categories.Length; i++)
                    {
                        var cards = poolEntry.dccs.categories[i].cards.ToList();
                        foreach (DirectorCard card in cards)
                        {
                            SpawnCard spawnCard = card.spawnCard;
                            if (spawnCard.name.ToLowerInvariant() == interactableNameLowered)
                            {
                                card.selectionWeight = newWeight;

                                if (maxPerStage >= 0 && spawnCard is InteractableSpawnCard)
                                {
                                    ((InteractableSpawnCard)spawnCard).maxSpawnsPerStage = maxPerStage;
                                }
                            }
                        }
                        poolEntry.dccs.categories[i].cards = cards.ToArray();
                    }
                });
            }
        }

        //code belonds to r2api
        private static void RemoveExistingInteractable(DccsPool pool, string interactableNameLowered)
        {
            if (pool)
            {
                Helpers.ForEachPoolEntryInDccsPool(pool, (poolEntry) =>
                {
                    for (int i = 0; i < poolEntry.dccs.categories.Length; i++)
                    {
                        var cards = poolEntry.dccs.categories[i].cards.ToList();
                        cards.RemoveAll((card) => card.spawnCard.name.ToLowerInvariant() == interactableNameLowered);
                        poolEntry.dccs.categories[i].cards = cards.ToArray();
                    }
                });
            }
        }
        #endregion
    }
}