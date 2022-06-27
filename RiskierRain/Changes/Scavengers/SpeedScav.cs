using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain.Scavengers
{
    class SpeedScav : TwistedScavengerBase<SpeedScav>
    {
        public override string ScavName => "Baba";

        public override string ScavTitle => "Enlightened";

        public override string ScavLangTokenName => "Speed";

        //NinjaGear.instance.EquipDef.name
        //RoR2Content.Equipment.FireBallDash.name
        //RoR2Content.Equipment.Jetpack.name
        public override string ScavEquipDefName => RoR2Content.Equipment.Jetpack.name;
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfDefenseAndHealing;

        public override void PopulateItemInfos(ConfigFile config)
        {
            //white
            AddItemInfo(RoR2Content.Items.Hoof.name, 30);
            AddItemInfo(RoR2Content.Items.SprintBonus.name, 5);
            AddItemInfo(RoR2Content.Items.BoostAttackSpeed.name, 7); //3

            //green
            AddItemInfo(RoR2Content.Items.Feather.name, 2);
            AddItemInfo(RoR2Content.Items.JumpBoost.name, 2);
            AddItemInfo(RoR2Content.Items.EquipmentMagazine.name, 3);

            //red
            AddItemInfo(RoR2Content.Items.ExtraLife.name, 2);

            //yellow

            //lunar
            AddItemInfo(RoR2Content.Items.LunarSecondaryReplacement.name, 1);
            AddItemInfo(RoR2Content.Items.LunarUtilityReplacement.name, 1);
            AddItemInfo(RoR2Content.Items.AutoCastEquipment.name, 0);
        }

        public override void Init(ConfigFile config)
        {
            GenerateTwistedScavenger();
            ScavBody.baseDamage *= 0.3f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
        }
    }
}
