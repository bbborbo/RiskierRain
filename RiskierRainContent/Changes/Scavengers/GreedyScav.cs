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

        public override string ScavEquipName => nameof(RoR2Content.Equipment.GoldGat);

        public override void Init(ConfigFile config)
        {
            GenerateTwistedScavenger();
            ScavBody.baseMaxHealth *= 0.7f;
            ScavBody.levelMaxHealth = ScavBody.baseMaxHealth * 0.3f;
        }

        public override void PopulateItemInfos(ConfigFile config)
        {
            //white
            AddItemInfo(nameof(RoR2Content.Items.Hoof), 5);
            AddItemInfo(nameof(RoR2Content.Items.Bear), 3);
            AddItemInfo(nameof(RoR2Content.Items.SecondarySkillMagazine), 2);
            AddItemDefInfo(Elixir2.instance.ItemsDef, 5);

            //green
            AddItemInfo(nameof(RoR2Content.Items.Bandolier), 1);
            AddItemInfo(nameof(RoR2Content.Items.BonusGoldPackOnKill), 5);

            AddItemDefInfo(CoinGun.instance.ItemsDef, 1);
            AddItemDefInfo(Slungus.instance.ItemsDef, 3);
            //AddItemDefInfo(GreedyRing.instance.ItemsDef, 1);

            //red
            AddItemInfo(nameof(RoR2Content.Items.UtilitySkillMagazine), 1);
            AddItemDefInfo(WickedBand.instance.ItemsDef, 3);

            //yellow
            //AddItemInfo(ref itemInfos, RoR2Content.Items.BleedOnHitAndExplode.name, 0);

            //lunar
            AddItemInfo(nameof(RoR2Content.Items.GoldOnHit), 10);
        }
    }
}
