using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public static float gestureEquipBreakChance = 40;
        public static int gestureStockBase = 4;
        public static int gestureStockStack = 2;
        public static float gestureCdiBase = 1;
        public static float gestureCdiStack = 0.5f;
        public void GestureChanges()
        {
            LanguageAPI.Add("ITEM_AUTOCASTEQUIPMENT_PICKUP", $"Greatly increase equipment stock... {HealthColor("BUT greatly increase equipment cooldown.")} " +
                $"Equipments can be activated during their cooldown, {HealthColor("with a chance to break.")}");
            LanguageAPI.Add("ITEM_AUTOCASTEQUIPMENT_DESC", 
                $"Hold {UtilityColor($"{gestureStockBase} additional equipment charges")} {StackText($"+{gestureStockStack}")}... " +
                $"{HealthColor($"BUT increase equipment cooldown by +{Tools.ConvertDecimal(gestureCdiBase)}")} " +
                $"{StackText("+" + Tools.ConvertDecimal(gestureCdiStack))}. " +
                $"Using your equipment without charges {UtilityColor($"under-casts")} it, " +
                $"allowing it to be used {HealthColor($"with a {gestureEquipBreakChance}% chance to break")}. " +
                $"{UtilityColor("Unaffected by luck.")}");
            IL.RoR2.EquipmentSlot.MyFixedUpdate += RemoveGestureAutocast;
            IL.RoR2.Inventory.CalculateEquipmentCooldownScale += RemoveGestureCdr;

            IL.RoR2.EquipmentSlot.ExecuteIfReady += AllowGestureUndercast;
            On.RoR2.EquipmentSlot.OnEquipmentExecuted += AddGestureUndercast;
            //IL.RoR2.EquipmentSlot.MyFixedUpdate += AddPreonAccumulatorBreak;
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += AddGestureCdi;
            On.RoR2.Inventory.GetEquipmentSlotMaxCharges += AddGestureStock;
            On.RoR2.EquipmentSlot.MyFixedUpdate += AddGestureBreak;
            IL.RoR2.Inventory.UpdateEquipment += FixMaxStock;
        }

        private void AllowGestureUndercast(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<EquipmentSlot>("get_stock"));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, EquipmentSlot, int>>((stock, slot) => 
            {
                if (stock > 0)
                    return stock;
                if (slot.inventory.GetItemCount(RoR2Content.Items.AutoCastEquipment) > 0)
                    return 1;
                return 0;
            });
        }

        private void FixMaxStock(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "EquipmentMagazine"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchStloc(out _));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, Inventory, int>>((stock, inv) =>
            {
                stock += GetGestureStockFromInventory(inv);
                return stock;
            });
        }

        private int AddGestureStock(On.RoR2.Inventory.orig_GetEquipmentSlotMaxCharges orig, Inventory self, byte slot)
        {
            int stock = orig(self, slot);
            stock += GetGestureStockFromInventory(self);
            return stock;
        }
        public static int GetGestureStockFromInventory(Inventory inv)
        {
            int gestureCount = inv.GetItemCount(RoR2Content.Items.AutoCastEquipment);
            if (gestureCount > 0)
            {
                return 4 + 2 * (gestureCount - 1);
            }
            return 0;
        }

        private float AddGestureCdi(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            float scale = orig(self);
            int gestureCount = self.GetItemCount(RoR2Content.Items.AutoCastEquipment);
            if(gestureCount > 0)
            {
                scale *= 1 + gestureCdiBase + gestureCdiStack * (gestureCount - 1);
            }
            return scale;
        }

        private void RemoveGestureCdr(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "AutoCastEquipment"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }

        private void AddGestureUndercast(On.RoR2.EquipmentSlot.orig_OnEquipmentExecuted orig, EquipmentSlot self)
        {
            bool undercast = false;
            if (NetworkServer.active && self.stock <= 0 && self.inventory.GetItemCount(RoR2Content.Items.AutoCastEquipment) > 0)
            {
                self.inventory.RestockEquipmentCharges(self.activeEquipmentSlot, 1);
                undercast = true;
            }

            orig(self);

            if (undercast)
            {
                self.characterBody.AddBuff(Modules.CommonAssets.gestureQueueEquipBreak);
                if (self.subcooldownTimer <= 0)
                    TryGestureEquipmentBreak(self);
            }
        }

        private void AddGestureBreak(On.RoR2.EquipmentSlot.orig_MyFixedUpdate orig, EquipmentSlot self, float deltaTime)
        {
            orig(self, deltaTime);
            if (NetworkServer.active && self.subcooldownTimer <= 0)
            {
                TryGestureEquipmentBreak(self);
            }
        }

        private void AddPreonAccumulatorBreak(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(0),
                x => x.MatchStfld<EquipmentSlot>(nameof(EquipmentSlot.bfgChargeTimer))
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<EquipmentSlot>>((self) =>
            {
                TryGestureEquipmentBreak(self);
            });
        }

        public static void TryGestureEquipmentBreak(EquipmentSlot self)
        {
            if (self.characterBody.HasBuff(Modules.CommonAssets.gestureQueueEquipBreak))
            {
                if (Util.CheckRoll(gestureEquipBreakChance))
                {
                    self.inventory.SetEquipmentIndex(EquipmentIndex.None);
                }
                self.characterBody.RemoveBuff(Modules.CommonAssets.gestureQueueEquipBreak);
            }
        }

        private void RemoveGestureAutocast(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "AutoCastEquipment"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }
    }
}
