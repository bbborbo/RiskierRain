using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class MiniAmethyst : ItemBase<MiniAmethyst>
    {
        public override bool isEnabled => true;
        public static float equipmentCooldownFractionToGiveAsRecharge = 0.10f;
        public static float minRechargePerAbility = 2f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Amethyst Fragment";

        public override string ItemLangTokenName => "MINIAMETHYST";

        public override string ItemPickupDesc => "Activating your Equipment reduces your ability cooldowns.";

        public override string ItemFullDescription => $"Activating your Equipment reduces <style=cIsUtility>all ability cooldowns</style> " +
            $"by {UtilityColor(equipmentCooldownFractionToGiveAsRecharge.AsPercent())} {StackText("+" + equipmentCooldownFractionToGiveAsRecharge.AsPercent())} of your Equipment's base cooldown.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.EquipmentRelated, ItemTag.AIBlacklist };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            EquipmentSlot.onServerEquipmentActivated += AmethystOnEquipUse;
            RoR2.Stage.onStageStartGlobal += AmethystBarrelSpawn;
        }

        private void AmethystBarrelSpawn(Stage currentStage)
        {
            if (!Run.instance || !NetworkServer.active)
                return;

            SceneDef currentScene = currentStage.sceneDef;
            if (currentScene.sceneType == SceneType.Intermission
                || currentScene.sceneType == SceneType.Cutscene
                || currentScene.sceneType == SceneType.Junk)
                return;

            foreach(NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                CharacterMaster master = null;
                if (user.isLocalPlayer && user.masterObject)
                {
                    master = user.masterObject.GetComponent<CharacterMaster>();
                }
                if(GetCount(master) > 0)
                {
                    GameObject t = master.GetBodyObject() ?? master.gameObject;
                    SpawnEquipmentBarrelNearby(t.transform);
                }
            }

            void SpawnEquipmentBarrelNearby(Transform t)
            {
                Xoroshiro128Plus rng = Run.instance.stageRng;
                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    placementMode =
                        SceneInfo.instance && SceneInfo.instance.approximateMapBoundMesh
                            ? DirectorPlacementRule.PlacementMode.RandomNormalized
                            : DirectorPlacementRule.PlacementMode.Random,
                    spawnOnTarget = t,
                    minDistance = 5f,
                    maxDistance = 10f
                };

                InteractableSpawnCard spawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_EquipmentBarrel.iscEquipmentBarrel_asset).WaitForCompletion();
                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, rng);

                GameObject pillarObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
                //if (pillarObject)
                //{
                //    createdPillarObjects.Add(pillarObject);
                //    pillarTypeSpawnCount[pillarIndex]++;
                //}
            }
        }

        private void AmethystOnEquipUse(EquipmentSlot activator, EquipmentIndex equipment)
        {
            CharacterBody body = activator.characterBody;
            Inventory inv = activator.inventory;
            if(body && inv)
            {
                int amethystCount = GetCount(inv);
                if(amethystCount > 0)
                {
                    float baseEquipCd = EquipmentCatalog.GetEquipmentDef(equipment).cooldown;
                    float recharge = baseEquipCd * equipmentCooldownFractionToGiveAsRecharge * amethystCount;
                    float totalRecharge = 0;
                    int totalAbilities = 0;

                    SkillLocator skillLocator = body.skillLocator;
                    if(skillLocator != null)
                    {
                        //foreach(GenericSkill skill in skillLocator.AllSkills)
                        //{
                        //    float? overflow = GetOverflowFromSkillSlot(skill, recharge);
                        //    if (overflow != null)
                        //    {
                        //        totalRecharge += recharge + overflow.Value;
                        //        totalAbilities++;
                        //    }
                        //}
                        //
                        //float delta = Mathf.Max(totalRecharge / (float)totalAbilities, minRechargePerAbility * amethystCount);
                        //float delta = Mathf.Max(totalRecharge / (float)totalAbilities, minRechargePerAbility * amethystCount);
                        skillLocator.DeductCooldownFromAllSkillsServer(recharge);
                    }
                }
            }

            float? GetOverflowFromSkillSlot(GenericSkill skill, float recharge)
            {
                SkillDef def = skill.skillDef;
                if (def.baseRechargeInterval == 0 || def.rechargeStock == 0)
                    return null;
                if (def.stockToConsume == 0 && def.baseMaxStock == 0)
                    return null;
                float totalCooldownRemaining = skill.cooldownRemaining + (skill.maxStock - skill.stock) * skill.finalRechargeInterval;
                return Mathf.Max(recharge - totalCooldownRemaining, 0);
            }
        }
    }
}
