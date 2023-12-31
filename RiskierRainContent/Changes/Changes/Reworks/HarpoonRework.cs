using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RiskierRainContent.CoreModules.StatHooks;

namespace RiskierRainContent
{
    public partial class RiskierRainContent
    {
        public static float harpoonBarrierBase = 8;
        public static float harpoonBarrierStack = 4;
        public static float harpoonTargetTime = 15;
        public void HuntersHarpoonRework()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath += RevokeHarpoonRights;
            On.RoR2.CharacterBody.OnInventoryChanged += AddHarpoonBehavior;
            GetHitBehavior += HarpoonOnHit;
            LanguageAPI.Add("ITEM_MOVESPEEDONKILL_PICKUP", "Target a nearby enemy, gaining barrier on hit.");
            LanguageAPI.Add("ITEM_MOVESPEEDONKILL_DESC", $"Once every <style=cIsDamage>{harpoonTargetTime}</style> seconds, <style=cIsDamage>target</style> a random enemy. " +
                $"Attacking the targeted enemy grants a <style=cIsHealing>temporary barrier</style> " +
                $"for <style=cIsHealing>{harpoonBarrierBase} health</style> <style=cStack>(+{harpoonBarrierStack} per stack)</style>.");
        }

        private void HarpoonOnHit(CharacterBody attackerBody, DamageInfo damageInfo, GameObject victim)
        {
            CharacterBody victimBody = victim.GetComponent<CharacterBody>();
            Inventory inv = attackerBody.inventory;
            HealthComponent hc = attackerBody.healthComponent;
            if (inv != null && hc != null && victimBody != null && victimBody.HasBuff(Assets.harpoonDebuff))
            {
                int harpoonCount = inv.GetItemCount(DLC1Content.Items.MoveSpeedOnKill);
                if(harpoonCount > 0)
                {
                    float barrierGrant = harpoonBarrierBase + harpoonBarrierStack * (harpoonCount - 1);
                    hc.AddBarrierAuthority(barrierGrant);
                }
            }
        }

        private void AddHarpoonBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            int maskCount = self.inventory.GetItemCount(DLC1Content.Items.MoveSpeedOnKill);
            self.AddItemBehavior<HuntersHarpoonBehavior>(maskCount);
        }

        private void RevokeHarpoonRights(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "MoveSpeedOnKill"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }
    }
    public class HuntersHarpoonBehavior : RoR2.CharacterBody.ItemBehavior
    {
        public static float baseHauntRadius = 35;
        public static float hauntRetryTime = 1;
        float hauntStopwatch = 0;
        void Start()
        {
            hauntStopwatch = RiskierRainContent.harpoonTargetTime;
        }
        private void FixedUpdate()
        {
            hauntStopwatch += Time.fixedDeltaTime;
            if (hauntStopwatch >= RiskierRainContent.harpoonTargetTime)
            {
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

                        if (!enemyBody)
                        {
                            hurtBoxesList.Remove(hurtBoxesList[i]);
                            hurtBoxCount--;
                            continue;
                        }

                        for (int n = 0; n < stack; n++)
                        {
                            enemyBody.AddTimedBuffAuthority(Assets.harpoonDebuff.buffIndex, RiskierRainContent.harpoonTargetTime);
                        }

                        hauntStopwatch -= RiskierRainContent.harpoonTargetTime;
                        return;
                    }
                    hauntStopwatch -= hauntRetryTime;
                }
            }
        }

        private void OnDisable()
        {
            hauntStopwatch = 0;
        }
    }
}
