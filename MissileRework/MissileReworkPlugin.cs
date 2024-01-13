using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using ModularEclipse;

namespace MissileRework
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(ModularEclipsePlugin.guid, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DamageAPI))]
    public partial class MissileReworkPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "HouseOfFruits";
        public const string modName = "MissileRework";
        public const string version = "1.0.0";
        #endregion

        ArtifactDef MissileArtifact = ScriptableObject.CreateInstance<ArtifactDef>();
        ItemDef icbmItemDef;

        public void Awake()
        {
            MissileArtifact.cachedName = "BorboWarfare";
            MissileArtifact.nameToken = "ARTIFACT_MISSILE_NAME";
            MissileArtifact.descriptionToken = "ARTIFACT_MISSILE_NAME";
            MissileArtifact.smallIconDeselectedSprite = LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
            MissileArtifact.smallIconSelectedSprite = LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
            MissileArtifact.unlockableDef = null;

            icbmItemDef = Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/MoreMissile/MoreMissile.asset").WaitForCompletion();
            if (icbmItemDef != null)
            {
                icbmItemDef.tier = ItemTier.NoTier;
                //icbmItemDef.deprecatedTier = ItemTier.NoTier;
            }
            On.RoR2.Inventory.GetItemCount_ItemIndex += OverrideItemCount;

            LanguageAPI.Add(MissileArtifact.nameToken, "Artifact of Warfare");
            LanguageAPI.Add(MissileArtifact.descriptionToken, "Triple ALL missile effects.");
            ContentAddition.AddArtifactDef(MissileArtifact);
            ModularEclipsePlugin.SetArtifactDefaultWhitelist(MissileArtifact, true);
        }

        private int OverrideItemCount(On.RoR2.Inventory.orig_GetItemCount_ItemIndex orig, Inventory self, ItemIndex itemIndex)
        {
            if (itemIndex == DLC1Content.Items.MoreMissile.itemIndex)
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact.artifactIndex))
                    return 1;
                return 0;
            }
            return orig(self, itemIndex);
        }
    }
}