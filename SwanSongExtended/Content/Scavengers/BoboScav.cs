using BepInEx.Configuration;
using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using RoR2.ExpansionManagement;

namespace SwanSongExtended.Scavengers
{
    public class BoboScav : TwistedScavengerBase<BoboScav>
    {
        public override string ScavName => "Bobo";

        public override string ScavTitle => "Unbreakable";

        public override string ScavLangTokenName => "ScavUnstoppable";

        public override string ScavEquipName => nameof(RoR2Content.Equipment.GainArmor);

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override float SelectionWeight => 1;

        public override void Init()
        {
            base.Init();
            ScavBody.baseMaxHealth *= 0.5f;
            ScavBody.levelMaxHealth = ScavBody.baseMaxHealth * 0.3f;
            ScavBody.baseDamage *= 0.5f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
            ScavBody.baseAttackSpeed = 0.4f;
            ScavBody.baseMoveSpeed = 2f;
        }

        public override void PopulateItemInfos()
        {
            //white
            AddItemInfo(nameof(RoR2Content.Items.PersonalShield), 5);
            AddItemDefInfo(Fuse.instance.ItemsDef, 0);
            AddItemDefInfo(BigBattery.instance.ItemsDef, 3);
            //AddItemInfo(ref itemInfos, RoR2Content.Items.IgniteOnKill.name, 1); 

            //green
            AddItemDefInfo(FrozenShell.instance.ItemsDef, 1);
            AddItemDefInfo(FlowerCrown.instance.ItemsDef, 3);
            AddItemDefInfo(BirdBand.instance.ItemsDef, 0);
            AddItemDefInfo(UtilityBelt.instance.ItemsDef, 1);

            //red
            AddItemInfo(nameof(RoR2Content.Items.BarrierOnOverHeal), 1);

            //yellow
            AddItemInfo(nameof(RoR2Content.Items.Pearl), 2);
            AddItemInfo(nameof(RoR2Content.Items.ShinyPearl), 2);

            //lunar
            //AddItemInfo(nameof(RoR2Content.Items.RandomDamageZone), 1);
            AddItemInfo(nameof(RoR2Content.Items.LunarBadLuck.name), 1);
        }
    }
}
