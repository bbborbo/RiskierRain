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

        public override string ScavEquipName => nameof(RoR2Content.Equipment.GainArmor);
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfHealth;

        public override void Init(ConfigFile config)
        {
            GenerateTwistedScavenger();
            ScavBody.baseMaxHealth *= 0.5f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.3f;
            ScavBody.baseDamage *= 0.5f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
            ScavBody.baseAttackSpeed = 0.4f;
            ScavBody.baseMoveSpeed = 2f;
        }

        public override void PopulateItemInfos(ConfigFile config)
        {
            //white
            AddItemInfo(nameof(RoR2Content.Items.PersonalShield), 0);
            AddItemDefInfo(Fuse.instance.ItemsDef, 0);
            //AddItemInfo(ref itemInfos, RoR2Content.Items.IgniteOnKill.name, 1); 

            //green
            AddItemDefInfo(FrozenShell.instance.ItemsDef, 3);
            AddItemDefInfo(FlowerCrown.instance.ItemsDef, 0);
            AddItemDefInfo(BigBattery.instance.ItemsDef, 5);
            AddItemDefInfo(BirdBand.instance.ItemsDef, 0);
            AddItemDefInfo(UtilityBelt.instance.ItemsDef, 1);

            //red
            AddItemInfo(nameof(RoR2Content.Items.BarrierOnOverHeal), 1);

            //yellow
            AddItemInfo(nameof(RoR2Content.Items.Pearl), 2);
            AddItemInfo(nameof(RoR2Content.Items.ShinyPearl), 2);

            //lunar
            AddItemInfo(nameof(RoR2Content.Items.RandomDamageZone), 1);
            //AddItemInfo(ref itemInfos, RoR2Content.Items.LunarBadLuck.name, 1);
        }
    }
}
