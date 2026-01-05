using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using R2API;
using R2API.Utils;
using RoR2.Projectile;
using UnityEngine;
using RoR2;
using System.Linq;
using System.Security.Permissions;
using System.Security;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2.ExpansionManagement;
using ModularEclipse;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

using EntityStates.Captain.Weapon;
using EntityStates.ArtifactShell;
using EntityStates.LemurianMonster;
using EntityStates.VagrantMonster;
using EntityStates.LunarWisp;
using EntityStates.BeetleGuardMonster;
using EntityStates;
using EntityStates.ClayBoss;
using EntityStates.Mage.Weapon;
using EntityStates.Loader;
using EntityStates.MiniMushroom;
using EntityStates.ChildMonster;
using RoR2.Orbs;
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
        public static ArtifactDef MissileArtifact = null;
        public const float missileSpread = 45;
        public const float projectileSpread = 20;

        private bool IsMoreMissilesActive(CharacterBody sender)
        {
            return RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact);
        }

        private void CreateArtifact()
        {
            MissileArtifact = ScriptableObject.CreateInstance<ArtifactDef>();

            MissileArtifact.cachedName = "BorboWarfare";
            MissileArtifact.nameToken = "ARTIFACT_MISSILE_NAME";
            MissileArtifact.descriptionToken = "ARTIFACT_MISSILE_DESC";
            MissileArtifact.smallIconSelectedSprite = assetBundle.LoadAsset<Sprite>("Assets/warfare.png");
            MissileArtifact.smallIconDeselectedSprite = assetBundle.LoadAsset<Sprite>("Assets/warfaredeactivated.png");
            MissileArtifact.unlockableDef = null;
            MissileArtifact.requiredExpansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

            LanguageAPI.Add(MissileArtifact.nameToken, "Artifact of Warfare");
            LanguageAPI.Add(MissileArtifact.descriptionToken, "Triple most projectile attacks.");
            ContentAddition.AddArtifactDef(MissileArtifact);

            //compatibility with Modular Eclipse
            if (ModularEclipseLoaded)
            {
                ModularEclipseCompat(MissileArtifact);
            }

            MoreProjectilesModule.MoreProjectilesProvider += IsMoreMissilesActive;
        }
    }
}
