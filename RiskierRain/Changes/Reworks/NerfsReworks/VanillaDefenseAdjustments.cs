using BepInEx;
using RiskierRain.CoreModules;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        #region defense
        public static int rapFreeArmor = 2;
        public static int knurlFreeArmor = 15;
        public static int bucklerFreeArmor = 10;

        void AdjustVanillaDefense()
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
            LanguageAPI.Add("ITEM_REPULSIONARMORPLATE_PICKUP",
                "Receive damage reduction from all attacks.");
            LanguageAPI.Add("ITEM_REPULSIONARMORPLATE_DESC",
                $"Reduce all <style=cIsDamage>incoming damage</style> by " +
                $"<style=cIsDamage>5<style=cStack> (+5 per stack)</style></style>. Cannot be reduced below <style=cIsDamage>1</style>. " +
                $"Gain another <style=cIsHealing>{rapFreeArmor} armor<style=cStack>(+{rapFreeArmor} per stack)</style>.");
        }
        private void FreeBonusArmor(CharacterBody sender, StatHookEventArgs args)
        {
            float freeArmor = 0;

            Inventory inv = sender.inventory;
            if (inv != null)
            {
                freeArmor += inv.GetItemCount(RoR2Content.Items.ArmorPlate) * rapFreeArmor;
                freeArmor += inv.GetItemCount(RoR2Content.Items.SprintArmor) * bucklerFreeArmor;
                freeArmor += inv.GetItemCount(RoR2Content.Items.Knurl) * knurlFreeArmor;
            }

            args.armorAdd += freeArmor;
        }

        private void TeddyChanges()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += TeddyChanges;
            IL.RoR2.HealthComponent.TakeDamageProcess += VoidBearChanges;
            LanguageAPI.Add("ITEM_BEAR_DESC",
                $"<style=cIsHealing>{15 / teddyNewMaxValue}%</style> " +
                $"<style=cStack>(+{15 / teddyNewMaxValue}% per stack)</style> " +
                $"chance to <style=cIsHealing>block</style> incoming damage, " +
                $"up to a maximum of <style=cIsHealing>{Tools.ConvertDecimal(teddyNewMaxValue)}%</style>. " +
                $"<style=cIsUtility>Unaffected by luck</style>.");
            LanguageAPI.Add("ITEM_BEARVOID_DESC",
                $"<style=cIsHealing>Blocks</style> incoming damage once. " +
                $"Recharges after <style=cIsUtility>{voidBearNewMaxCooldown} seconds</style> <style=cStack>(-10% per stack)</style>, " +
                $"to a minimum of <style=cIsUtility>{voidBearNewMinCooldown} seconds</style>. " +
                $"<style=cIsVoid>Corrupts all Tougher Times</style>.");
        }
        public static float voidBearNewMaxCooldown = 15f;
        public static float voidBearNewMinCooldown = 5f;
        private void VoidBearChanges(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = 14;
            c.GotoNext(MoveType.AfterLabel,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "BearVoid"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out countLoc)
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.AddTimedBuff))
                );
            c.EmitDelegate<Func<float, float>>((inDuration) =>
            {
                float baseDuration = 15;
                float outDuration = 5;
                outDuration += inDuration * ((baseDuration - outDuration) / baseDuration);
                return outDuration;
            });
        }

        public static float teddyNewMaxValue = 0.5f; //1.0
        private void TeddyChanges(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.AfterLabel,
                x => x.MatchLdfld("RoR2.HealthComponent/ItemCounts", "bear")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.ConvertAmplificationPercentageIntoReductionPercentage))
                );
            c.Emit(OpCodes.Ldc_R4, teddyNewMaxValue);
            c.Emit(OpCodes.Mul);
        }
        #endregion

        #region mobility
        public static float hoofSpeedBonusBase = 0.1f; //0.14
        public static float hoofSpeedBonusStack = 0.1f; //0.14
        private void GoatHoofNerf()
        {
            IL.RoR2.CharacterBody.RecalculateStats += HoofNerf;
            LanguageAPI.Add("ITEM_HOOF_DESC",
                $"Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>{Tools.ConvertDecimal(hoofSpeedBonusBase)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(hoofSpeedBonusStack)} per stack)</style>.");
        }
        private void HoofNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = 6;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Hoof"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out countLoc)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(countLoc),
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(out _)
                );
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

        public static float dynamicJumpAscentHoldGravity = 0.8f; //1f
        public static float dynamicJumpAscentReleaseGravity = 1.3f; //1f
        public static float dynamicJumpDescentGravity = 1f; //1f
        private void DynamicJump(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<UnityEngine.Physics>("get_gravity"),
                x => x.MatchLdfld<Vector3>("y")
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, CharacterMotor, float>>((gravityIn, motor) =>
            {
                float gravityOut = gravityIn;

                if (!motor.disableAirControlUntilCollision)
                {
                    if(motor.velocity.y >= 0)
                    {
                        if (motor.body.inputBank.jump.down)
                        {
                            gravityOut *= dynamicJumpAscentHoldGravity;
                        }
                        else
                        {
                            gravityOut *= dynamicJumpAscentReleaseGravity;
                        }
                    }
                    else
                    {
                        gravityOut *= dynamicJumpDescentGravity;
                    }
                }

                return gravityOut;
            });
        }

        public static float doubleJumpVerticalBonus = 1.0f; //1.5f
        public static float doubleJumpHorizontalBonus = 1.1f; //1.3f; //1.5f
        private void FeatherNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityStates.EntityState>("get_characterBody"),
                x => x.MatchLdfld<CharacterBody>("baseJumpCount")
                );

            int horizontalBoostLoc = 3;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStloc(out horizontalBoostLoc)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, doubleJumpHorizontalBonus);
            c.Index++;

            int verticalBoostLoc = 4;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStloc(out verticalBoostLoc)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, doubleJumpVerticalBonus);
        }


        public static float drinkSpeedBonusBase = 0.2f; //0.25
        public static float drinkSpeedBonusStack = 0.15f; //0.25
        private void EnergyDrinkNerf()
        {
            if (!RiskierRainPlugin.isHBULoaded)
            {
                LanguageAPI.Add("ITEM_SPRINTBONUS_DESC",
                    $"<style=cIsUtility>Sprint speed</style> is improved by <style=cIsUtility>{Tools.ConvertDecimal(drinkSpeedBonusBase)}</style> " +
                    $"<style=cStack>(+{Tools.ConvertDecimal(drinkSpeedBonusStack)} per stack)</style>.");
                IL.RoR2.CharacterBody.RecalculateStats += DrinkNerf;
            }
        }
        private void DrinkNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = -1;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "SprintBonus"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out countLoc)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdloc(countLoc),
                x => x.MatchConvR4()
                );
            c.EmitDelegate<Func<float, float, float>>((speedBonus, itemCount) =>
            {
                float newSpeedBonus = 0;
                if (itemCount > 0)
                {
                    newSpeedBonus = drinkSpeedBonusBase + (drinkSpeedBonusStack * (itemCount - 1));
                }
                return newSpeedBonus;
            });
            c.Remove();
        }
        #endregion

        #region healing
        public static float scytheBaseHeal = 0f; //4
        public static float scytheStackHeal = 3f; //4

        public static float monsterToothFlatHeal = 8;
        public static float monsterToothPercentHeal = 0.00f;

        public static float medkitFlatHeal = 25;
        public static float medkitPercentHeal = 0.00f;


        private void ScytheNerf()
        {
            IL.RoR2.GlobalEventManager.OnCrit += ScytheNerf;
            LanguageAPI.Add("ITEM_HEALONCRIT_DESC",
                $"Gain <style=cIsDamage>5% critical chance</style>. <style=cIsDamage>Critical strikes</style> <style=cIsHealing>heal</style> for " +
                $"<style=cIsHealing>{scytheBaseHeal + scytheStackHeal}</style> <style=cStack>(+{scytheStackHeal} per stack)</style> <style=cIsHealing>health</style>.");
        }

        private void MedkitNerf()
        {
            LoadBuffDef(nameof(RoR2Content.Buffs.MedkitHeal)).isDebuff = true;
            IL.RoR2.CharacterBody.RemoveBuff_BuffIndex += MedkitHealChange;
            LanguageAPI.Add("ITEM_MEDKIT_DESC",
                $"2 seconds after getting hurt, <style=cIsHealing>heal</style> for " +
                $" <style=cIsHealing>{medkitFlatHeal} health</style> <style=cStack>(+{medkitFlatHeal} per stack)</style>.");
        }

        private void MonsterToothNerf()
        {
            IL.RoR2.GlobalEventManager.OnCharacterDeath += MonsterToothHealChange;
            LanguageAPI.Add("ITEM_TOOTH_DESC",
            $"Killing an enemy spawns a <style=cIsHealing>healing orb</style> that heals for " +
            $"<style=cIsHealing>{monsterToothFlatHeal} health</style> <style=cStack>(+{monsterToothFlatHeal} per stack)</style>.");
        }

        private void MonsterToothHealChange(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = -1;
            c.GotoNext(MoveType.AfterLabel,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Tooth"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out countLoc)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchStfld<RoR2.HealthPickup>("flatHealing")
                );
            c.Emit(OpCodes.Ldloc, countLoc);
            c.EmitDelegate<Func<float, int, float>>((currentHealAmt, itemCount) =>
            {
                float newFlatHealAmt = monsterToothFlatHeal * (itemCount - 1);

                return newFlatHealAmt;
            });


            c.GotoNext(MoveType.Before,
                x => x.MatchStfld<RoR2.HealthPickup>("fractionalHealing")
                );
            c.EmitDelegate<Func<float, float>>((currentHealAmt) =>
            {
                float newPercentHealAmt = monsterToothPercentHeal;

                return newPercentHealAmt;
            });
        }

        private void MedkitHealChange(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = -1;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Medkit"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out countLoc)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchStloc(out _)
                );
            c.Emit(OpCodes.Ldloc, countLoc);
            c.EmitDelegate<Func<float, int, float>>((currentHealAmt, itemCount) =>
            {
                float newFlatHealAmt = medkitFlatHeal * (itemCount - 1);

                return newFlatHealAmt;
            });


            c.GotoNext(MoveType.Before,
                x => x.MatchStloc(out _)
                );
            c.EmitDelegate<Func<float, float>>((currentHealAmt) =>
            {
                float newPercentHealAmt = medkitPercentHeal;

                return newPercentHealAmt;
            });
        }

        private void ScytheNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int countLoc = -1;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "HealOnCrit"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out countLoc)
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<RoR2.HealthComponent>(nameof(RoR2.HealthComponent.Heal))
                );

            c.Index -= 2;
            c.Emit(OpCodes.Ldloc, countLoc);
            c.EmitDelegate<Func<float, int, float>>((currentHealAmt, itemCount) =>
            {
                float newHealAmt = scytheBaseHeal + scytheStackHeal * itemCount;

                return newHealAmt;
            });
        }
        #endregion

        #region barrier
        private float barrierDecayRate = 0.33f;
        void BuffBarrier()
        {
            On.RoR2.CharacterBody.FixedUpdate += this.BarrierBuff;
        }
        private void BarrierBuff(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            if(self.barrierDecayRate > 0)
                self.barrierDecayRate = Mathf.Max(1f, self.healthComponent.barrier * this.barrierDecayRate);

            orig(self);
        }
        #endregion
    }
}
