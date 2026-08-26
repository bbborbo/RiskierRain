using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MoreStats.StatHooks;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Equipment
{
    public class TheMachine : EquipmentBase<TheMachine>
    {
        public static float baseDamageBoost = 0.03f;
        public static float baseMspdBoost = 0.04f;
        public static float baseCdrBoost = 0.05f;
        public static int baseArmorBoost = 4;
        public static float baseShieldBoost = 8;
        public static float baseBarrierBoost = 0.02f;
        public static float baseLuckBoost = 0.05f;
        public static float baseRegenBoost = 0.2f;
        public static int tier3ScrapValue = 4;
        public static int tierBossScrapValue = 3;
        public static int tier2ScrapValue = 2;
        public static int tier1ScrapValue = 1;
        public static int selfScrapValue = 3;
        public override string EquipmentName => "The Machine";

        public override string EquipmentLangTokenName => "THEMACHINE";

        public override string EquipmentPickupDesc => "It doesn't do anything. That's the beauty of it!";

        public override string EquipmentFullDescription => "It doesn't do anything. That's the beauty of it!";

        public override string EquipmentLore => "";

        public override GameObject EquipmentModel => LoadDropPrefab("TheMachine");

        public override Sprite EquipmentIcon => LoadItemIcon("TheMachine");

        public override float BaseCooldown => 10f;

        public override bool EnigmaCompatible => true;

        public override bool CanBeRandomlyActivated => false;

        public override string ConfigName => "The Machine";

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Hooks()
        {
            GetMoreStatCoefficients += MachineMoreStats;
            GetStatCoefficients += MachineStats;
        }

        public static int GetScrapValue(CharacterBody sender)
        {
            MoreStats.MoreStatCoefficients customStats = GetMoreStatsFromBody(sender);
            return (customStats.bodyScrapWhiteCount * tier1ScrapValue)
                + (customStats.bodyScrapGreenCount * tier2ScrapValue)
                + (customStats.bodyScrapRedCount * tier3ScrapValue)
                + (customStats.bodyScrapYellowCount * tierBossScrapValue);
        }

        private void MachineStats(CharacterBody sender, StatHookEventArgs args)
        {
            if (HasEquip(sender) == false)
                return;
            float scrapValue = GetScrapValue(sender);

            args.baseShieldAdd += baseShieldBoost * scrapValue;
            args.luckAdd += baseLuckBoost * scrapValue;
            args.damageMultAdd += baseDamageBoost * scrapValue;
            args.moveSpeedMultAdd += baseMspdBoost * scrapValue;
            args.baseRegenAdd += baseRegenBoost * scrapValue;
            args.levelRegenAdd += baseRegenBoost * scrapValue * 0.2f;
            args.allSkills.cooldownReductionMultAdd += baseCdrBoost * scrapValue;
        }

        private void MachineMoreStats(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (HasEquip(sender) == false)
                return;
            float scrapValue = GetScrapValue(sender);

            args.barrierDecayRatePercentIncreaseMult = baseBarrierBoost * scrapValue;
            switch (selfScrapValue)
            {
                default:
                case 4:
                    args.scrapRedCountAdd += 1;
                    break;
                case 3:
                    args.scrapYellowCountAdd += 1;
                    break;
                case 2:
                    args.scrapGreenCountAdd += 1;
                    break;
                case 1:
                    args.scrapWhiteCountAdd += 1;
                    break;
            }
            args.scrapWhiteCountAdd += selfScrapValue;
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return true;
        }
    }
}
