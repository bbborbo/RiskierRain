using EntityStates.Headstompers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using static MoreStats.JumpAPI;

namespace JumpRework
{
    public partial class JumpReworkPlugin
    {
        public float hopooDamageBuffDuration = 0.5f;
        public static float hopooDamageIncreasePerBuff = 0.1f;
        float featherBaseDuration = 0.75f;
        float featherStackDuration = 0.5f;
        public static float featherJumpPowerBase = 0.15f;
        public static float featherJumpPowerStack = 0.15f;
        private void FeatherRework()
        {
            OnJumpEvent += FeatherJumpPowerBoost;
            GetStatCoefficients += FeatherJumpPower;
            LanguageAPI.Add("ITEM_FEATHER_PICKUP", "Double jump with increased jump height.");
            LanguageAPI.Add("ITEM_FEATHER_DESC",
                (FeatherJumpCount.Value == 1 ? $"Gain an additional jump. " : $"Gain <style=cIsUtility>{FeatherJumpCount.Value}</style> jumps. ") +
                $"Increase jump power by <style=cIsUtility>{featherJumpPowerBase.AsPercent()}</style> <style=cStack>(+{featherJumpPowerStack.AsPercent()} per stack)</style>.");
                //$"<style=cIsUtility>On jump</style>, increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>125%</style>, " +
                //$"fading over <style=cIsUtility>{featherBaseDuration}</style> <style=cStack>(+{featherStackDuration} per stack)</style> seconds.");
            return;
            OnJumpEvent += FeatherOnJump;
            //On.RoR2.GlobalEventManager.OnCharacterHitGroundServer += FeatherOnLandServer;
            //GetStatCoefficients += FeatherDamageBoost;
            LanguageAPI.Add("ITEM_FEATHER_PICKUP", "Double jump. Jumping gives you a boost of movement speed.");
            LanguageAPI.Add("ITEM_FEATHER_DESC",
                $"Gain <style=cIsUtility>{FeatherJumpCount.Value}</style> jumps. " +
                $"<style=cIsUtility>On jump</style>, increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>125%</style>, " +
                $"fading over <style=cIsUtility>{featherBaseDuration}</style> <style=cStack>(+{featherStackDuration} per stack)</style> seconds.");
        }

        private void FeatherJumpPower(CharacterBody sender, StatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv)
            {
                int count = inv.GetItemCountEffective(RoR2Content.Items.Feather);
                if (count > 0)
                {
                    float power = featherJumpPowerBase + featherJumpPowerStack * (count - 1);
                    args.jumpPowerMultAdd += power;
                }
            }
        }

        private void FeatherJumpPowerBoost(CharacterMotor sender, JumpSynergyInfo jumpSynergyInfo)
        {
            CharacterBody body = sender.body;
            Inventory inv = body.inventory;
            if (inv)
            {
                int count = inv.GetItemCountEffective(RoR2Content.Items.Feather);
                if (count > 0 && IsDoubleJump(sender))
                {
                    float power = featherJumpPowerBase + featherJumpPowerStack * (count - 1);
                    ProposeJumpPowerBonusForCurrentJump(0, power);
                }
            }
        }

        private void FeatherOnJump(CharacterMotor motor, JumpSynergyInfo synergyInfo)
        {
            CharacterBody body = motor.body;
            Inventory inv = body.inventory;
            if (inv)
            {
                int count = inv.GetItemCountEffective(RoR2Content.Items.Feather);
                if (count > 0 && IsDoubleJump(motor))
                {

                    return;
                    int increments = 5;
                    float totalDuration = featherBaseDuration + (float)(count - 1) * featherStackDuration;
                    body.ClearTimedBuffs(DLC1Content.Buffs.KillMoveSpeed);
                    for (int l = 0; l < increments; l++)
                    {
                        body.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, totalDuration * (float)(l + 1) / (float)increments);
                    }
                    EffectData effectData = new EffectData();
                    effectData.origin = body.corePosition;
                    CharacterMotor characterMotor = body.characterMotor;
                    bool flag = false;
                    if (characterMotor)
                    {
                        Vector3 moveDirection = characterMotor.moveDirection;
                        if (moveDirection != Vector3.zero)
                        {
                            effectData.rotation = Util.QuaternionSafeLookRotation(moveDirection);
                            flag = true;
                        }
                    }
                    if (!flag)
                    {
                        effectData.rotation = body.transform.rotation;
                    }
                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MoveSpeedOnKillActivate"), effectData, true);
                }
                //body.AddBuff(Assets.hopooDamageBuff);
            }
        }
    }
}
