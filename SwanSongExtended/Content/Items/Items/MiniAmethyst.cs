using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SwanSongExtended.Items
{
    class MiniAmethyst : ItemBase<MiniAmethyst>
    {
        public override bool isEnabled => false;
        public override string ConfigName => "Items : Amethyst";
        public static float equipmentCooldownFractionToGiveAsRecharge = 0.10f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDef;
        public override string ItemName => "Amethyst Fragment";

        public override string ItemLangTokenName => "AMETHYST";

        public override string ItemPickupDesc => "Activating your Equipment reduces your ability cooldowns.";

        public override string ItemFullDescription => $"Activating your Equipment resets your <style=cIsUtility>Utility skill's cooldown</style>, " +
            $"and reduces <style=cIsUtility>all other cooldowns</style> by <style=cIsUtility>{Tools.ConvertDecimal(equipmentCooldownFractionToGiveAsRecharge)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(equipmentCooldownFractionToGiveAsRecharge)} per stack)</style> of your Equipment's base cooldown.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.EquipmentRelated, ItemTag.AIBlacklist };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/NullModel.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Core/texNullIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            EquipmentSlot.onServerEquipmentActivated += AmethystOnEquipUse;
        }

        private void AmethystOnEquipUse(EquipmentSlot activator, EquipmentIndex equipment)
        {
            CharacterBody body = activator.characterBody;
            Inventory inv = activator.inventory;
            if(body && inv)
            {
                int amethystCount = GetCount(inv);
                if(amethystCount > 0)
                {
                    float baseEquipCd = EquipmentCatalog.GetEquipmentDef(equipment).cooldown;
                    float recharge = baseEquipCd * equipmentCooldownFractionToGiveAsRecharge * amethystCount;

                    SkillLocator skillLocator = body.skillLocator;
                    if(skillLocator != null)
                    {
                        if (skillLocator.primary)
                            skillLocator.primary.RunRecharge(recharge);
                        if (skillLocator.secondary)
                            skillLocator.secondary.RunRecharge(recharge);
                        if (skillLocator.utility)
                            skillLocator.utility.RunRecharge(Mathf.Max(skillLocator.utility.cooldownRemaining, recharge));
                        if (skillLocator.special)
                            skillLocator.special.RunRecharge(recharge);
                    }
                }
            }
        }
    }
}
