using RainrotSharedUtils;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwanSongExtended.Scavengers
{
    public class GooboScav : TwistedScavengerBase<GooboScav>
    {
        public override string ScavName => "Chipchip";

        public override string ScavTitle => "Cunning";

        public override string ScavLangTokenName => "ScavGoobo";

        public override string ScavEquipName => nameof(DLC1Content.Equipment.GummyClone);

        public override ExpansionDef RequiredExpansion => TwistedScavengerUtils.dlc1;

        public override float SelectionWeight => 0.5f;
        public override void Init()
        {
            base.Init();
            ScavBody.baseDamage *= 0.5f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.1f;
            ScavBody.baseMaxHealth *= 0.5f;
            ScavBody.levelMaxHealth = ScavBody.baseMaxHealth * 0.3f;
        }

        public override void PopulateItemInfos()
        {
            //white

            //green
            AddItemInfo(nameof(RoR2Content.Items.EquipmentMagazine), 2);
            AddItemInfo(nameof(DLC1Content.Items.MoveSpeedOnKill), 1);

            //red
            AddItemInfo(nameof(DLC1Content.Items.RandomEquipmentTrigger), 1);

            //yellow

            //lunar
            AddItemInfo(nameof(DLC1Content.Items.HalfAttackSpeedHalfCooldowns), 1);
            AddItemInfo(nameof(DLC1Content.Items.HalfSpeedDoubleHealth), 1);

            //void
            AddItemInfo(nameof(DLC1Content.Items.BleedOnHitVoid), 10); //this might be a huge mistake
            AddItemInfo(nameof(DLC1Content.Items.BearVoid), 1);
        }
    }
}
