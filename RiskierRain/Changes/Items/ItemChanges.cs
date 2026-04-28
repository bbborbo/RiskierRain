using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using MoreStats;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;

namespace RiskierRain.Changes
{
    public static partial class ItemChanges
    {
        public static float notMovingRequirement = 0.1f;

        public static void AIBlacklistThisItem(string guid)
        {
            LoadAsync<ItemDef>(guid, AIBlacklistThisItem);
        }
        public static void AIBlacklistThisItem(ItemDef itemDef)
        {
            List<ItemTag> itemTags = new List<ItemTag>(itemDef.tags);
            itemTags.Add(ItemTag.AIBlacklist);

            itemDef.tags = itemTags.ToArray();
        }

        public static void Initialize()
        {
            #region rework pending / priority removal
            //RiskierRainPlugin.RetierItemAsync(RoR2_DLC3_Items_BarrierOnCooldown.BarrierOnCooldown_asset);//eclipse lite
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC3_Items_CritAtLowerElevation.CritAtLowerElevation_asset);//hikers boots

            //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.PrimarySkillShuriken)); //shuriken
            //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.MoveSpeedOnKill)); //hunter's harpoon
            //RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.Squid)); //squid polyp HAS BEEN REWORKED AND IS AWESOME NOW
            RiskierRainPlugin.RetierItemAsync(RoR2_Base_BonusGoldPackOnKill.BonusGoldPackOnKill_asset); //ghors
            //RiskierRainPlugin.RetierItemAsync(RoR2_Base_DeathMark.DeathMark_asset); //guess faggot
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC3_Items_ShieldBooster.ShieldBooster_asset);//kinetic dampener

            RiskierRainPlugin.RetierItemAsync(RoR2_Base_Talisman.Talisman_asset); //soulbound
            //RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_MoreMissile.MoreMissile_asset); //icbm
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_PermanentDebuffOnHit.PermanentDebuffOnHit_asset); //scorpion
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_DroneWeapons.DroneWeapons_asset); //sdp
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC2_Items_BarrageOnBoss.BarrageOnBoss_asset); //war bonds
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC2_Items_BoostAllStats.BoostAllStats_asset); //growth nectar


            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_HalfSpeedDoubleHealth.HalfSpeedDoubleHealth_asset); //
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_HalfAttackSpeedHalfCooldowns.HalfAttackSpeedHalfCooldowns_asset); //

            //RiskierRainPlugin.RetierItem(Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/AutoCastEquipment/AutoCastEquipment.asset").WaitForCompletion());
            #endregion

            #region ehp
            BaseStats.BarrierDecayStaticMaxHealthTime = 30f;// BarrierDecayRateStatic.Value; //30f
            BaseStats.BarrierHighDecayFactor = 5f;// BarrierDecayHighFactor.Value; //3f
            BaseStats.BarrierLowDecayFactor = 0.33f;// BarrierDecayLowFactor.Value; //0.5f

            FreeBonusArmor();
            ChangeSaferSpaces();
            //FuckingFixInfusion();
            ChangeTopazBrooch();
            ChangeEclipseLite();
            ChangeWarpedEcho();
            #endregion

            #region healing
            ChangeMedkit();
            ChangeMonsterTooth();
            ChangeBungus();
            ChangeWeepingFungus();
            ChangeScythe();
            ChangeLeptonDaisy();
            ChangeLeechingSeed();
            #endregion

            #region mobility
            //GoatHoofNerf();
            ChangeFaraday();
            ChangeElusiveAntlers();
            #endregion

            #region autoplay
            ChangeNkuhana();
            ChangeDisciple();
            ChangeWillowisp();
            ChangeShatterspleen();
            ChangeFireworks();
            ChangeVoidsent();
            ChangeGasoline();
            #endregion

            #region damage
            ChangeRunald();
            ChangeKjaro();
            //NerfCritGlasses();
            ChangeDeathMark();
            ChangePolylute();
            ChangeBoxOfDynamite();
            ChangeLuminousShot();
            ChangeGenesisLoop();
            //ChangeJustice();
            //ChangePauldron();
            FixLostSeers();
            //ChangeStickyBomb();
            #endregion

            #region economy
            ChangeChanceDoll();
            ChangeSaleStar();
            #endregion

            #region utility
            ChangeFuelCell();
            ChangeWarbanner();
            //ChangeBottledChaos();
            #endregion
        }
    }
}
