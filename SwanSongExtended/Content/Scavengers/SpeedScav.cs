using BepInEx.Configuration;
using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace SwanSongExtended.Scavengers
{
    public class SpeedScav : TwistedScavengerBase<SpeedScav>
    {
        public override string ScavName => "Baba";

        public override string ScavTitle => "Enlightened";

        public override string ScavLangTokenName => "ScavEnlightened";

        //NinjaGear.instance.EquipDef.name
        //RoR2Content.Equipment.FireBallDash.name
        //RoR2Content.Equipment.Jetpack.name
        public override string ScavEquipName => nameof(RoR2Content.Equipment.Jetpack);

        public override ExpansionDef RequiredExpansion => null;

        public override float SelectionWeight => 3;

        public override void PopulateItemInfos()
        {
            //white
            AddItemInfo(nameof(RoR2Content.Items.Hoof), 10); // 30
            AddItemInfo(nameof(RoR2Content.Items.SprintBonus), 0);
            AddItemInfo(nameof(RoR2Content.Items.BoostAttackSpeed), 7); //3
            AddItemInfo(nameof(DLC1Content.Items.AttackSpeedAndMoveSpeed), 0); //2

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

        public override void Init()
        {
            base.Init();
            ScavBody.baseDamage *= 0.3f;
            ScavBody.levelDamage = ScavBody.baseDamage * 0.2f;
            ScavBody.baseMaxHealth *= 0.2f;
            ScavBody.levelMaxHealth = ScavBody.baseMaxHealth * 0.3f;
        }
    }
}
