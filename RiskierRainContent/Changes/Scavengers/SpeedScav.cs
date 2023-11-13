using BepInEx.Configuration;
using RiskierRainContent.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRainContent.Scavengers
{
    class SpeedScav : TwistedScavengerBase<SpeedScav>
    {
        public override string ScavName => "Baba";

        public override string ScavTitle => "Enlightened";

        public override string ScavLangTokenName => "Speed";

        //NinjaGear.instance.EquipDef.name
        //RoR2Content.Equipment.FireBallDash.name
        //RoR2Content.Equipment.Jetpack.name
        public override string ScavEquipName => nameof(RoR2Content.Equipment.Jetpack);

        public override void PopulateItemInfos(ConfigFile config)
        {
            //white
            AddItemInfo(nameof(RoR2Content.Items.Hoof), 10); // 30
            AddItemInfo(nameof(RoR2Content.Items.SprintBonus), 5);
            AddItemInfo(nameof(RoR2Content.Items.BoostAttackSpeed), 7); //3
            AddItemDefInfo(Mocha2.instance.ItemsDef, 2); //1

            //green
            AddItemInfo(nameof(RoR2Content.Items.Feather), 2);
            AddItemInfo(nameof(RoR2Content.Items.EquipmentMagazine), 3);

            //red
            AddItemInfo(nameof(RoR2Content.Items.ExtraLife), 2);
            AddItemInfo(nameof(RoR2Content.Items.JumpBoost), 2);

            //yellow

            //lunar
            AddItemInfo(nameof(RoR2Content.Items.LunarSecondaryReplacement), 1);
            AddItemInfo(nameof(RoR2Content.Items.LunarUtilityReplacement), 1);
            AddItemInfo(nameof(RoR2Content.Items.AutoCastEquipment), 0);
        }

        public override void Init(ConfigFile config)
        {
            GenerateTwistedScavenger();
            ScavBody.baseDamage *= 0.3f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
            ScavBody.baseMaxHealth *= 0.2f;
            ScavBody.levelMaxHealth = ScavBody.baseMaxHealth * 0.3f;
        }
    }
}
