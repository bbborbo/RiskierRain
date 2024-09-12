using MonoMod.Cil;
using RoR2;
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
        }


        private void DisableICBM()
        {
            icbmItemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/MoreMissile/MoreMissile.asset").WaitForCompletion();
            if (icbmItemDef != null)
            {
                icbmItemDef.deprecatedTier = ItemTier.NoTier;
                icbmItemDef.tier = ItemTier.NoTier;
                //icbmItemDef.deprecatedTier = ItemTier.NoTier;
            }
        }
    }
}
