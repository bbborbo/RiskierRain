using BepInEx;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static MoreStats.StatHooks;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;

using RoR2.Items;
using UnityEngine.AddressableAssets;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes.ReworksNew
{
    public class HappyMask : ReworkBase<HappyMask>
    {
        public static BuffDef hauntDebuff;
        public static GameObject hauntEffectPrefab;
        public static float hauntExecutionThreshold = 0.25f;
        float ghostSpawnChanceOnExecute = 100;
        float ghostDurationPerStack = 6.66f;
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_GhostOnKill.GhostOnKill_asset;

        public override string ItemName => "Happiest Mask";

        public override string ItemPickupDesc => "Haunt nearby enemies, marking them for execution. Executing enemies summons a ghost.";

        public override string ItemFullDesc =>
            $"<style=cIsDamage>Haunt</style> a random non-boss enemy, marking them for Execution " +
            $"below <style=cIsHealth>{Tools.ConvertDecimal(hauntExecutionThreshold)}</style> health. " +
            $"Execution <style=cIsDamage>spawns a ghost</style> of the killed enemy with <style=cIsDamage>1500%</style> damage, " +
            $"lasting for <style=cIsDamage>{ghostDurationPerStack}s</style> <style=cStack>(+{ghostDurationPerStack}s per stack)</style> " +
            $"{UtilityColor("(double for Haunted enemies)")}.";

        public override void Init()
        {
            hauntDebuff = Content.CreateAndAddBuff(
                "bdHappiestMaskHauntDebuff",
                Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion(), //replace me
                new Color(0.9f, 0.7f, 1.0f),
                false,
                true);
            hauntDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;

            GameObject deathMarkVisualEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathMark/DeathMarkEffect.prefab").WaitForCompletion();
            hauntEffectPrefab = PrefabAPI.InstantiateClone(deathMarkVisualEffect, "HauntVisualEffect");
            base.Init();
        }
        public override void Hooks()
        {
            GetMoreStatCoefficients += MaskExecution;
            GlobalEventManager.onServerCharacterExecuted += HappiestMaskGhostSpawn;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += RevokeHappiestMaskRights;
        }

        private void MaskExecution(CharacterBody sender, MoreStatHookEventArgs args)
        {
            bool hasHauntBuff = sender.HasBuff(HappyMask.hauntDebuff);
            args.ModifyBaseExecutionThreshold(hauntExecutionThreshold, hasHauntBuff);
        }

        private void HappiestMaskGhostSpawn(DamageReport damageReport, float executionHealthLost)
        {
            CharacterBody victimBody = damageReport.victimBody;
            CharacterBody attackerBody = damageReport.attackerBody;
            if (victimBody && attackerBody)
            {
                Inventory inventory = attackerBody.inventory;
                if (inventory)
                {
                    int maskCount = inventory.GetItemCountEffective(RoR2Content.Items.GhostOnKill);
                    if (maskCount > 0 && victimBody && Util.CheckRoll(ghostSpawnChanceOnExecute, attackerBody.master))
                    {
                        if (victimBody.HasBuff(HappyMask.hauntDebuff))
                            maskCount *= 2;
                        Util.TryToCreateGhost(victimBody, attackerBody, Mathf.CeilToInt(maskCount * ghostDurationPerStack));
                    }
                }
            }
        }

        private void RevokeHappiestMaskRights(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "GhostOnKill"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective))
                );
            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Mul);
        }
    }

    public class HappiestMaskBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => RoR2Content.Items.GhostOnKill;
        public static float baseHauntRadius = 35;
        public static float baseHauntInterval = 10;
        float hauntStopwatch = 0;
        private void FixedUpdate()
        {
            hauntStopwatch += Time.fixedDeltaTime;
            if (hauntStopwatch >= baseHauntInterval)
            {
                hauntStopwatch -= baseHauntInterval;
                if (NetworkServer.active)
                {
                    SphereSearch sphereSearch = new SphereSearch
                    {
                        mask = LayerIndex.entityPrecise.mask,
                        origin = body.transform.position,
                        queryTriggerInteraction = QueryTriggerInteraction.Collide,
                        radius = baseHauntRadius
                    };

                    TeamMask teamMask = TeamMask.AllExcept(body.teamComponent.teamIndex);
                    List<HurtBox> hurtBoxesList = new List<HurtBox>();

                    sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(hurtBoxesList);

                    int hurtBoxCount = hurtBoxesList.Count;
                    while (hurtBoxCount > 0)
                    {
                        int i = UnityEngine.Random.Range(0, hurtBoxCount - 1);
                        HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
                        CharacterBody enemyBody = healthComponent.body;

                        if (enemyBody.isBoss || !enemyBody)
                        {
                            hurtBoxesList.Remove(hurtBoxesList[i]);
                            hurtBoxCount--;
                            continue;
                        }

                        for (int n = 0; n < stack; n++)
                        {
                            enemyBody.AddBuff(HappyMask.hauntDebuff.buffIndex);
                        }
                        break;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            hauntStopwatch = 0;
        }
    }
}
