using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RoR2.HealthComponent;

namespace RiskierRainContent.Items
{
    class RejuvRack : ItemBase<RejuvRack>
    {
        public static float reserveFillPerSecond = 0.02f;
        public static float healthFractionToRestorePerSecond = 0.10f; //the % of max health which leaves the reserve, per second
        public static float baseOverhealEfficiency = 0.5f; //the amount that healing which enters reserve gets multiplied by
        public static float stackOverhealEfficiency = 0.5f; 
        public static float baseReserveCapacityFraction = 0.25f; //the % of max health which can be held in reserve
        public static float stackReserveCapacityFraction = 0f;
        public override string ItemName => "Rejuvenation Rack";

        public override string ItemLangTokenName => "REWORKRACK";

        public override string ItemPickupDesc => "Stores overheal for later.";

        public override string ItemFullDescription => $"Healing past full will <style=cIsHealing>store the excess</style> in a <style=cIsHealing>healing reserve</style> at " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(baseOverhealEfficiency)}</style> <style=cStack>(+{Tools.ConvertDecimal(stackOverhealEfficiency)} per stack)</style> " +
            $"efficiency, up to " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(baseReserveCapacityFraction)}</style> <style=cStack>(+{Tools.ConvertDecimal(stackReserveCapacityFraction)} per stack)</style> " +
            $"of your maximum health. When injured, heal for " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(healthFractionToRestorePerSecond)}</style> of your <style=cIsHealing>health per second</style>, " +
            $"drawing from the <style=cIsHealing>healing reserve</style> until it is empty.";

        public override string ItemLore => "\"Nature has a way of nurturing the physical. The mind, by perseverance and dedication. The soul, however... is healed by fantasy, and fantasy alone.\"\r\n-Unknown Venetian monk\r\n";

        public override ItemTier Tier => ItemTier.Tier3;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.AIBlacklist };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/IncreaseHealing/PickupAntler.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/IncreaseHealing/texAntlerIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            RiskierRainContent.RetierItem(nameof(RoR2Content.Items.RepeatHeal));
            RiskierRainContent.RetierItem(nameof(RoR2Content.Items.IncreaseHealing));

            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, RoR2Content.Items.IncreaseHealing);
            On.RoR2.Inventory.GetItemCount_ItemIndex += OverrideItemCount;
            On.RoR2.HealthComponent.RepeatHealComponent.FixedUpdate += RepeatHealWhileHurt;
            IL.RoR2.HealthComponent.Heal += RejuvOverheal;
        }

        private void RejuvOverheal(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //void the original corpsebloom logic, we dont need it
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<HealthComponent>(nameof(HealthComponent.repeatHealComponent)),
                x => x.MatchCallOrCallvirt(out _)
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
            /*c.EmitDelegate<Func<HealthComponent, bool>>((healthComponent) =>
            {
                return false;// healthComponent.health >= healthComponent.fullHealth;
            });*/

            int overhealLoc = 2;
            //jump to aegis logic, we wont delete aegis here but we need to find it
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld("RoR2.HealthComponent/ItemCounts", "barrierOnOverHeal")
                );
            c.GotoPrev(MoveType.Before,
                x => x.MatchLdloc(out overhealLoc),
                x => x.MatchLdcR4(0.0f),
                x => x.MatchBleUn(out _)
                );
            c.Emit(OpCodes.Ldloc, overhealLoc);
            c.Emit(OpCodes.Ldarg, 0); //self, healthComponent
            c.Emit(OpCodes.Ldarg, 3); //nonRegen
            c.EmitDelegate<Action<float, HealthComponent, bool>>((overhealAmt, self, nonRegen) =>
            {
                int rackCount = self.itemCounts.repeatHeal;
                if (overhealAmt > 0 && nonRegen && rackCount > 0 && self.repeatHealComponent)
                {
                    float reserveHealing = overhealAmt * (baseOverhealEfficiency + stackOverhealEfficiency * (rackCount - 1));
                    float reserveMax = self.fullHealth * (baseReserveCapacityFraction + stackReserveCapacityFraction * (rackCount - 1));
                    self.repeatHealComponent.AddReserve(reserveHealing, reserveMax);
                }
            });

            /*c.GotoNext(MoveType.After,
                x => x.MatchAdd(),
                x => x.MatchConvR4()
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((multiplier, healthComponent) =>
            {
                int rackCount = healthComponent.itemCounts.repeatHeal;
                return baseReserveMultiplier + stackReserveMultiplier * (rackCount - 1);
                //return multiplier;
            });

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.fullHealth))
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((reserve, healthComponent) =>
            {
                int rackCount = healthComponent.itemCounts.repeatHeal;
                return baseReserveFraction + stackReserveFraction * (rackCount - 1);
                //return multiplier;
            });*/
        }

        private void RepeatHealWhileHurt(On.RoR2.HealthComponent.RepeatHealComponent.orig_FixedUpdate orig, MonoBehaviour mono)
        {
            RepeatHealComponent self = (mono as RepeatHealComponent);
            HealthComponent hc = self.healthComponent;
            self.AddReserve(hc.fullHealth * reserveFillPerSecond * Time.fixedDeltaTime, 
                hc.fullHealth * (baseReserveCapacityFraction + stackReserveCapacityFraction * (hc.itemCounts.repeatHeal - 1)));
            if (!hc || hc.health < hc.fullHealth)
            {
                self.healthFractionToRestorePerSecond = healthFractionToRestorePerSecond;
                orig(mono);
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private int OverrideItemCount(On.RoR2.Inventory.orig_GetItemCount_ItemIndex orig, Inventory self, ItemIndex itemIndex)
        {
            if(itemIndex == RoR2Content.Items.RepeatHeal.itemIndex)
            {
                return GetCount(self);
            }
            return orig(self, itemIndex);
        }
    }
}
