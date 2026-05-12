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

namespace RiskierRain.Changes
{
    public static partial class ItemChanges
    {
        #region safer spaces void bear
        public static float voidBearNewMaxCooldown = 15f;
        public static float voidBearNewMinCooldown = 3f;
        //public static float teddyNewMaxValue = 0.5f; //1.0
        public static void ChangeSaferSpaces()
        {
            //IL.RoR2.HealthComponent.TakeDamageProcess += TeddyChanges;
            IL.RoR2.HealthComponent.TakeDamageProcess += VoidBearChanges;
            //LanguageAPI.Add("ITEM_BEAR_DESC",
            //    $"<style=cIsHealing>{15 * teddyNewMaxValue}%</style> " +
            //    $"<style=cStack>(+{15 * teddyNewMaxValue}% per stack)</style> " +
            //    $"chance to <style=cIsHealing>block</style> incoming damage, " +
            //    $"up to a maximum of <style=cIsHealing>{Tools.ConvertDecimal(teddyNewMaxValue)}</style>. " +
            //    $"<style=cIsUtility>Unaffected by luck</style>.");
            LanguageAPI.Add("ITEM_BEARVOID_DESC",
                $"<style=cIsHealing>Blocks</style> incoming damage once. " +
                $"Recharges after <style=cIsUtility>{voidBearNewMaxCooldown} seconds</style> <style=cStack>(-10% per stack)</style>, " +
                $"to a minimum of <style=cIsUtility>{voidBearNewMinCooldown} seconds</style>. " +
                $"<style=cIsVoid>Corrupts all Tougher Times</style>.");
        }
        public static void VoidBearChanges(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = 14;
            bool b = c.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "BearVoid")
                )
            && c.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchStloc(out countLoc)
                )
            && c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.AddTimedBuff))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(VoidBearChanges));
                return;
            }
            c.EmitDelegate<Func<float, float>>((inDuration) =>
            {
                float baseDuration = 15;
                float outDuration = 5;
                outDuration += inDuration * ((baseDuration - outDuration) / baseDuration);
                return outDuration;
            });
        }
        //public static void TeddyChanges(ILContext il)
        //{
        //    ILCursor c = new ILCursor(il);
        //
        //    c.GotoNext(MoveType.AfterLabel,
        //        x => x.MatchLdfld("RoR2.HealthComponent/ItemCounts", "bear")
        //        );
        //    c.GotoNext(MoveType.After,
        //        x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.ConvertAmplificationPercentageIntoReductionPercentage))
        //        );
        //    c.Emit(OpCodes.Ldc_R4, teddyNewMaxValue);
        //    c.Emit(OpCodes.Mul);
        //}
        #endregion

        #region bonus armor knurl buckler
        public static int knurlFreeArmor = 15;
        public static int bucklerFreeArmor = 10;

        public static void FreeBonusArmor()
        {
            GetStatCoefficients += FreeBonusArmor;
            LanguageAPI.Add("ITEM_KNURL_PICKUP", "Boosts health, regeneration, and armor.");
            LanguageAPI.Add("ITEM_KNURL_DESC",
                $"<style=cIsHealing>Increase maximum health</style> by <style=cIsHealing>40</style> <style=cStack>(+40 per stack)</style>, " +
                $"<style=cIsHealing>base health regeneration</style> by <style=cIsHealing>+1.6 hp/s <style=cStack>(+1.6 hp/s per stack)</style>, and " +
                $"<style=cIsHealing>armor</style> by <style=cIsHealing>{knurlFreeArmor} <style=cStack>(+{knurlFreeArmor} per stack)</style>.");
            LanguageAPI.Add("ITEM_SPRINTARMOR_DESC",
                $"<style=cIsHealing>Increase armor</style> by <style=cIsHealing>{bucklerFreeArmor}</style> <style=cStack>(+{bucklerFreeArmor} per stack)</style>, and another " +
                $"<style=cIsHealing>30</style> <style=cStack>(+30 per stack)</style> <style=cIsUtility>while sprinting</style>.");
        }
        public static void FreeBonusArmor(CharacterBody sender, StatHookEventArgs args)
        {
            float freeArmor = 0;

            Inventory inv = sender.inventory;
            if (inv != null)
            {
                freeArmor += inv.GetItemCountEffective(RoR2Content.Items.SprintArmor) * bucklerFreeArmor;
                freeArmor += inv.GetItemCountEffective(RoR2Content.Items.Knurl) * knurlFreeArmor;
            }

            args.armorAdd += freeArmor;
        }
        #endregion

        #region infusion
        public static float newInfusionBaseHealth = 40;

        public static void FuckingFixInfusion()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath += InfusionBuff;
            LanguageAPI.Add("ITEM_INFUSION_PICKUP",
            "Killing an enemy permanently increases your base health.");
            LanguageAPI.Add("ITEM_INFUSION_DESC",
                $"Killing an enemy increases your <style=cIsHealing>base health permanently</style> by <style=cIsHealing>1</style> <style=cStack>(+1 per stack)</style>, " +
                $"up to a <style=cIsHealing>maximum</style> of <style=cIsHealing>{newInfusionBaseHealth} <style=cStack>(+{newInfusionBaseHealth} per stack)</style> health</style>.");
        }

        private static void InfusionBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int attackerBodyLoc = 15; //really need to be getting this through IL but i dont care tbh
            int countLoc = 43;
            int capLoc = 63;

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Infusion"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out countLoc)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out countLoc),
                x => x.MatchLdcI4(out _),
                x => x.MatchMul(),
                x => x.MatchStloc(out capLoc)
                );
            c.Index--;

            c.Emit(OpCodes.Ldloc, countLoc);
            c.Emit(OpCodes.Ldloc, attackerBodyLoc); //body loc
            c.EmitDelegate<Func<int, int, RoR2.CharacterBody, int>>((currentInfusionCap, infusionCount, body) =>
            {
                float newInfusionCap = 100 * infusionCount;

                if (body != null)
                {
                    float levelBonus = 1 + 0.3f * (body.level - 1);

                    newInfusionCap = newInfusionBaseHealth * levelBonus * infusionCount;
                }

                return (int)newInfusionCap;
            });
        }
        #endregion

        #region topaz brooch

        public static float broochPercentBase = 0.02f;
        public static float broochPercentStack = 0.0f;
        public static float broochFlatBase = 15f;//15f
        public static float broochFlatStack = 15f;//15f

        public static void ChangeTopazBrooch()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath += TopazBroochPercentBarrier;

            LanguageAPI.Add("ITEM_BARRIERONKILL_DESC",
                $"Gain a <style=cIsHealing>temporary barrier</style> on kill " +
                $"for <style=cIsHealing>15 health <style=cStack>(+15 per stack)</style></style> " +
                $"PLUS <style=cIsHealing>{broochPercentBase.AsPercent()}</style> of your <style=cIsHealing>maximum health</style>.");
        }

        private static void TopazBroochPercentBarrier(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int broochCountLoc = 55;
            int bodyLoc = 16;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", nameof(RoR2Content.Items.BarrierOnKill)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(out broochCountLoc))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out bodyLoc),
                x => x.MatchCallOrCallvirt<CharacterBody>("get_healthComponent"))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.AddBarrier))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(TopazBroochPercentBarrier));
                return;
            }

            c.Emit(OpCodes.Ldloc, broochCountLoc);
            c.Emit(OpCodes.Ldloc, bodyLoc);
            c.EmitDelegate<Func<float, int, CharacterBody, float>>((barrierIn, stack, body) =>
            {
                if (body == null)
                    return barrierIn;

                float percentInBarrier = broochPercentBase + (broochPercentStack * (stack - 1));
                return barrierIn + body.healthComponent.fullCombinedHealth * percentInBarrier;
            });
        }
        #endregion

        #region eclipse lite

        public static float eclipseLiteHealPerSecondBase = 1f;
        public static float eclipseLiteHealPerSecondStack = 1f;
        public static void ChangeEclipseLite()
        {
            AIBlacklistThisItem(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_BarrierOnCooldown.BarrierOnCooldown_asset);
            On.RoR2.CharacterBody.OnSkillCooldown += FixEclipseLiteRestockScaling;
            IL.RoR2.CharacterBody.OnSkillCooldown += ChangeEclipseLiteStats;

            LanguageAPI.Add("ITEM_BARRIERONCOOLDOWN_PICKUP", "Gain a small heal when a skill comes off cooldown.");
            LanguageAPI.Add("ITEM_BARRIERONCOOLDOWN_DESC",
                $"When a skill comes off cooldown, <style=cIsHealing>heal</style> for " +
                $"<style=cIsHealing>{eclipseLiteHealPerSecondBase} <style=cStack>(+{eclipseLiteHealPerSecondStack} per stack)</style> health</style>. " +
                $"Scales with the skill's base cooldown.");
        }

        private static void FixEclipseLiteRestockScaling(On.RoR2.CharacterBody.orig_OnSkillCooldown orig, CharacterBody self, GenericSkill skill, int restocks)
        {
            if (restocks > 1)
            {
                int rechargeStock = skill.skillDef.GetRechargeStock(skill);
                if (rechargeStock > 1)
                    restocks = Mathf.CeilToInt(restocks / rechargeStock);
            }
            orig(self, skill, restocks);
        }

        private static void ChangeEclipseLiteStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ChangeEclipseLiteToHealing(c);
            RemoveEclipseLiteMaxHealthScaling(c);
        }

        private static HealthComponent ecliteThingy = null;
        private static void ChangeEclipseLiteToHealing(ILCursor c)
        {
            c.Index = 0;

            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.AddBarrierAuthority))
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(ChangeEclipseLiteToHealing));
                return;
            }

            c.Remove();
            c.EmitDelegate<Action<HealthComponent, float>>((healthComponent, value) =>
            {
                ecliteThingy = healthComponent;
                healthComponent.Heal(value, default(ProcChainMask), true);
            });
        }

        private static void RemoveEclipseLiteMaxHealthScaling(ILCursor c)
        {
            c.Index = 0;

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxHealth")
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(RemoveEclipseLiteMaxHealthScaling), 1);
                return;
            }
            //replace max health with 1
            c.EmitDelegate<Func<float, float>>((maxHealthWhichFunctionsAsAMultiplier) => { return 1; });

            //change the fraction values to not be fractions of 1
            ChangeSingleValue(eclipseLiteHealPerSecondBase, index: 1);
            ChangeSingleValue(eclipseLiteHealPerSecondStack, index: 2);

            void ChangeSingleValue(float newValue, int index)
            {
                bool b2 = c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdcR4(out _)
                    );
                if (!b2)
                {
                    DebugBreakpoint($"{nameof(RemoveEclipseLiteMaxHealthScaling)}:{nameof(ChangeSingleValue)}", 1);
                    return;
                }
                c.Next.Operand = newValue;
            }
        }

        private static void RemoveEclipseLiteRestockScaling(ILCursor c)
        {
            c.Index = 0;

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(2),
                x => x.MatchConvR4()
                );
            if (!b)
            {
                DebugBreakpoint(nameof(RemoveEclipseLiteRestockScaling));
                return;
            }

            c.EmitDelegate<Func<float, GenericSkill, float>>((restocks, skill) =>
            {
                int rechargeStock = skill.skillDef.GetRechargeStock(skill);
                if (rechargeStock > 1)
                {
                    restocks /= rechargeStock;
                }
                return restocks;
            });
            //c.EmitDelegate<Func<int, int>>((_) => { return 1; });
        }
        #endregion

        #region warped echo

        public static float warpedEchoDamageReduction = 0.3f;
        public static void ChangeWarpedEcho()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += WarpedEchoDamageReduction;

            LanguageAPI.Add("ITEM_DELAYEDDAMAGE_DESC",
                $"The next source of damage is <style=cIsHealing>reduced</style> by " +
                $"<style=cIsHealing>{warpedEchoDamageReduction * 100}%</style> and " +
                $"<style=cIsHealing>spread</style> into <style=cIsUtility>3 <style=cStack>(+1 per stack)</style> hits</style>. " +
                $"Recharges every <style=cIsUtility>15s</style>.");
        }

        private static void WarpedEchoDamageReduction(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Items", nameof(DLC2Content.Items.DelayedDamage)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(0.9f)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(WarpedEchoDamageReduction));
                return;
            }

            c.Next.Operand = 1 - warpedEchoDamageReduction;
        }
        #endregion

        #region medkit
        public static float medkitFlatHeal = 40; //20
        public static float medkitPercentHeal = 0.00f; //0.05
        public static void ChangeMedkit()
        {
            AIBlacklistThisItem(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Medkit.Medkit_asset);
            LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Medkit.bdMedkitHeal_asset, (buffDef) =>
            {
                buffDef.isDebuff = true;
                buffDef.isHidden = true;
            });
            IL.RoR2.CharacterBody.RemoveBuff_BuffIndex += MedkitHealChange;
            LanguageAPI.Add("ITEM_MEDKIT_DESC",
                $"2 seconds after getting hurt, <style=cIsHealing>heal</style> for " +
                $" <style=cIsHealing>{medkitFlatHeal} health</style> <style=cStack>(+{medkitFlatHeal} per stack)</style>.");
        }

        public static void MedkitHealChange(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = -1;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Medkit"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out countLoc)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(MedkitHealChange), 1);
                return;
            }

            //match to flat heal location
            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(20),
                x => x.MatchStloc(out _)
                );
            if (b2)
            {
                c.Index++;
                c.Emit(OpCodes.Ldloc, countLoc);
                c.EmitDelegate<Func<float, int, float>>((currentHealAmt, itemCount) =>
                {
                    float newFlatHealAmt = medkitFlatHeal * (itemCount);

                    return newFlatHealAmt;
                });
            }
            else
            {
                DebugBreakpoint(nameof(MedkitHealChange), 2);
            }


            //match to percent heal location
            bool b3 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxHealth"),
                x => x.MatchLdcR4(0.05f)
                );
            if (b3)
            {
                c.EmitDelegate<Func<float, float>>((currentHealAmt) =>
                {
                    float newPercentHealAmt = medkitPercentHeal;

                    return newPercentHealAmt;
                });
            }
            else
            {
                DebugBreakpoint(nameof(MedkitHealChange), 3);
            }
        }
        #endregion

        #region monster tooth
        public static float monsterToothFlatHeal = 10; //8
        public static float monsterToothPercentHeal = 0.00f; //0.02
        public static float monsterToothPickupDuration = 15; //5
        public static void ChangeMonsterTooth()
        {
            AIBlacklistThisItem(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Tooth.Tooth_asset);
            IL.RoR2.GlobalEventManager.OnCharacterDeath += MonsterToothHealChange;
            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Tooth.HealPack_prefab, MonsterToothDurationBuff);
            LanguageAPI.Add("ITEM_TOOTH_DESC",
            $"Killing an enemy spawns a <style=cIsHealing>healing orb</style> that heals for " +
            $"<style=cIsHealing>{monsterToothFlatHeal} health</style> <style=cStack>(+{monsterToothFlatHeal} per stack)</style>.");
        }

        private static void MonsterToothDurationBuff(GameObject healPack)
        {
            healPack.GetComponent<DestroyOnTimer>().duration = monsterToothPickupDuration;
            healPack.GetComponent<BeginRapidlyActivatingAndDeactivating>().delayBeforeBeginningBlinking = (monsterToothPickupDuration - 2f);
        }
        private static void MonsterToothHealChange(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = -1;
            bool b1 = c.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Tooth"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out countLoc))
            && c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<RoR2.HealthPickup>("flatHealing")
                );
            if(!b1)
            {
                DebugBreakpoint(nameof(MonsterToothHealChange), 1);
                return;
            }
            c.Emit(OpCodes.Ldloc, countLoc);
            c.EmitDelegate<Func<float, int, float>>((currentHealAmt, itemCount) =>
            {
                float newFlatHealAmt = monsterToothFlatHeal * itemCount;

                return newFlatHealAmt;
            });


            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<RoR2.HealthPickup>("fractionalHealing")
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(MonsterToothHealChange), 2);
                return;
            }
            c.EmitDelegate<Func<float, float>>((currentHealAmt) =>
            {
                float newPercentHealAmt = monsterToothPercentHeal;

                return newPercentHealAmt;
            });
        }
        #endregion

        #region weeping fungus wungus
        public static float wungusRegenBase = 1.5f;
        public static float wungusRegenStack = 1.5f;
        public static void ChangeWeepingFungus()
        {
            AIBlacklistThisItem(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_MushroomVoid.MushroomVoid_asset);
            LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_MushroomVoid.bdMushroomVoidActive_asset, (buffDef) =>
            {
                buffDef.isHidden = true;
            });
            GetStatCoefficients += WungusRegen;
            On.RoR2.MushroomVoidBehavior.FixedUpdate += FuckWungusHeal;

            LanguageAPI.Add("ITEM_MUSHROOMVOID_PICKUP", "Regenerate health while sprinting. <style=cIsVoid>Corrupts all Bustling Fungi</style>.");
            LanguageAPI.Add("ITEM_MUSHROOMVOID_DESC",
                $"Increases <style=cIsHealing>base health regeneration</style> " +
                $"by <style=cIsHealing>+{wungusRegenBase} hp/s</style> " +
                $"<style=cStack>(+{wungusRegenStack} hp/s per stack)</style> <style=cIsUtility>while sprinting</style>. " +
                $"<style=cIsVoid>Corrupts all Bustling Fungi</style>.");
        }

        public static void WungusRegen(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(DLC1Content.Buffs.MushroomVoidActive))
            {
                if (sender.inventory)
                {
                    int wungusCount = sender.inventory.GetItemCountEffective(DLC1Content.Items.MushroomVoid);
                    args.baseRegenAdd += wungusRegenBase + wungusRegenStack * (wungusCount - 1) * (1 + sender.level * 0.2f);
                }
            }
        }

        public static void FuckWungusHeal(On.RoR2.MushroomVoidBehavior.orig_FixedUpdate orig, MushroomVoidBehavior self)
        {
            self.healTimer = 0;
            orig(self);
            self.healTimer = 0;
        }
        #endregion

        #region bustling fungus bungus
        public static float fungusHealInterval = 0.125f;
        public static void ChangeBungus()
        {
            AIBlacklistThisItem(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Mushroom.Mushroom_asset);
            LanguageAPI.Add("ITEM_MUSHROOM_PICKUP", "Heal all nearby allies while standing still.");
            LanguageAPI.Add("ITEM_MUSHROOM_DESC", $"After standing still for <style=cIsHealing>{notMovingRequirement}s</style>, " +
                $"create a zone that <style=cIsHealing>heals</style> for " +
                $"<style=cIsHealing>4.5%</style> <style=cStack>(+2.25% per stack)</style> " +
                $"of your <style=cIsHealing>health</style> every second to " +
                $"all allies within <style=cIsHealing>3m</style> <style=cStack>(+1.5m per stack)</style>.");
            IL.RoR2.CharacterBody.GetNotMoving += ReduceBungusWaitTime;
            IL.RoR2.Items.MushroomBodyBehavior.FixedUpdate += ReduceBungusInterval;
        }

        public static void ReduceBungusInterval(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<HealingWard>(nameof(HealingWard.interval)));
            //c.Prev.Operand = fungusHealInterval;
            if (!b)
            {
                DebugBreakpoint(nameof(ReduceBungusInterval));
                return;
            }
            c.EmitDelegate<Func<float, float>>((interval) =>
            {
                return fungusHealInterval;
            });
        }

        public static void ReduceBungusWaitTime(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.notMovingStopwatch)));
            if (!b)
            {
                DebugBreakpoint(nameof(ReduceBungusWaitTime));
                return;
            }
            c.Next.Operand = notMovingRequirement;
        }
        #endregion

        #region harvesters scythe
        public static float scytheBaseHeal = 0f; //4
        public static float scytheStackHeal = 5f; //4
        public static void ChangeScythe()
        {
            AIBlacklistThisItem(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_HealOnCrit.HealOnCrit_asset);
            IL.RoR2.GlobalEventManager.OnCrit += ScytheNerf;
            LanguageAPI.Add("ITEM_HEALONCRIT_DESC",
                $"Gain <style=cIsDamage>5% critical chance</style>. <style=cIsDamage>Critical strikes</style> <style=cIsHealing>heal</style> for " +
                $"<style=cIsHealing>{scytheBaseHeal + scytheStackHeal}</style> <style=cStack>(+{scytheStackHeal} per stack)</style> <style=cIsHealing>health</style>.");
        }
        private static void ScytheNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = -1;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "HealOnCrit")
                )
            && c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(out countLoc)
                )
            && c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<RoR2.HealthComponent>(nameof(RoR2.HealthComponent.Heal))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(ScytheNerf));
                return;
            }

            c.Index -= 2;
            c.Emit(OpCodes.Ldloc, countLoc);
            c.EmitDelegate<Func<float, int, float>>((currentHealAmt, itemCount) =>
            {
                float newHealAmt = scytheBaseHeal + scytheStackHeal * itemCount;

                return newHealAmt;
            });
        }
        #endregion

        #region lepton daisy
        public static float daisyRadiusMultiplier = 1.15f; //increase by 10%
        public static void ChangeLeptonDaisy()
        {
            On.RoR2.HoldoutZoneController.OnEnable += DaisyRadiusIncrease;
        }

        private static void DaisyRadiusIncrease(On.RoR2.HoldoutZoneController.orig_OnEnable orig, HoldoutZoneController self)
        {
            orig(self);
            int itemCount = Util.GetItemCountForTeam(TeamIndex.Player, RoR2Content.Items.TPHealingNova.itemIndex, false);
            if (itemCount > 0)
            {
                self.baseRadius *= daisyRadiusMultiplier;
            }
        }
        #endregion

        #region leeching seed
        public static void ChangeLeechingSeed()
        {
            AIBlacklistThisItem(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Seed.Seed_asset);
        }
        #endregion

        #region goat hoof ghoof
        public static float hoofSpeedBonusBase = 0.14f; //0.14
        public static float hoofSpeedBonusStack = 0.14f; //0.14
        public static void GoatHoofNerf()
        {
            IL.RoR2.CharacterBody.RecalculateStats += HoofNerf;
            LanguageAPI.Add("ITEM_HOOF_DESC",
                $"Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>{Tools.ConvertDecimal(hoofSpeedBonusBase)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(hoofSpeedBonusStack)} per stack)</style>.");
        }
        public static void HoofNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = 6;
            bool b = 
            c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Hoof")
                )
            && c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(out countLoc)
                )
            && c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(countLoc),
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(HoofNerf));
                return;
            }
            c.EmitDelegate<Func<float, float, float>>((itemCount, speedBonus) =>
            {
                float newSpeedBonus = 0;
                if (itemCount > 0)
                {
                    newSpeedBonus = hoofSpeedBonusBase + (hoofSpeedBonusStack * (itemCount - 1));
                }
                return newSpeedBonus;
            });
            c.Remove();
        }
        #endregion

        #region faraday spurs
        public static float faradayMaxMoveSpeed = 0.8f; //1.6f
        public static float faradayMaxJumpStrength = 1.5f; //2.0f
        public static float faradayChargeIncreaseBase = 1.5f; //1.0f
        public static float faradayChargeIncreaseStack = 0.5f; //0.0f
        public static float faradayDamageBase = 8f; //4.0f
        public static float faradayDamageStack = 5f; //2.5f
        public static int faradayRequiredCharge = 34; //25
        public static int faradayMaxDischarge = 67; //100
        public static bool faradayPreventDoubleDischarge = true;
        private static float faradayChargeIncreaseStackInverse => faradayChargeIncreaseStack / (1 + faradayChargeIncreaseStack);
        public static void ChangeFaraday()
        {
            LanguageAPI.Add("ITEM_JUMPDAMAGESTRIKE_PICKUP",
                $"Moving around builds up movement speed and jump height. " +
                $"At {faradayRequiredCharge}% charge or higher, jump to discharge into an electric blast.");
            LanguageAPI.Add("ITEM_JUMPDAMAGESTRIKE_DESC",
                $"Moving around builds up <style=cIsUtility>charge</style> <style=cStack>(+{faradayChargeIncreaseStackInverse * 100}% faster per stack)</style>, " +
                $"granting up to <style=cIsUtility>+{faradayMaxMoveSpeed * 100}% movement speed</style> " +
                $"and <style=cIsUtility>+{faradayMaxJumpStrength * 100}% jump strength</style> at 100%. " +
                $"At {faradayRequiredCharge}% charge or higher, jumping triggers an <style=cIsDamage>explosive discharge</style> " +
                $"for <style=cIsDamage>{faradayDamageBase * 100}% <style=cStack>(+{faradayDamageStack * 100}% per stack)</style> damage</style> " +
                $"in a 5m to 32.3m <style=cStack>(+7.5m per stack)</style> area.");

            JumpDamageStrikeBodyBehavior.MoveSpeedVelocityPerCharge = faradayMaxMoveSpeed / 100;
            JumpDamageStrikeBodyBehavior.JumpVelocityPerCharge = faradayMaxJumpStrength / 100;
            IL.RoR2.Items.JumpDamageStrikeBodyBehavior.UpdateCharge += FaradayUpdateCharge;
            IL.RoR2.Items.JumpDamageStrikeBodyBehavior.GetRadius += FaradayGetRadius;
            IL.RoR2.Items.JumpDamageStrikeBodyBehavior.DischargeEffects += FaradayDischargeEffects;
            IL.RoR2.JumpDamageStrikeSparks.FixedUpdate += FaradaySparks;
        }

        public static void FaradaySparks(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC3Content/Buffs", "JumpDamageStrikeCharge"),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.GetBuffCount)),
                x => x.MatchLdcI4(out _)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(FaradaySparks));
                return;
            }
            c.Index--;
            c.Next.Operand = faradayRequiredCharge;
        }

        public static void FaradayDischargeEffects(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            IncreaseFaradayChargeRequirement_DischargeEffects(c);
            c.Index = 0;
            ReduceFaradayMaxDischarge(c);
            c.Index = 0;
            IncreaseFaradayDischargeDamage(c);
            c.Index = 0;
            ChangeFaradayDischargeDamageType(c);
        }

        public static void ChangeFaradayDischargeDamageType(ILCursor c)
        {
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<BlastAttack>(nameof(BlastAttack.damageType))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(ChangeFaradayDischargeDamageType));
                return;
            }
            c.EmitDelegate<Func<DamageTypeCombo, DamageTypeCombo>>((doesntMatter) =>
            {
                return new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.Generic, DamageSource.Special);
            });
        }

        public static void IncreaseFaradayDischargeDamage(ILCursor c)
        {
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_damage")
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(IncreaseFaradayDischargeDamage), 1);
                return;
            }

            bool b2 = c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<BaseItemBodyBehavior>(nameof(BaseItemBodyBehavior.stack))
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(IncreaseFaradayDischargeDamage), 2);
                return;
            }
            c.Next.Operand = faradayDamageBase;
            c.Index++;
            c.Next.Operand = faradayDamageStack;
        }

        public static void ReduceFaradayMaxDischarge(ILCursor c)
        {
            int buffCountLoc = 0;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC3Content/Buffs", "JumpDamageStrikeCharge"),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.GetBuffCount)),
                x => x.MatchStloc(out buffCountLoc)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(ReduceFaradayMaxDischarge), 1);
                return;
            }

            bool b2 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC3Content/Buffs", "JumpDamageStrikeCharge"),
                x => x.MatchCallOrCallvirt<BuffDef>("get_buffIndex"),
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.SetBuffCount))
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(ReduceFaradayMaxDischarge), 2);
                return;
            }
            c.Index--;
            c.Emit(OpCodes.Ldloc, buffCountLoc);
            c.EmitDelegate<Func<int, int, int>>((doesntMatter, buffCount) =>
            {
                int newBuffCount = buffCount - faradayMaxDischarge;
                if (newBuffCount < 0)
                    return 0;
                if (newBuffCount == faradayRequiredCharge && faradayPreventDoubleDischarge)
                    return faradayRequiredCharge - 1;
                return newBuffCount;
            });
        }

        public static void IncreaseFaradayChargeRequirement_DischargeEffects(ILCursor c)
        {
            int buffCountLoc = 0;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC3Content/Buffs", "JumpDamageStrikeCharge"),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.GetBuffCount)),
                x => x.MatchStloc(out buffCountLoc)
                )
                &&
                c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(buffCountLoc),
                x => x.MatchLdcI4(out _),
                x => x.MatchBge(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(IncreaseFaradayChargeRequirement_DischargeEffects));
                return;
            }
            c.Index++;
            c.Next.Operand = faradayRequiredCharge;
        }

        public static void FaradayGetRadius(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            IncreaseFaradayChargeRequirement_GetRadius(c);
            c.Index = 0;
        }

        public static void FaradayUpdateCharge(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            FixFaradayDeltaTime(c);
            c.Index = 0;
            BuffFaradayChargeRate(c);
            c.Index = 0;
            IncreaseFaradayChargeRequirement_UpdateCharge(c);
        }

        public static void IncreaseFaradayChargeRequirement_UpdateCharge(ILCursor c)
        {
            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(out _),
                x => x.MatchClt()
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(IncreaseFaradayChargeRequirement_UpdateCharge));
                return;
            }
            c.Index++;
            c.Next.Operand = faradayRequiredCharge;
        }
        public static void IncreaseFaradayChargeRequirement_GetRadius(ILCursor c)
        {
            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(1),
                x => x.MatchLdcI4(out _),
                x => x.MatchBge(out _)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(IncreaseFaradayChargeRequirement_GetRadius));
                return;
            }
            c.Index++;
            c.Next.Operand = faradayRequiredCharge;
        }

        public static void BuffFaradayChargeRate(ILCursor c)
        {
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<JumpDamageStrikeBodyBehavior>(nameof(JumpDamageStrikeBodyBehavior.isCharging))
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(BuffFaradayChargeRate), 1);
                return;
            }

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<JumpDamageStrikeBodyBehavior>(nameof(JumpDamageStrikeBodyBehavior.distanceTraveled))
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(BuffFaradayChargeRate), 2);
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, JumpDamageStrikeBodyBehavior, float>>((addedDistance, behavior) =>
            {
                float multiplier = Mathf.Pow((1 + faradayChargeIncreaseStack * (behavior.stack - 1)) * faradayChargeIncreaseBase, 0.2f);
                return addedDistance * multiplier;
            });
        }

        private static void FixFaradayDeltaTime(ILCursor c)
        {
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<Time>("get_deltaTime")
                );

            if (b)
            {
                c.Remove();
                c.EmitDelegate<Func<float>>(() =>
                {
                    return Time.fixedDeltaTime;
                });
            }
            else
            {
                DebugBreakpoint(nameof(FixFaradayDeltaTime));
            }
        }
        #endregion

        #region elusive antlers
        public static float elusiveAntlersPickupDuration = 24f;//60f
        public static float elusiveAntlersBuffDuration = 18f;//12f
        public static float elusiveAntlersPickupInterval = 12f;//10f
        public static float elusiveAntlersPickupIntervalReductionStack = 0.1f;//0.1f
        public static float elusiveAntlersMoveSpeedPerBuff = 0.06f; //0.12f
        public static float elusiveAntlersFreeMovespeedBase = 0.06f; //0f
        public static float elusiveAntlersFreeMovespeedStack = 0.06f; //0f
        public static void ChangeElusiveAntlers()
        {
            LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_SpeedBoostPickup.ElusiveAntlersPickup_prefab, (pickupObject) =>
            {
                if (pickupObject.TryGetComponent(out BeginRapidlyActivatingAndDeactivating flasher))
                {
                    flasher.delayBeforeBeginningBlinking = elusiveAntlersPickupDuration - 2;
                }
                //destroying on timer is manually implemented in ElusiveAntlersPickup
                //if(pickupObject.TryGetComponent(out DestroyOnTimer timer))
                //{
                //    timer.duration = elusiveAntlersPickupDuration;
                //}
            });
            On.RoR2.ElusiveAntlersPickup.Start += ElusiveAntlersPickupStats;
            IL.RoR2.CharacterBody.RecalculateStats += ElusiveAntlersBuffMoveSpeed;
            IL.RoR2.ElusiveAntlersBehavior.FixedUpdate += ElusiveAntlersPickupInterval;
            GetStatCoefficients += ElusiveAntlersBaseMovespeed;

            LanguageAPI.Add("ITEM_SPEEDBOOSTPICKUP_DESC",
                $"Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>{elusiveAntlersFreeMovespeedBase.AsPercent()}</style> " +
                $"<style=cStack>(+{elusiveAntlersFreeMovespeedStack.AsPercent()} per stack)</style>. " +
                $"Every <style=cIsUtility>{elusiveAntlersPickupInterval}s</style> " +
                $"<style=cStack>(-{elusiveAntlersPickupIntervalReductionStack.AsPercent()} per stack)</style>, " +
                $"spawn an orb of energy nearby granting " +
                $"<style=cIsUtility>+{elusiveAntlersMoveSpeedPerBuff.AsPercent()} movement speed</style> up to " +
                $"<style=cIsUtility>3 <style=cStack>(+3 per stack)</style> " +
                $"times</style> for <style=cIsUtility>{elusiveAntlersBuffDuration}s</style>.");
        }

        private static void ElusiveAntlersPickupInterval(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<ElusiveAntlersBehavior>(nameof(ElusiveAntlersBehavior.spawnTimer)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<ElusiveAntlersBehavior>(nameof(ElusiveAntlersBehavior.spawnTimer))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(ElusiveAntlersPickupInterval));
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, ElusiveAntlersBehavior, float>>((timerOld, self) =>
            {
                float interval = elusiveAntlersPickupInterval;
                if (self.body.inventory)
                {
                    int itemCount = self.body.inventory.GetItemCountEffective(DLC2Content.Items.SpeedBoostPickup);
                    if (itemCount > 1)
                    {
                        interval *= Mathf.Pow(1f - elusiveAntlersPickupIntervalReductionStack, itemCount - 1f);
                    }
                }
                return interval;
            });
        }

        private static void ElusiveAntlersPickupStats(On.RoR2.ElusiveAntlersPickup.orig_Start orig, ElusiveAntlersPickup self)
        {
            self.despawnMinAge = elusiveAntlersPickupDuration;
            self.shardPickupBuffTimeSeconds = elusiveAntlersBuffDuration;
            orig(self);
        }

        private static void ElusiveAntlersBuffMoveSpeed(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Buffs", nameof(DLC2Content.Buffs.ElusiveAntlersBuff)))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdcR4(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(ElusiveAntlersBuffMoveSpeed));
                return;
            }
            c.Next.Operand = elusiveAntlersMoveSpeedPerBuff;
        }

        private static void ElusiveAntlersBaseMovespeed(CharacterBody sender, StatHookEventArgs args)
        {
            if (!sender.inventory)
                return;
            int itemCount = sender.inventory.GetItemCountEffective(DLC2Content.Items.SpeedBoostPickup);
            if (itemCount > 0)
            {
                args.moveSpeedMultAdd += elusiveAntlersFreeMovespeedBase + elusiveAntlersFreeMovespeedStack * (itemCount - 1);
            }
        }
        #endregion
    }
}
