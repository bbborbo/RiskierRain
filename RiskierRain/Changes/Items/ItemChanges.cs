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
using static RiskierRain.CoreModules.Language.Styling;

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
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_HealingPotion.HealingPotion_asset);//elixir

            //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.PrimarySkillShuriken)); //shuriken
            //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.MoveSpeedOnKill)); //hunter's harpoon
            //RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.Squid)); //squid polyp HAS BEEN REWORKED AND IS AWESOME NOW
            //RiskierRainPlugin.RetierItemAsync(RoR2_Base_BonusGoldPackOnKill.BonusGoldPackOnKill_asset); //ghors
            //RiskierRainPlugin.RetierItemAsync(RoR2_Base_DeathMark.DeathMark_asset); //guess faggot
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC3_Items_ShieldBooster.ShieldBooster_asset);//kinetic dampener
            //RiskierRainPlugin.RetierItemAsync(RoR2_DLC3_Items_JumpDamageStrike.JumpDamageStrike_asset);//faraday

            RiskierRainPlugin.RetierItemAsync(RoR2_Base_Talisman.Talisman_asset); //soulbound
            //RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_MoreMissile.MoreMissile_asset); //icbm
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_PermanentDebuffOnHit.PermanentDebuffOnHit_asset); //scorpion
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_DroneWeapons.DroneWeapons_asset); //sdp
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC2_Items_BarrageOnBoss.BarrageOnBoss_asset); //war bonds
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC2_Items_BoostAllStats.BoostAllStats_asset); //growth nectar


            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_HalfSpeedDoubleHealth.HalfSpeedDoubleHealth_asset); //
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_HalfAttackSpeedHalfCooldowns.HalfAttackSpeedHalfCooldowns_asset); //
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC2_Items_OnLevelUpFreeUnlock.OnLevelUpFreeUnlock_asset); //longstanding solitude

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
            ChangeVengefulSpirit();
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
            ReworkShuriken();
            ChangeDormantSpirit();
            #endregion

            #region economy
            ChangeChanceDoll();
            ChangeSaleStar();
            #endregion

            #region utility
            ChangeFuelCell();
            ChangeWarbanner();
            ChangeChronobauble();
            //ChangeBottledChaos();
            ChangeDreamCrucible();
            #endregion

            #region misc
            RemoveAspdScalingOnCooldownsForever();
            FixPickupStats();
            //MakeMinionsInheritOnKillEffects();
            #endregion
        }

        #region dream crucible
        public static float dreamCrucibleActivationChance = 0.167f;
        private static void ChangeDreamCrucible()
        {
            On.RoR2.Items.LowHealthOrbSpawnerBehavior.GetRollChanceNormalized += LowHealthOrbSpawnerBehavior_GetRollChanceNormalized;
            LanguageAPI.Add("ITEM_LOWHEALTHORBSPAWNER_DESC",
                $"When another item goes on cooldown or is consumed, " +
                $"<style=cIsUtility>{dreamCrucibleActivationChance.AsPercent()} <style=cStack>(+{dreamCrucibleActivationChance.AsPercent()} per stack)</style></style> chance " +
                $"for <style=cIsHealing>this item to go on cooldown or be consumed instead</style>. " +
                $"At the start of each stage, it regenerates.");
        }

        private static float LowHealthOrbSpawnerBehavior_GetRollChanceNormalized(On.RoR2.Items.LowHealthOrbSpawnerBehavior.orig_GetRollChanceNormalized orig, LowHealthOrbSpawnerBehavior self)
        {
            return 1f - Mathf.Pow(1f - dreamCrucibleActivationChance, (float)self.stack);
        }
        #endregion

        #region vengeful spirit
        public static float vengefulSpiritHealth = 300;//90
        public static float vengefulSpiritAttackSpeedBase = 0.3f;
        public static float vengefulSpiritAttackSpeedStack = 0.3f;
        public static float vengefulSpiritCdrBase = 0.15f;
        public static float vengefulSpiritCdrStack = 0.15f;
        private static void ChangeVengefulSpirit()
        {
            LanguageAPI.Add("ITEM_VENGEFULSPIRIT_PICKUP",
                "Gain a protective spirit that absorbs 50% of all damage taken. " +
                "After the spirit is killed, gain attack speed and reduced skill cooldowns.");
            LanguageAPI.Add("ITEM_VENGEFULSPIRIT_DESC",
                $"Gain <style=cIsHealing>1 <style=cStack>(+1 per stack)</style> protective spirit</style> " +
                $"that absorbs <style=cIsHealing>50%</style> of all damage taken. " +
                $"If the spirit is killed, become <style=cIsDamage>vengeful</style> for the rest of the stage, " +
                $"gaining {DamageColor(vengefulSpiritAttackSpeedBase.AsPercent())} increased <style=cIsDamage>attack speed</style> {StackText("+" + vengefulSpiritAttackSpeedStack.AsPercent())} " +
                $"and {UtilityColor(vengefulSpiritCdrBase.AsPercent())} reduced <style=cIsUtility>skill cooldowns</style> {StackText("-" + vengefulSpiritCdrStack.AsPercent())}. ");

            RiskierRainPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4_VengefulSpirit.VengefulSpiritFollowerBody_prefab, (bodyObject) =>
            {
                if (bodyObject.TryGetComponent(out CharacterBody body))
                {
                    body.baseMaxHealth = vengefulSpiritHealth;
                    body.levelMaxHealth = vengefulSpiritHealth * 0.3f;
                }
            });
        }
        private static void ChangeDormantSpirit()
        {
            GetStatCoefficients += DormantSpiritStats;
            On.RoR2.Items.VengefulSpiritRageModeBehavior.FixedUpdate += VengefulSpiritRageModeBehavior_FixedUpdate;
            On.RoR2.Items.VengefulSpiritRageModeBehavior.OnEnable += VengefulSpiritRageModeBehavior_OnEnable;
            On.RoR2.Items.VengefulSpiritRageModeBehavior.OnDisable += VengefulSpiritRageModeBehavior_OnDisable;

            LanguageAPI.Add("ITEM_VENGEFULSPIRITRAGEMODE_PICKUP",
                "A dormant spirit. Gain increased attack speed and reduced cooldowns. " +
                "At the start of each stage, it regenerates.");
            LanguageAPI.Add("ITEM_VENGEFULSPIRITRAGEMODE_DESC",
                $"Increases <style=cIsDamage>attack speed</style> by {DamageColor(vengefulSpiritAttackSpeedBase.AsPercent())} {StackText("+" + vengefulSpiritAttackSpeedStack.AsPercent())} " +
                $"and reduces <style=cIsUtility>skill cooldowns</style> by {UtilityColor(vengefulSpiritCdrBase.AsPercent())} {StackText("-" + vengefulSpiritCdrStack.AsPercent())}. " +
                $"At the start of each stage, it regenerates.");
        }

        private static void DormantSpiritStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = sender.inventory?.GetItemCountPermanent(DLC4Content.Items.VengefulSpiritRageMode) ?? 0;
            if(itemCount > 0)
            {
                args.attackSpeedMultAdd += Tools.GetStackValue(vengefulSpiritAttackSpeedBase, vengefulSpiritAttackSpeedStack, itemCount);
                args.allSkills.cooldownMultiplier *= Mathf.Pow(1 - vengefulSpiritCdrStack, itemCount);
            }
        }

        private static void VengefulSpiritRageModeBehavior_FixedUpdate(On.RoR2.Items.VengefulSpiritRageModeBehavior.orig_FixedUpdate orig, VengefulSpiritRageModeBehavior self)
        {

        }

        private static void VengefulSpiritRageModeBehavior_OnDisable(On.RoR2.Items.VengefulSpiritRageModeBehavior.orig_OnDisable orig, VengefulSpiritRageModeBehavior self)
        {

        }

        private static void VengefulSpiritRageModeBehavior_OnEnable(On.RoR2.Items.VengefulSpiritRageModeBehavior.orig_OnEnable orig, VengefulSpiritRageModeBehavior self)
        {

        }
        #endregion


        #region minion on kill
        public static void MakeMinionsInheritOnKillEffects()
        {
            On.RoR2.Inventory.GetItemCountEffective_ItemIndex += GetItemCountEffectiveInheritOnKills;
        }
        public static int GetItemCountEffectiveInheritOnKills(On.RoR2.Inventory.orig_GetItemCountEffective_ItemIndex orig, Inventory self, ItemIndex itemIndex)
        {
            int itemCount = orig(self, itemIndex);
            if (ItemCatalog.GetItemDef(itemIndex).ContainsTag(ItemTag.OnKillEffect) && itemCount == 0)
            {
                CharacterMaster master = self.GetComponent<CharacterMaster>();
                if (master != null)
                {
                    MinionOwnership mo = master.minionOwnership;
                    CharacterMaster ownerMaster = mo.ownerMaster;
                    if (ownerMaster)
                    {
                        int masterItemCount = ownerMaster.inventory.GetItemCountEffective(itemIndex);
                        itemCount = masterItemCount;
                    }
                }
            }
            return itemCount;
        }
        #endregion
        #region pickup droplets
        static GameObject healPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/HealPack");
        static GameObject ammoPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/AmmoPack");
        static GameObject moneyPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/BonusMoneyPack");

        public static void FixPickupStats()
        {
            BuffPickupRange(healPack);
            BuffPickupRange(ammoPack);
            BuffPickupRange(moneyPack);

            On.RoR2.GravitatePickup.OnTriggerEnter += ChangeGravitateTargetBehavior;
        }

        private static void ChangeGravitateTargetBehavior(On.RoR2.GravitatePickup.orig_OnTriggerEnter orig, GravitatePickup self, Collider other)
        {
            if (NetworkServer.active && TeamComponent.GetObjectTeam(other.gameObject) == self.teamFilter.teamIndex)
            {
                if (self.gravitateTarget)
                {
                    if (other.gameObject.transform == self.gravitateTarget)
                        return;

                    HealthComponent targetHealthComponent = self.gravitateTarget.GetComponent<HealthComponent>();
                    if (targetHealthComponent && targetHealthComponent.body.isPlayerControlled)
                        return;
                }

                HealthComponent component = other.gameObject.GetComponent<HealthComponent>();
                if (component != null && (self.gravitateAtFullHealth || component.health < component.fullHealth))
                {
                    if (component.body.isPlayerControlled)
                    {
                        self.gravitateTarget = other.gameObject.transform;
                        return;
                    }
                }

                if (!self.gravitateTarget)
                {
                    if (self.gravitateAtFullHealth)
                    {
                        self.gravitateTarget = other.gameObject.transform;
                    }
                }
            }
        }

        public static void BuffPickupRange(GameObject pack)
        {
            GravitatePickup gravPickup = pack.GetComponentInChildren<GravitatePickup>();
            if (gravPickup != null)
            {
                Collider gravitateTrigger = gravPickup.gameObject.GetComponent<Collider>();
                if (gravitateTrigger.isTrigger)
                {
                    gravitateTrigger.transform.localScale *= 2.5f;
                }
            }
            else
            {
                Debug.Log($"GameObject {pack.name} has no GravitatePickup component!");
            }
        }
        #endregion
        #region attack speed affected cooldowns
        public static void RemoveAspdScalingOnCooldownsForever()
        {
            IL.RoR2.GenericSkill.RunRecharge_float_bool += FuckAspdScalingOnCooldowns;
            IL.RoR2.Skills.SkillDef.GetRechargeRate += FuckAspdScalingOnCooldowns;

            void FuckAspdScalingOnCooldowns(ILContext il)
            {
                ILCursor c = new ILCursor(il);

                bool ilFound = c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<RoR2.Skills.SkillDef>(nameof(RoR2.Skills.SkillDef.attackSpeedBuffsRestockSpeed))
                    );
                if (!ilFound)
                {
                    DebugBreakpoint(nameof(FuckAspdScalingOnCooldowns));
                    return;
                }
                c.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
            }
        }
        #endregion
    }
}
