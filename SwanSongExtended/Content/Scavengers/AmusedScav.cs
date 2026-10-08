using RainrotSharedUtils;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwanSongExtended.Scavengers
{
    public class AmusedScav : TwistedScavengerBase<AmusedScav>
    {
        public override string ScavName => "Apa\u2019Apa";

        public override string ScavTitle => "Amused";

        public override string ScavLangTokenName => "ScavAmused";

        public override string ScavEquipName => "PassiveHealing";

        public override ExpansionDef RequiredExpansion => TwistedScavengerUtils.dlc1;

        public override float SelectionWeight => 1;
        public override void Init()
        {
            base.Init();
            ScavBody.baseMaxHealth *= 0.25f;
            ScavBody.levelMaxHealth *= 0.25f;
            ScavBody.baseArmor += 100;
        }
        public override void PopulateItemInfos()
        {
            AddItemInfo(nameof(RoR2Content.Items.NovaOnLowHealth), 1);
            AddItemInfo(nameof(DLC1Content.Items.RandomEquipmentTrigger), 1);
            AddItemInfo(nameof(RoR2Content.Items.BarrierOnKill), 5);
            AddItemInfo(nameof(RoR2Content.Items.BarrierOnOverHeal), 5);
            AddItemInfo(nameof(RoR2Content.Items.RepeatHeal), 1);
            AddItemInfo(nameof(RoR2Content.Items.Clover), 1);
            //AddItemInfo(nameof(RoR2Content.Items.NovaOnHeal), 1);
            AddItemInfo(nameof(DLC1Content.Items.ElementalRingVoid), 1);
            AddItemInfo(nameof(DLC1Content.Items.RandomlyLunar), 1);
        }
    }
}
