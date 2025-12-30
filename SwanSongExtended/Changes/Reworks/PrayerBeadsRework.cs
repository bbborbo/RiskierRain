using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public static float beadsPermanentStatBonus = 0.07f;

        public void PrayerBeadsRework()
        {
            RetierItemAsync(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_ExtraStatsOnLevelUp.ExtraStatsOnLevelUp_asset, RoR2.ItemTier.Tier2, ChangeTags);
            void ChangeTags(ItemDef itemDef)
            {
                HG.ArrayUtils.ArrayAppend(ref itemDef.tags, ItemTag.PriorityScrap);
                HG.ArrayUtils.ArrayAppend(ref itemDef.tags, ItemTag.AIBlacklist);
            }

            On.RoR2.CharacterMaster.OnBodyStart += InitializeBeadBuff;
            On.RoR2.CharacterMaster.TrackBeadExperience += (orig, self, idk) => { };
            IL.RoR2.CharacterBody.RecalculateStats += ChangeBeadAppliedStats;
            On.RoR2.ExperienceManager.AwardExperience += BeadExperience;

            LanguageAPI.Add("ITEM_EXTRASTATSONLEVELUP_PICKUP", 
                "Prioritized when used with <style=cIsHealing>Uncommon</style> 3D Printers. Permanently increase ALL stats when removed.");
            LanguageAPI.Add("ITEM_EXTRASTATSONLEVELUP_DESC", 
                $"Prioritized when used with <style=cIsHealing>Uncommon</style> 3D Printers. " +
                $"On removal, permanently grants a {ConvertDecimal(beadsPermanentStatBonus)} increase to " +
                $"<style=cIsUtility>experience gain</style>, " +
                $"<style=cIsHealing>health</style>, <style=cIsHealing>shield</style>, " +
                $"<style=cIsHealing>regeneration</style>, and <style=cIsDamage>damage</style>.");
        }

        private void BeadExperience(On.RoR2.ExperienceManager.orig_AwardExperience orig, ExperienceManager self, Vector3 origin, CharacterBody body, ulong amount)
        {
            if(body.master != null && body.inventory != null)
            {
                float multiplier = 1 + body.inventory.beadAppliedHealth * beadsPermanentStatBonus;
                amount += (ulong)((float)amount * multiplier);
            }
            orig(self, origin, body, amount);
        }

        private void InitializeBeadBuff(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);
            Inventory inv = body.inventory;
            if (inv == null || inv.beadAppliedHealth <= 0)
                return;

            body.SetBuffCount(DLC2Content.Buffs.ExtraStatsOnLevelUpBuff.buffIndex, (int)inv.beadAppliedHealth);
        }

        private void ChangeBeadAppliedStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLoc = 69; //67 in decompiled C#
            ILLabel label = null;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Items", "ExtraStatsOnLevelUp"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountPermanent)),
                x => x.MatchStloc(out itemCountLoc)
                );
            if (!b1)
            {
                Log.DebugBreakpoint(nameof(ChangeBeadAppliedStats), 1);
                return;
            }

            bool b2 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(itemCountLoc),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<RoR2.CharacterBody>(nameof(RoR2.CharacterBody.extraStatsOnLevelUpCount_CachedLastApplied)),
                x => x.MatchBge(out label)
                );
            if (!b2)
            {
                Log.DebugBreakpoint(nameof(ChangeBeadAppliedStats), 2);
                return;
            }

            c.Emit(OpCodes.Ldloc, itemCountLoc);
            c.Emit(OpCodes.Ldarg_0);//characterbody self
            c.EmitDelegate<Action<int, CharacterBody>>((itemCount, self) => 
            {
                int beadsLost = self.extraStatsOnLevelUpCount_CachedLastApplied - itemCount;
                self.extraStatsOnLevelUpCount_CachedLastApplied = itemCount;

                self.inventory.beadAppliedHealth += beadsLost;
                self.inventory.beadAppliedShield += beadsLost;
                self.inventory.beadAppliedRegen += beadsLost;
                self.inventory.beadAppliedDamage += beadsLost;
                self.SetBuffCount(DLC2Content.Buffs.ExtraStatsOnLevelUpBuff.buffIndex, (int)self.inventory.beadAppliedHealth);
            }); 

            bool b3 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Buffs", "ExtraStatsOnLevelUpBuff"),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.GetBuffCount))
                );
            if (!b3)
            {
                Log.DebugBreakpoint(nameof(ChangeBeadAppliedStats), 3);
                return;
            }
            //fuck your buff count goddamn
            c.EmitDelegate<Func<int, int>>((buffCount) => { return 0; });

            c.GotoLabel(label);
            ChangeBeadStat(nameof(Inventory.beadAppliedHealth), ChangeBeadHealthBonus);
            ChangeBeadStat(nameof(Inventory.beadAppliedShield), ChangeBeadShieldBonus);
            ChangeBeadStat(nameof(Inventory.beadAppliedRegen), ChangeBeadRegenBonus);
            ChangeBeadStat(nameof(Inventory.beadAppliedDamage), ChangeBeadDamageBonus);

            void ChangeBeadStat(string beadStatName, Func<float, CharacterBody, float, float> callback)
            {
                bool b = c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<Inventory>(beadStatName),
                    x => x.MatchLdcR4(out _),
                    x => x.MatchBleUn(out _)) 
                    &&
                    c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<Inventory>(beadStatName)
                    );
                if (!b)
                {
                    Log.DebugBreakpoint($"{nameof(ChangeBeadStat)}:{beadStatName}", 3);
                    return;
                }
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, CharacterBody, float>>((beadBonus, self) =>
                {
                    float fixedLevel = self.level - 1;
                    return callback.Invoke(beadBonus, self, fixedLevel);
                });
            }
            float ChangeBeadHealthBonus(float beadBonusStacks, CharacterBody self, float level)
            {
                float baseHealth = self.baseMaxHealth + (self.levelMaxHealth * level);
                return baseHealth * beadsPermanentStatBonus * beadBonusStacks;
            }
            float ChangeBeadShieldBonus(float beadBonusStacks, CharacterBody self, float level)
            {
                float baseHealth = self.baseMaxHealth + (self.levelMaxHealth * level);
                return baseHealth * beadsPermanentStatBonus * beadBonusStacks;
            }
            float ChangeBeadRegenBonus(float beadBonusStacks, CharacterBody self, float level)
            {
                float baseHealth = Mathf.Abs(self.baseRegen + (self.levelRegen * level));
                return baseHealth * beadsPermanentStatBonus * beadBonusStacks;
            }
            float ChangeBeadDamageBonus(float beadBonusStacks, CharacterBody self, float level)
            {
                float baseHealth = self.baseDamage + (self.levelDamage * level);
                return baseHealth * beadsPermanentStatBonus * beadBonusStacks;
            }
        }
    }
}
