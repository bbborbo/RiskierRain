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
using static BorboStatUtils.BorboStatUtils;

namespace RiskierRain
{
    partial class RiskierRainPlugin
    {
        public static float hauntExecutionThreshold = 0.25f;
        float ghostSpawnChanceOnExecute = 100;
        float ghostDurationPerStack = 6.66f;

        public void HappiestMaskRework()
        {
            GetExecutionThreshold += MaskExecution;
            RoR2.GlobalEventManager.onServerCharacterExecuted += HappiestMaskGhostSpawn;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += RevokeHappiestMaskRights;
            On.RoR2.CharacterBody.OnInventoryChanged += AddMaskBehavior;

            LanguageAPI.Add("ITEM_GHOSTONKILL_PICKUP", "Haunt nearby enemies, marking them for execution. Executing enemies summons a ghost.");
            LanguageAPI.Add("ITEM_GHOSTONKILL_DESC", $"Once every <style=cIsDamage>{HappiestMaskBehavior.baseHauntInterval}</style> seconds, " +
                $"Haunt a nearby non-boss enemy, marking them for Execution " +
                $"below <style=cIsHealth>{Tools.ConvertDecimal(hauntExecutionThreshold)}</style> health. " +
                $"Execution <style=cIsDamage>spawns a ghost</style> of the killed enemy with <style=cIsDamage>1500%</style> damage, " +
                $"lasting for <style=cIsDamage>{ghostDurationPerStack}s</style> <style=cStack>(+{ghostDurationPerStack}s per stack)</style>.");
            LanguageAPI.Add("ITEM_GHOSTONKILL_LORE", 
                "<style=cMono>\r\n\u002F\u002F--AUTO-TRANSCRIPTION FROM RALLYPOINT DELTA --\u002F\u002F</style>\r\n\r\n" +
                "\u201CSir, the ghosts are back." +
                "\u201D\r\n\r\nThe man sighed. After a routine expedition, one of the crew members \u2013 a simple soldier " +
                "- had recovered an artifact thought to have been aboard the Contact Light \u2013 " +
                "a simple mask, adorned with a painfully happy grin. " +
                "\r\n\r\n\u201CI\u2019ll take care of it.\u201D The man trudged down the hall towards the barracks. " +
                "The Lemurians he had killed earlier that day walked down the hall by him, barely earning a second glance from the man. " +
                "This had become so commonplace that most of the crew members in this block had grown accustomed to having a ghostly room-mate." +
                "\r\n\r\nBut enough was enough. Stepping through the ghost of an Imp, the man slammed the door open. " +
                "The lights were off, and in the corner sat the soldier." +
                "\r\n\r\n\u201CAlright, we\u2019ve had enough fun playing with the dead. Fork it over." +
                "\u201D\r\n\r\nNo response. The man grunted and hoisted the soldier to his feet, giving him a few rough shakes. " +
                "\u201CHey, can you hear me!? I said hand over the mask! I\u2019m tired of waking up next to Beetles, so give it a rest already--\u201D" +
                "\r\n\r\nThe soldier\u2019s limp body moved. Slowly, the soldier raised his finger \u2013 pointing directly at the man." +
                "\r\n\r\n\u201CWhat are you...?\u201D With a sense of dread, the man turned and saw the Lemurians he had killed earlier step into the room. " +
                "Their mouths began to glow with an otherworldly light." +
                "\r\n\r\nThe man cursed under his breath as he loaded his shotgun. \u201CThis planet, I tell you...\u201D");
        }

        private void MaskExecution(CharacterBody sender, float executeThreshold)
        {
            bool hasHauntBuff = sender.HasBuff(Assets.hauntDebuff);
            executeThreshold = ModifyExecutionThreshold(ref executeThreshold, hauntExecutionThreshold, hasHauntBuff);
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
