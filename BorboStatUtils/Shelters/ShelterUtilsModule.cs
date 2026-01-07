using EntityStates;
using RoR2;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RainrotSharedUtils.Shelters
{
    public static class ShelterUtilsModule
    {
        public static bool UseGlobalShelters = false;
        public static bool UseCustomShelters = false;

        #region interfacing
        public static bool IsBodySuperSheltered(CharacterBody body, float radius = 0)
        {
            foreach (ShelterProviderBehavior shelter in ShelterProviderBehavior.readOnlyInstancesList)
            {
                if (!shelter.isSuperShelter)
                    continue;
                if (shelter.IsInBounds(body.corePosition, radius))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsPositionSuperSheltered(Vector3 position, float radius = 0)
        {
            foreach (ShelterProviderBehavior shelter in ShelterProviderBehavior.readOnlyInstancesList)
            {
                if (!shelter.isSuperShelter)
                    continue;
                if (shelter.IsInBounds(position, radius))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsBodySheltered(CharacterBody body, float radius = 0)
        {
            foreach (ShelterProviderBehavior shelter in ShelterProviderBehavior.readOnlyInstancesList)
            {
                bool isHazard = shelter.isHazardZone;
                //if shelter is [void seed fog bubble], then ignore (these are processed independently)
                if (isHazard)
                    continue;
                //is inside a shelter or outside an inverted shelter
                if (shelter.IsInBounds(body.corePosition, radius))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsPositionSheltered(Vector3 position, float radius = 0)
        {
            foreach (ShelterProviderBehavior shelter in ShelterProviderBehavior.readOnlyInstancesList)
            {
                bool isHazard = shelter.isHazardZone;
                //if filtering for [storms] and shelter is [void seed fog bubble], then skip
                if (isHazard)
                    continue;
                //is inside a shelter or outside an inverted shelter
                if (shelter.IsInBounds(position, radius))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        public static void Init()
        {
            On.RoR2.TeleporterInteraction.Awake += SheltersOnTeleporterAwake;
            On.RoR2.HoldoutZoneController.Awake += SheltersOnHoldoutAwake;
            On.RoR2.SphereZone.OnEnable += SheltersOnSphereZoneEnable;
            On.RoR2.VerticalTubeZone.OnEnable += SheltersOnTubeZoneEnable;

            //IL.RoR2.FogDamageController.
            On.RoR2.FogDamageController.EvaluateTeam += EvaluateShelteredTeam;
            //related to voidlings final stand, could disable this
            On.RoR2.FogDamageController.GetAffectedBodiesOnTeam += GetFogAffectedBodies;

            //halcyon
            GameObject halcyoniteShrinePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/ShrineHalcyonite.prefab").WaitForCompletion();
            if (halcyoniteShrinePrefab)
            {
                HalcyoniteShrineInteractable hsi = halcyoniteShrinePrefab.GetComponent<HalcyoniteShrineInteractable>();
                if (hsi)
                {
                    ShelterProviderBehavior shelter = halcyoniteShrinePrefab.AddComponent<ShelterProviderBehavior>();
                    shelter.fallbackRadius = hsi.radius;
                }
            }

            //mockshelters
            On.RoR2.HalcyoniteShrineInteractable.DrainConditionMet += MockShelter_Halcyon;
            On.RoR2.TeleporterInteraction.ChargingState.OnExit += MockShelter_TP;

            RoR2.Run.onRunDestroyGlobal += SheltersOnRunDestroy;
        }


        private static void MockShelter_TP(On.RoR2.TeleporterInteraction.ChargingState.orig_OnExit orig, TeleporterInteraction.ChargingState self)
        {
            HoldoutZoneController zone = self.teleporterInteraction.holdoutZoneController;
            float radius = zone.currentRadius;
            GameObject indicator = zone.radiusIndicator.gameObject;
            MakeMockShelter(indicator, radius, 25f);

            orig(self);
        }

        private static void MockShelter_Halcyon(On.RoR2.HalcyoniteShrineInteractable.orig_DrainConditionMet orig, RoR2.HalcyoniteShrineInteractable self)
        {
            float radius = self.radius;
            //self.shrineHalcyoniteBubble.SetActive(true);
            GameObject indicator = self.shrineHalcyoniteBubble.gameObject;
            //self.shrineHalcyoniteBubble.SetActive(false);
            MakeMockShelter(indicator, radius, 15f, stupidBullshit: 2);

            orig(self);
        }

        public static void MakeMockShelter(GameObject indicator, float startRadius, float endRadius, float stupidBullshit = 1)
        {
            if (!UseGlobalShelters)
                return;
            GameObject mockShelter = new GameObject();
            mockShelter.name = "MockShelter";
            mockShelter.transform.position = indicator.transform.position;
            MockShelterComponent shelterComponent = mockShelter.AddComponent<MockShelterComponent>();
            shelterComponent.startingRadius = startRadius;
            shelterComponent.endRadius = endRadius;
            shelterComponent.scaleMultiplier = stupidBullshit;

            if (indicator == null)
                return;
            GameObject newIndicator = GameObject.Instantiate(indicator, mockShelter.transform);
            if (!newIndicator.activeInHierarchy)
                newIndicator.SetActive(true);
            newIndicator.transform.parent = mockShelter.transform;
            shelterComponent.areaIndicatorReference = newIndicator;
            if (newIndicator.TryGetComponent<ShelterProviderBehavior>(out ShelterProviderBehavior component))
            {
                component.enabled = false;
            }
        }

        private static void EvaluateShelteredTeam(On.RoR2.FogDamageController.orig_EvaluateTeam orig, FogDamageController self, TeamIndex teamIndex)
        {
            if (!UseCustomShelters && !UseGlobalShelters)
            {
                orig(self, teamIndex);
                return;
            }
            foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(teamIndex))
            {
                CharacterBody body = teamComponent.body;
                bool bodyHasStacks = self.characterBodyToStacks.ContainsKey(body);
                //this (below) is the only line different from orig, IsBodySheltered instead of false
                bool bodyIsSheltered = IsBodySheltered(body);
                bool bodyHasCooldown = body.HasBuff(RoR2Content.Buffs.VoidFogStackCooldown);
                if (!bodyIsSheltered)
                {
                    using (List<IZone>.Enumerator enumerator2 = self.safeZones.GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            if (enumerator2.Current.IsInBounds(teamComponent.transform.position))
                            {
                                bodyIsSheltered = true;
                                break;
                            }
                        }
                    }
                }
                if (bodyIsSheltered)
                {
                    if (bodyHasStacks)
                    {
                        self.characterBodyToStacks.Remove(body);
                        if (bodyHasCooldown)
                        {
                            body.RemoveOldestTimedBuff(RoR2Content.Buffs.VoidFogStackCooldown);
                        }
                    }
                }
                else if (!bodyHasStacks)
                {
                    self.characterBodyToStacks.Add(body, 1);
                    self.DumpArenaDamageInfo(body);
                    body.AddTimedBuff(RoR2Content.Buffs.VoidFogStackCooldown, self.healthFractionRampIncreaseCooldown);
                }
                else if (!bodyHasCooldown)
                {
                    Dictionary<CharacterBody, int> dictionary = self.characterBodyToStacks;
                    CharacterBody key = body;
                    dictionary[key]++;
                    self.DumpArenaDamageInfo(body);
                    body.AddTimedBuff(RoR2Content.Buffs.VoidFogStackCooldown, self.healthFractionRampIncreaseCooldown);
                }
            }
        }

        private static IEnumerable<CharacterBody> GetFogAffectedBodies(On.RoR2.FogDamageController.orig_GetAffectedBodiesOnTeam orig, FogDamageController self, TeamIndex teamIndex)
        {
            IEnumerable<CharacterBody> affectedBodies = orig(self, teamIndex);

            return affectedBodies.Where(body => !ShelterUtilsModule.IsBodySheltered(body));
        }

        private static ShelterProviderBehavior AddShelterProvider(GameObject obj, IZone zone, bool inverted = false, float radius = 0)
        {
            ShelterProviderBehavior shelter = obj.GetComponent<ShelterProviderBehavior>();
            if (!shelter)
            {
                shelter = obj.AddComponent<ShelterProviderBehavior>();
                shelter.isHazardZone = inverted;
            }
            shelter.zoneBehavior = zone;
            shelter.fallbackRadius = radius;
            return shelter;
        }

        private static void SheltersOnRunDestroy(Run obj)
        {
            ReadOnlyCollection<ShelterProviderBehavior> allShelterInstances = ShelterProviderBehavior.readOnlyInstancesList;
            for (int i = allShelterInstances.Count - 1; i >= 0; i--)
            {
                if (allShelterInstances[i])
                {
                    UnityEngine.Object.Destroy(allShelterInstances[i].gameObject);
                }
            }
        }

        #region zones to shelters
        private static void SheltersOnTeleporterAwake(On.RoR2.TeleporterInteraction.orig_Awake orig, TeleporterInteraction self)
        {
            orig(self);
            if (!UseGlobalShelters)
                return;

            ShelterProviderBehavior shelter = AddShelterProvider(self.gameObject, self.holdoutZoneController as IZone, 
                radius: self.holdoutZoneController.baseRadius);
            shelter.holdoutZoneController = self.holdoutZoneController;
            shelter.isSuperShelter = true;
        }
        private static void SheltersOnTubeZoneEnable(On.RoR2.VerticalTubeZone.orig_OnEnable orig, VerticalTubeZone self)
        {
            orig(self);
            if (!UseGlobalShelters)
                return;
            AddShelterProvider(self.gameObject, self as IZone, radius: self.radius);
        }

        private static void SheltersOnSphereZoneEnable(On.RoR2.SphereZone.orig_OnEnable orig, SphereZone self)
        {
            orig(self);
            if (!UseGlobalShelters)
                return;
            AddShelterProvider(self.gameObject, self as IZone, self.isInverted, radius: self.radius);
        }

        private static void SheltersOnHoldoutAwake(On.RoR2.HoldoutZoneController.orig_Awake orig, HoldoutZoneController self)
        {
            orig(self);
            if (!UseGlobalShelters)
                return;
            ShelterProviderBehavior shelter = AddShelterProvider(self.gameObject, self as IZone, radius: self.baseRadius);
            shelter.holdoutZoneController = self;
        }
        #endregion
    }
}
