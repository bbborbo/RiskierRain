using BepInEx.Configuration;
using RiskierRain.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain.Scavengers
{
    class SadistScav : TwistedScavengerBase<SadistScav>
    {
        public override string ScavName => "Chipchip";

        public override string ScavTitle => "Wicked";

        public override string ScavLangTokenName => "Sadist";

        public override string ScavEquipDefName => RoR2Content.Equipment.CrippleWard.name; //effigy debuff
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfDamage;

        public override void Init(ConfigFile config)
        {
            GenerateTwistedScavenger();
            ScavBody.baseDamage *= 0.3f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
        }

        public override void PopulateItemInfos(ConfigFile config)
        {
            //white
            AddItemInfo(RoR2Content.Items.CritGlasses.name, 0); //3
            AddItemInfo(RoR2Content.Items.BleedOnHit.name, 4); //debuff
            AddItemInfo(RoR2Content.Items.BoostAttackSpeed.name, 0); //3
            AddItemInfo(RoR2Content.Items.IgniteOnKill.name, 1); //1

            //green
            AddItemInfo(RoR2Content.Items.DeathMark.name, 3);
            AddItemInfo(RoR2Content.Items.SlowOnHit.name, 2); //debuff
            AddItemInfo(RoR2Content.Items.WarCryOnMultiKill.name, 2);

            AddItemInfo(ChefReference.instance.ItemsDef.name, 2); //debuff

            //red
            AddItemInfo(RoR2Content.Items.ArmorReductionOnHit.name, 2); //debuff
            AddItemInfo(RoR2Content.Items.Talisman.name, 0);
            AddItemInfo(RoR2Content.Items.Icicle.name, 2); //debuff

            //yellow
            AddItemInfo(RoR2Content.Items.BleedOnHitAndExplode.name, 0); //1, debuff

            //lunar
            AddItemInfo(RoR2Content.Items.RandomDamageZone.name, 0);
            AddItemInfo( RoR2Content.Items.AutoCastEquipment.name, 1);
        }
    }
}
