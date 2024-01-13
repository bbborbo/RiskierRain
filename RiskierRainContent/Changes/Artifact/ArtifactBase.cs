using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRainContent.Artifacts
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

    public abstract class ArtifactBase
    {
        public abstract string ArtifactName { get; }
        public abstract string ArtifactDescription { get; }
        public abstract string ArtifactLangTokenName { get; }
        public abstract Sprite ArtifactSelectedIcon { get; }
        public abstract Sprite ArtifactDeselectedIcon { get; }

        public ArtifactDef ArtifactDef;

        public abstract void Init(ConfigFile config);
        public abstract void OnEnabled();
        public abstract void OnDisabled();

        protected void CreateLang()
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
            Assets.artifactDefs.Add(ArtifactDef);
            RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
            RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
        }

        private void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDef)
                return;
            if (NetworkServer.active)
                OnEnabled();
        }

        private void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
        {
            if (artifactDef != ArtifactDef)
                return;
            OnDisabled();
        }

        public bool IsEnabled()
        {
            return RunArtifactManager.instance.IsArtifactEnabled(ArtifactDef);
        }
    }
}
