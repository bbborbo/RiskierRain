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

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MissileRework
{
    public partial class MissileReworkPlugin
    {
        internal void ReworkIcbm()
        {
            DisableICBM();

            LanguageAPI.Add("ITEM_MOREMISSILE_PICKUP", "Knock \'em dead, faggot.");
            LanguageAPI.Add("ITEM_MOREMISSILE_DESC", "Knock \'em dead, faggot.");
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
