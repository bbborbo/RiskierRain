using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class StickyBomb : ItemBase<StickyBomb>
    {
        public override string ItemName => "Sticky Bomb Retier (Rework)";

        public override string ItemLangTokenName => throw new NotImplementedException();

        public override string ItemPickupDesc => throw new NotImplementedException();

        public override string ItemFullDescription => throw new NotImplementedException();

        public override string ItemLore => throw new NotImplementedException();

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => throw new NotImplementedException();

        public override BalanceCategory Category => BalanceCategory.StateOfDamage;

        public override GameObject ItemModel => throw new NotImplementedException();

        public override Sprite ItemIcon => throw new NotImplementedException();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            //ItemDef stickybomb = Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/StickyBomb/StickyBomb.asset").WaitForCompletion();
            //stickybomb._itemTierDef = ItemTierCatalog.GetItemTierDef(ItemTier.Tier2);
            RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.StickyBomb), ItemTier.Tier2);

            IL.RoR2.GlobalEventManager.OnHitEnemy += StickyBombRework;
            LanguageAPI.Add("ITEM_STICKYBOMB_DESC",
                $"<style=cIsDamage>5%</style> <style=cStack>(+5% per stack)</style> chance " +
                $"on hit to attach a <style=cIsDamage>bomb</style> to an enemy, detonating for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(stickyDamageCoeffBase)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(stickyDamageCoeffStack)} per stack)</style> TOTAL damage.");

            GameObject stickyPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/StickyBomb");
            ProjectileImpactExplosion pie = stickyPrefab.GetComponent<ProjectileImpactExplosion>();
        }
        public static float stickyDamageCoeffBase = 3.2f; //3.2 is 8 stacks to beat atg, 4.0 is 6 stacks
        public static float stickyDamageCoeffStack = 0.4f;

        private void StickyBombRework(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int stickyLoc = 14;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "StickyBomb"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCount)),
                x => x.MatchStloc(out stickyLoc)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnHitProcDamage))
                );
            c.Emit(OpCodes.Ldloc, stickyLoc);
            c.EmitDelegate<Func<float, int, float>>((damageCoefficient, itemCount) =>
            {
                float damageOut = stickyDamageCoeffBase + (stickyDamageCoeffStack * (itemCount - 1));
                return damageOut;
            });
        }
    }
}
