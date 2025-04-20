using BepInEx.Configuration;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SwanSongExtended.Artifacts
{
    // The directly below is entirely from TILER2 API (by ThinkInvis) specifically the Item module. Utilized to keep instance checking functionality as I migrate off TILER2.
    // TILER2 API can be found at the following places:
    // https://github.com/ThinkInvis/RoR2-TILER2
    // https://thunderstore.io/package/ThinkInvis/TILER2/

    public abstract class ArtifactBase<T> : ArtifactBase where T : ArtifactBase<T>
    {
        public static T instance { get; private set; }

        public ArtifactBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class ArtifactBase : SharedBase
    {
        public override string ConfigName => "Artifacts : " + ArtifactName;
        public override AssetBundle assetBundle => null;
        public abstract string ArtifactName { get; }
        public abstract string ArtifactDescription { get; }
        public abstract string ArtifactLangTokenName { get; }
        public abstract Sprite ArtifactSelectedIcon { get; }
        public abstract Sprite ArtifactDeselectedIcon { get; }

        public ArtifactDef ArtifactDef;
        public abstract void OnArtifactEnabledServer();
        public abstract void OnArtifactDisabledServer();

        public override void Init()
        {
            base.Init();
            CreateArtifact();
        }

        public override void Lang()
        {
            LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_NAME", "Artifact of " + ArtifactName);
            LanguageAPI.Add("ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION", ArtifactDescription);
        }

        protected void CreateArtifact()
        {
            ArtifactDef = ScriptableObject.CreateInstance<ArtifactDef>();
            {
                ArtifactDef.cachedName = "2r4r" + ArtifactName;
                ArtifactDef.nameToken = "ARTIFACT_" + ArtifactLangTokenName + "_NAME";
                ArtifactDef.descriptionToken = "ARTIFACT_" + ArtifactLangTokenName + "_DESCRIPTION";
                ArtifactDef.smallIconDeselectedSprite = ArtifactDeselectedIcon;
                ArtifactDef.smallIconSelectedSprite = ArtifactSelectedIcon;
                //ArtifactDef.unlockableDef = UnlockableCatalog.GetUnlockableDef("SuicideHermitCrabs");
            }
            Content.AddArtifactDef(ArtifactDef);

            RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
            RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
        }

        private void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDef)
                return;
            if (NetworkServer.active)
                OnArtifactEnabledServer();
        }

        private void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDef)
                return;
            OnArtifactDisabledServer();
        }

        public bool IsArtifactEnabled()
        {
            return RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef);
        }
    }
}
