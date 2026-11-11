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
using UnityEngine.AddressableAssets;
using SwanSongExtended.Items;
using System.Linq;

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
            QuickSection(RemoveItems, ": Disable Items Pending Reworks");
            void RemoveItems()
            {
                ChangeTrialDropTables();

                #region rework pending / priority removal
                //RiskierRainPlugin.RetierItemAsync(RoR2_DLC3_Items_BarrierOnCooldown.BarrierOnCooldown_asset);//eclipse lite
                RiskierRainPlugin.RetierItemAsync(RoR2_DLC3_Items_CritAtLowerElevation.CritAtLowerElevation_asset);//hikers boots
                RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_HealingPotion.HealingPotion_asset);//elixir

                //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.PrimarySkillShuriken)); //shuriken
                //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.MoveSpeedOnKill)); //hunter's harpoon
                //RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.Squid)); //squid polyp HAS BEEN REWORKED AND IS AWESOME NOW
                //RiskierRainPlugin.RetierItemAsync(RoR2_Base_BonusGoldPackOnKill.BonusGoldPackOnKill_asset); //ghors
                //RiskierRainPlugin.RetierItemAsync(RoR2_Base_DeathMark.DeathMark_asset); //guess faggot
                //RiskierRainPlugin.RetierItemAsync(RoR2_DLC3_Items_ShieldBooster.ShieldBooster_asset);//kinetic dampener
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
            }

            #region ehp
            BaseStats.BarrierDecayStaticMaxHealthTime = 30f;// BarrierDecayRateStatic.Value; //30f
            BaseStats.BarrierHighDecayFactor = 5f;// BarrierDecayHighFactor.Value; //3f
            BaseStats.BarrierLowDecayFactor = 0.33f;// BarrierDecayLowFactor.Value; //0.5f

            //FreeBonusArmor();
            QuickSection(ChangeSaferSpaces, "Items : Safer Spaces");
            //FuckingFixInfusion();
            QuickSection(ChangeTopazBrooch, "Items : Topaz Brooch");
            QuickSection(ChangeEclipseLite, "Items : Eclipse Lite");
            QuickSection(ChangeWarpedEcho, "Items : Warped Echo");
            QuickSection(ChangeVengefulSpirit, "Items : Vengeful Spirit");
            QuickSection(ChangeEarthboundVessel, "Items : Earthbound Vessel");
            #endregion

            #region healing
            QuickSection(ChangeMedkit, "Items : Medkit");
            QuickSection(ChangeMonsterTooth, "Items : Monster Tooth");
            QuickSection(ChangeBungus, "Items : Bustling Fungus");
            QuickSection(ChangeWeepingFungus, "Items : Weeping Fungus");
            QuickSection(ChangeScythe, "Items : Harvesters Scythe");
            QuickSection(ChangeLeptonDaisy, "Items : Lepton Daisy");
            QuickSection(ChangeLeechingSeed, "Items : Leeching Seed");
            #endregion

            #region mobility
            //GoatHoofNerf();
            QuickSection(ChangeFaraday, "Items : Faraday Spur");
            QuickSection(ChangeElusiveAntlers, "Items : Elusive Antlers");
            #endregion

            #region autoplay
            QuickSection(ChangeNkuhana, "Items : NKuhanas Opinion");
            QuickSection(ChangeDisciple, "Items : Little Disciple");
            QuickSection(ChangeWillowisp, "Items : Will-o-the-Wisp");
            QuickSection(ChangeShatterspleen, "Items : Shatterspleen");
            QuickSection(ChangeFireworks, "Items : Fireworks");
            QuickSection(ChangeVoidsent, "Items : Voidsent Flame");
            QuickSection(ChangeGasoline, "Items : Gasoline");
            #endregion

            #region damage
            QuickSection(ChangeRunald, "Items : Runalds Band");
            QuickSection(ChangeKjaro, "Items : NKuhanas Opinion");
            //NerfCritGlasses();
            QuickSection(ChangeDeathMark, "Items : Death Mark");
            QuickSection(ChangePolylute, "Items : Polylute");
            QuickSection(ChangeBoxOfDynamite, "Items : Box of Dynamite");
            QuickSection(ChangeLuminousShot, "Items : Luminous Shot");
            QuickSection(ChangeGenesisLoop, "Items : Genesis Loop");
            //ChangeJustice();
            //ChangePauldron();
            QuickSection(FixLostSeers, "Items : Lost Seers Lenses");
            //ChangeStickyBomb();
            QuickSection(ReworkShuriken, "Items : Shuriken");
            QuickSection(ChangeDormantSpirit, "Items : Dormant Spirit");
            QuickSection(ChangeWarHorn, "Items : Warhorn");
            #endregion

            #region economy
            QuickSection(ChangeChanceDoll, "Items : Chance Doll");
            QuickSection(ChangeSaleStar, "Items : Sale Star");
            #endregion

            #region utility
            QuickSection(ChangeFuelCell, "Items : Fuel Cell");
            QuickSection(ChangeWarbanner, "Items : Warbanner");
            //QuickBind(ChangeChronobauble, "Items : Chronobauble");
            QuickSection(ChangeDreamCrucible, "Items : Dream Crucible");
            QuickSection(ChangeStealthkit, "Items : Old War Stealthkit");
            #endregion

            #region misc
            QuickSection(RemoveAspdScalingOnCooldownsForever, ": Remove Attack Speed Scaling On Cooldowns");
            QuickSection(FixPickupStats, ": Increase Pickup Droplet Pickup Range");
            //MakeMinionsInheritOnKillEffects();
            #endregion

            QuickSection(() => On.RoR2.ChampRushMissionController.CompleteReaperFight += RegenerateReincarnate, "Items : Reincarnation");
        }

        private static void RegenerateReincarnate(On.RoR2.ChampRushMissionController.orig_CompleteReaperFight orig, ChampRushMissionController self)
        {
            orig(self);
            if (!NetworkServer.active)
                return;
            CursedBookSpellDef reincarnate = CursedBookSpellCatalog.GetCursedBookSpellDef(CursedBookSpellCatalog.FindSpellByItemIndex(DLC4Content.Items.PageDie.itemIndex));
            foreach (PlayerCharacterMasterController controller in PlayerCharacterMasterController._instancesReadOnly)
            {
                CharacterMaster master = controller.master;
                if (master == null)
                    continue;
                Inventory inv = master.inventory;
                if (inv == null)
                    continue;
                if(reincarnate.cooldownItemDef && inv.GetItemCountPermanent(reincarnate.cooldownItemDef) > 0)
                {
                    inv.RemoveItemPermanent(reincarnate.cooldownItemDef, 1);
                }

                Inventory.ItemTransformation.TryTransformResult tryTransformResult;
                new Inventory.ItemTransformation
                {
                    originalItemIndex = reincarnate.consumedItemDef.itemIndex,
                    newItemIndex = reincarnate.itemDef.itemIndex,
                    maxToTransform = int.MaxValue,
                    transformationType = (ItemTransformationTypeIndex)0
                }.TryTransform(inv, out tryTransformResult);
            }
        }


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
        #region trial drop tables
        public static string[] bannedTrialItemPaths = new string[]
        {
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_HealingPotion.HealingPotion_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC3_Items_ShieldBooster.ShieldBooster_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC3_Items_CritAtLowerElevation.CritAtLowerElevation_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Talisman.Talisman_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_PermanentDebuffOnHit.PermanentDebuffOnHit_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_OutOfCombatArmor.OutOfCombatArmor_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_ArmorPlate.ArmorPlate_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC3_Items_JumpDamageStrike.JumpDamageStrike_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_StunChanceOnHit.StunChanceOnHit_asset
        };
        public static ItemDef[] bannedTrialItemDefs;
        public static ExplicitPickupDropTable safeTrialItemDefs;
        [SystemInitializer(typeof(ItemCatalog), typeof(PickupCatalog))]
        private static void InitTrialBanList()
        {
            bannedTrialItemDefs = new ItemDef[]
            {
                RoR2Content.Items.Talisman,
                RoR2Content.Items.ArmorPlate,
                RoR2Content.Items.StunChanceOnHit,
                DLC1Content.Items.HealingPotion,
                DLC1Content.Items.PermanentDebuffOnHit,
                DLC1Content.Items.OutOfCombatArmor,
                DLC3Content.Items.ShieldBooster,
                DLC3Content.Items.CritAtLowerElevation,
                DLC3Content.Items.JumpDamageStrike
            };
            safeTrialItemDefs = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
            safeTrialItemDefs.pickupEntries = new ExplicitPickupDropTable.PickupDefEntry[]
            {
                GetPickupEntry(ManaFlower.instance?.ItemsDef ?? null, 0.01f),
                GetPickupEntry(DLC2Content.Items.LowerPricedChests, 0.2f),
                GetPickupEntry(DLC1Content.Items.FreeChest, 0.2f),
                GetPickupEntry(DLC1Content.Items.AttackSpeedAndMoveSpeed),
                GetPickupEntry(RoR2Content.Items.Feather, 0.2f),
                GetPickupEntry(RoR2Content.Items.IgniteOnKill),
                GetPickupEntry(RoR2Content.Items.Hoof),
                GetPickupEntry(RoR2Content.Items.Syringe)
            };

        }
        private static ExplicitPickupDropTable.PickupDefEntry GetPickupEntry(ItemDef itemDef, float weight = 1)
        {
            ExplicitPickupDropTable.PickupDefEntry entry = new ExplicitPickupDropTable.PickupDefEntry();
            entry.pickupDef = itemDef;
            entry.pickupWeight = weight;
            return entry;
        }
        public static void ChangeTrialDropTables()
        {
            On.RoR2.ItemSpawnPoint.Awake += ChangeDisabledTrialItems;

            RiskierRainPlugin.LoadAsync<ExplicitPickupDropTable>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4_sadism_sad1.dtSad1RentalsExplicit_OpenChests_asset, (dropTable) =>
            {
                dropTable.pickupEntries[1] = GetPickupEntry(RoR2Content.Items.Squid, 0.3f);//stun grenade -> squid polyp
            });
            RiskierRainPlugin.LoadAsync<ExplicitPickupDropTable>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4_sadism_sad1.dtSad1RentalsExplicit_Money3_asset, (dropTable) =>
            {
                dropTable.pickupEntries[0] = GetPickupEntry(RoR2Content.Items.Thorns, 0.3f);//guillotine -> razorwire
                dropTable.pickupEntries[2].pickupDef = RoR2Content.Items.ExplodeOnDeath;//willowisp
            });
        }

        private static void ChangeDisabledTrialItems(On.RoR2.ItemSpawnPoint.orig_Awake orig, RoR2.ItemSpawnPoint self)
        {
            if (self.useExplicitItemDef)
            {
                bool banned = false;
                if (self._explicitItemDefAddress.RuntimeKeyIsValid())
                {
                    if (bannedTrialItemPaths.Contains(self._explicitItemDefAddress.AssetGUID))
                    {
                        banned = true;
                    }
                }
                else if (self._explicitItemDefReference != null)
                {
                    if (bannedTrialItemDefs != null && bannedTrialItemDefs.Contains(self._explicitItemDefReference))
                    {
                        banned = true;
                    }
                }

                if (banned)
                {
                    self.useExplicitItemDef = false;
                    self.dropTable = safeTrialItemDefs;
                }
            }
            orig(self);
        }

        #endregion
    }
}
