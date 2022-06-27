using BepInEx.Configuration;
using RiskierRain.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain.Scavengers
{
    class BoboScav : TwistedScavengerBase<BoboScav>
    {
        public override string ScavName => "Bobo";

        public override string ScavTitle => "Unbreakable";

        public override string ScavLangTokenName => "Unstoppable";

        public override string ScavEquipDefName => RoR2Content.Equipment.GainArmor.name;
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfHealth;

        public override void Init(ConfigFile config)
        {
            GenerateTwistedScavenger();
            ScavBody.baseDamage *= 0.5f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
            ScavBody.baseAttackSpeed = 0.7f;
            ScavBody.baseMoveSpeed = 2f;
        }

        public override void PopulateItemInfos(ConfigFile config)
        {
            //white
            AddItemInfo(RoR2Content.Items.PersonalShield.name, 0);
            //AddItemInfo(ref itemInfos, RoR2Content.Items.IgniteOnKill.name, 1); 

            //green
            AddItemInfo(FrozenShell.instance.ItemsDef.name, 1);
            AddItemInfo(FlowerCrown.instance.ItemsDef.name, 1);
            AddItemInfo(BirdBand.instance.ItemsDef.name, 0);
            AddItemInfo(UtilityBelt.instance.ItemsDef.name, 10);
            AddItemInfo(ChefReference.instance.ItemsDef.name, 1);

            //red
            AddItemInfo(RoR2Content.Items.BarrierOnOverHeal.name, 3);

            //yellow
            AddItemInfo(RoR2Content.Items.Pearl.name, 2);
            AddItemInfo(RoR2Content.Items.ShinyPearl.name, 2);

            //lunar
            AddItemInfo(RoR2Content.Items.RandomDamageZone.name, 3);
            //AddItemInfo(ref itemInfos, RoR2Content.Items.LunarBadLuck.name, 1);
        }
    }
}
