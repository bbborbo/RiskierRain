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
        public override string ScavName => "Mimi";

        public override string ScavTitle => "Wicked";

        public override string ScavLangTokenName => "Sadist";

        public override string ScavEquipName => nameof(RoR2Content.Equipment.CrippleWard); //effigy debuff

        public override void Init(ConfigFile config)
        {
            GenerateTwistedScavenger();
            ScavBody.baseDamage *= 0.1f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
        }

        public override void PopulateItemInfos(ConfigFile config)
        {
            //white
            AddItemInfo(nameof(RoR2Content.Items.CritGlasses), 0); //3
            AddItemInfo(nameof(RoR2Content.Items.BleedOnHit), 2); //4, debuff
            AddItemInfo(nameof(RoR2Content.Items.BoostAttackSpeed), 0); //3
            AddItemInfo(nameof(RoR2Content.Items.IgniteOnKill), 0); //1, debuff

            //green
            AddItemInfo(nameof(RoR2Content.Items.DeathMark), 1);
            AddItemInfo(nameof(RoR2Content.Items.SlowOnHit), 2); //debuff
            AddItemInfo(nameof(RoR2Content.Items.WarCryOnMultiKill), 1);
            AddItemInfo(nameof(DLC1Content.Items.PrimarySkillShuriken), 5); //debuff

            AddItemDefInfo(ChefReference.instance.ItemsDef, 1); //debuff
            AddItemInfo(nameof(DLC1Content.Items.StrengthenBurn), 1); //debuff

            //red
            AddItemInfo(nameof(RoR2Content.Items.ArmorReductionOnHit), 2); //debuff
            AddItemInfo(nameof(RoR2Content.Items.Talisman), 0);
            AddItemInfo(nameof(RoR2Content.Items.Icicle), 0); //debuff

            //yellow
            AddItemInfo(nameof(RoR2Content.Items.BleedOnHitAndExplode), 0); //1, debuff

            //lunar
            AddItemInfo(nameof(RoR2Content.Items.RandomDamageZone), 0); //buff
            AddItemInfo(nameof(RoR2Content.Items.AutoCastEquipment), 3);
            AddItemInfo(nameof(DLC1Content.Items.HalfAttackSpeedHalfCooldowns), 1);
        }
    }
}
