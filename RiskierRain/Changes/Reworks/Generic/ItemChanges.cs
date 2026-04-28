using BepInEx;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.Items;
using R2API;
using RiskierRain.Changes.Components;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static RoR2.HoldoutZoneController;

namespace RiskierRain
{
    partial class RiskierRainPlugin
    {
        internal static void AIBlacklistSingleItem(string name)
        {
            ItemDef itemDef = LoadItemDef(name);
            List<ItemTag> itemTags = new List<ItemTag>(itemDef.tags);
            itemTags.Add(ItemTag.AIBlacklist);

            itemDef.tags = itemTags.ToArray();
        }
        #region blacklist
        void HealingItemBlacklist()
        {
            AIBlacklistSingleItem(nameof(RoR2Content.Items.BarrierOnKill));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.BarrierOnOverHeal));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.NovaOnHeal));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.Mushroom));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.Medkit));
            AIBlacklistSingleItem(nameof(RoR2Content.Items.Tooth));
        }
        #endregion

        #region stuns
        public static float capacitorDamageCoefficient = 10f;
        public static float capacitorBlastRadius = 13f;
        public static float capacitorCooldown = 20f; //20
        void ChangeCapacitor()
        {
            LoadEquipDef(nameof(RoR2Content.Equipment.Lightning)).cooldown = capacitorCooldown;
            IL.RoR2.EquipmentSlot.FireLightning += CapacitorNerf;
            IL.RoR2.Orbs.LightningStrikeOrb.OnArrival += CapacitorBuff;
            LanguageAPI.Add("EQUIPMENT_LIGHTNING_DESC", $"Call down a lightning strike on a targeted monster, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(capacitorDamageCoefficient)} damage</style> " +
                $"and <style=cIsDamage>stunning</style> nearby monsters in a large radius.");
        }

        private void CapacitorNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul(),
                x => x.MatchStfld<RoR2.Orbs.GenericDamageOrb>("damageValue")
                );
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, capacitorDamageCoefficient);
        }

        private void CapacitorBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcI4(out _),
                x => x.MatchStfld<BlastAttack>("falloffModel")
                );
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, (int)BlastAttack.FalloffModel.SweetSpot);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStfld<BlastAttack>("radius")
                );
            //c.Index++;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, capacitorBlastRadius);
        }
        #endregion

        #region meteor
        BlastAttack.FalloffModel falloffModel = BlastAttack.FalloffModel.None;
        void FixMeteorFalloff()
        {
            IL.RoR2.MeteorStormController.DetonateMeteor += MeteorFix;
        }
        private void MeteorFix(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchStfld<BlastAttack>(nameof(BlastAttack.falloffModel))
                );

            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, (int)falloffModel);
        }
        #endregion

        #region minion on kill
        void MakeMinionsInheritOnKillEffects()
        {
            On.RoR2.Inventory.GetItemCountEffective_ItemIndex += GetItemCountEffectiveInheritOnKills;
        }

        private int GetItemCountEffectiveInheritOnKills(On.RoR2.Inventory.orig_GetItemCountEffective_ItemIndex orig, Inventory self, ItemIndex itemIndex)
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








    }

}