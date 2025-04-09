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
        public static List<EquipmentIndex> gestureBreakBlacklist = new List<EquipmentIndex>();
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
            On.RoR2.EquipmentCatalog.Init += CreateGestureBlacklist;
            On.RoR2.EquipmentSlot.ExecuteIfReady += AddGestureUndercast;
            On.RoR2.EquipmentSlot.OnEquipmentExecuted += AddGestureBreak;
            IL.RoR2.EquipmentSlot.MyFixedUpdate += RemoveGestureAutocast;
            IL.RoR2.EquipmentSlot.MyFixedUpdate += AddPreonAccumulatorBreak;
            IL.RoR2.Inventory.CalculateEquipmentCooldownScale += RemoveGestureCdr;
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += AddGestureCdi;
            On.RoR2.Inventory.GetEquipmentSlotMaxCharges += AddGestureStock;
            IL.RoR2.Inventory.UpdateEquipment += FixMaxStock;
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

        private void AddGestureBreak(On.RoR2.EquipmentSlot.orig_OnEquipmentExecuted orig, EquipmentSlot self)
        {
            orig(self);
            if (!gestureBreakBlacklist.Contains(self.equipmentIndex))
                TryGestureEquipmentBreak(self);
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

        private void CreateGestureBlacklist(On.RoR2.EquipmentCatalog.orig_Init orig)
        {
            orig();
            gestureBreakBlacklist.Add(EquipmentIndex.None);
            gestureBreakBlacklist.Add(RoR2Content.Equipment.BFG.equipmentIndex);
        }

        private bool AddGestureUndercast(On.RoR2.EquipmentSlot.orig_ExecuteIfReady orig, EquipmentSlot self)
        {
            if (NetworkServer.active && self.inventory?.GetItemCount(RoR2Content.Items.AutoCastEquipment) > 0 && self.inputBank.activateEquipment.justPressed && self.stock <= 0)
            {
                self.stock += 1;
                self.characterBody.AddBuff(Modules.CommonAssets.gestureQueueEquipBreak);
            }
            return orig(self);
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
