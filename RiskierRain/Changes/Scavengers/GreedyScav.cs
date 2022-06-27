using BepInEx.Configuration;
using RiskierRain.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain.Scavengers
{
    class GreedyScav : TwistedScavengerBase<GreedyScav>
    {
        public override string ScavName => "Gibgib";

        public override string ScavTitle => "Greedy";

        public override string ScavLangTokenName => "Greedy";

        public override string ScavEquipDefName => RoR2Content.Equipment.GoldGat.name;

        public override void Init(ConfigFile config)
        {
            GenerateTwistedScavenger();
        }

        public override void PopulateItemInfos(ConfigFile config)
        {
            //white
            AddItemInfo(RoR2Content.Items.Bear.name, 5);
            AddItemInfo(RoR2Content.Items.SecondarySkillMagazine.name, 4);

            //green
            AddItemInfo(RoR2Content.Items.Bandolier.name, 1);
            AddItemInfo(RoR2Content.Items.BonusGoldPackOnKill.name, 5);

            AddItemInfo(CoinGun.instance.ItemsDef.name, 1);

            //red
            AddItemInfo(RoR2Content.Items.UtilitySkillMagazine.name, 1);

            //yellow
            //AddItemInfo(ref itemInfos, RoR2Content.Items.BleedOnHitAndExplode.name, 0);

            //lunar
            AddItemInfo(RoR2Content.Items.GoldOnHit.name, 10);
        }
    }
}
