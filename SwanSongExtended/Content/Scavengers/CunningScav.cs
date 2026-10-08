using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRainContent.Scavengers
{
    class CunningScav : TwistedScavengerBase<CunningScav>
    {
        public override string ScavName => "ChipChip";

        public override string ScavTitle => "Cunning";

        public override string ScavLangTokenName => "Cunning";

        public override string ScavEquipName => nameof(RoR2Content.Equipment.Lightning);

        public override void Init(ConfigFile config)
        {
        }

        public override void PopulateItemInfos(ConfigFile config)
        {
        }
    }
}
