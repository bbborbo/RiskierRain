using BepInEx;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.RoR2.Items;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain.Items;
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

namespace RiskierRain
{
    partial class RiskierRainPlugin
    {
        float ghostSpawnChanceOnExecute = 100;
        float ghostDurationPerStack = 6.66f;
        public void HappiestMaskRework()
        {
            RoR2.GlobalEventManager.onServerCharacterExecuted += HappiestMaskGhostSpawn;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += RevokeHappiestMaskRights;
            On.RoR2.CharacterBody.OnInventoryChanged += AddMaskBehavior;

            LanguageAPI.Add("ITEM_GHOSTONKILL_PICKUP", "Haunt nearby enemies, marking them for execution. Executing enemies summons a ghost.");
            LanguageAPI.Add("ITEM_GHOSTONKILL_DESC", $"Once every <style=cIsDamage>{HappiestMaskBehavior.baseHauntInterval}</style> seconds, " +
                $"Haunt a nearby non-boss enemy, marking them for Execution " +
                $"below <style=cIsHealth>{Tools.ConvertDecimal(Assets.hauntExecutionThreshold)}</style> health. " +
                $"Execution <style=cIsDamage>spawns a ghost</style> of the killed enemy with <style=cIsDamage>1500%</style> damage, " +
                $"lasting for <style=cIsDamage>{ghostDurationPerStack}s</style> <style=cStack>(+{ghostDurationPerStack}s per stack)</style>.");
        }

        private void AddMaskBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            int maskCount = self.inventory.GetItemCount(RoR2Content.Items.GhostOnKill);
            self.AddItemBehavior<HappiestMaskBehavior>(maskCount);
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
                    int maskCount = inventory.GetItemCount(RoR2Content.Items.GhostOnKill);
                    if (maskCount > 0 && victimBody && Util.CheckRoll(ghostSpawnChanceOnExecute, attackerBody.master))
                    {
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
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Mul);
        }
    }

    public class HappiestMaskBehavior : RoR2.CharacterBody.ItemBehavior
    {
        public static float baseHauntRadius = 35;
        public static float baseHauntInterval = 10;
        float hauntStopwatch = 0;
        private void FixedUpdate()
        {
            hauntStopwatch += Time.fixedDeltaTime;
            if(hauntStopwatch >= baseHauntInterval)
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

                        for(int n = 0; n < stack; n++)
                        {
                            enemyBody.AddBuff(Assets.hauntDebuff.buffIndex);
                        }
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            hauntStopwatch = 0;
        }
    }
}
