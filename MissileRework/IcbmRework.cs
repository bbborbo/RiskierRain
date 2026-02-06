using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine.AddressableAssets;
using RainrotSharedUtils.MoreProjectiles;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MissileRework
{
    public partial class MissileReworkPlugin
    {
        private bool IsVanillaIcbmHeld(CharacterBody sender)
        {
            return sender.inventory && sender.inventory.GetItemCountEffective(DLC1Content.Items.MoreMissile) > 0;
        }

        internal void ReworkIcbm()
        {
            MoreProjectilesModule.MoreProjectilesProvider += IsVanillaIcbmHeld;
            On.RoR2.MissileUtils.GetMoreMissileDamageMultiplier += ChangeIcbmMissileDamageMultiplier;
            //DisableICBM();
            //
            //LanguageAPI.Add("ITEM_MOREMISSILE_PICKUP", "Knock \'em dead, faggot.");
            //LanguageAPI.Add("ITEM_MOREMISSILE_DESC", "Knock \'em dead, faggot.");
            LanguageAPI.Add("ITEM_MOREMISSILE_PICKUP", "Triple most projectile attacks.");
            LanguageAPI.Add("ITEM_MOREMISSILE_DESC", "Most projectile attacks fire an additional <style=cIsDamage>2 projectiles</style>. " +
                "Increase missile damage by <style=cIsDamage>0%</style> <style=cStack>+50% per stack)</style>.");
        }

        private float ChangeIcbmMissileDamageMultiplier(On.RoR2.MissileUtils.orig_GetMoreMissileDamageMultiplier orig, int moreMissileCount)
        {
            return orig(moreMissileCount);
        }

        private void DisableICBM()
        {
            AssetReferenceT<ItemDef> ref1 = new AssetReferenceT<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_MoreMissile.MoreMissile_asset);
            AssetAsyncReferenceManager<ItemDef>.LoadAsset(ref1).Completed += (ctx) =>
            {
                ItemDef itemDef = ctx.Result;
                itemDef.tier = ItemTier.NoTier;
                itemDef.deprecatedTier = ItemTier.NoTier;
            };
        }
    }
}
