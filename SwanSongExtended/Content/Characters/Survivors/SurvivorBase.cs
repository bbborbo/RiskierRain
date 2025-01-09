using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using RoR2.Skills;
using System;
using SwanSongExtended.Characters;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Survivors
{
    public abstract class SurvivorBase<T> : SurvivorBase where T : SurvivorBase<T>
    {
        public static T instance { get; private set; }

        public SurvivorBase()
        {
            if (instance != null) throw new InvalidOperationException(
                $"Singleton class \"{typeof(T).Name}\" inheriting {SwanSongPlugin.modName} {typeof(SurvivorBase).Name} was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class SurvivorBase : CharacterBase
    {
        public abstract string SurvivorSubtitle { get; }
        public abstract string SurvivorDescription { get; }
        public abstract string SurvivorOutroWin { get; }
        public abstract string SurvivorOutroFailure { get; }

        public abstract string masterName { get; }

        public abstract string displayPrefabName { get; }

        public abstract string survivorTokenPrefix { get; }

        public abstract UnlockableDef characterUnlockableDef { get; }

        public abstract GameObject displayPrefab { get; protected set; }

        public override void InitializeCharacter()
        {
            base.InitializeCharacter();

            InitializeDisplayPrefab();

            InitializeSurvivor();
        }

        protected virtual void InitializeDisplayPrefab()
        {
            displayPrefab = Prefabs.CreateDisplayPrefab(assetBundle, displayPrefabName, bodyPrefab);
        }

        protected virtual void InitializeSurvivor()
        {
            RegisterNewSurvivor(bodyPrefab, displayPrefab, bodyInfo.bodyColor, survivorTokenPrefix, characterUnlockableDef, bodyInfo.sortPosition);
        }

        public static void RegisterNewSurvivor(GameObject bodyPrefab, GameObject displayPrefab, Color charColor, string tokenPrefix, UnlockableDef unlockableDef, float sortPosition)
        {
            Content.CreateSurvivor(bodyPrefab, displayPrefab, charColor, tokenPrefix, unlockableDef, sortPosition);
        }

        #region CharacterSelectSurvivorPreviewDisplayController
        protected virtual void AddCssPreviewSkill(int indexFromEditor, SkillFamily skillFamily, SkillDef skillDef)
        {
            CharacterSelectSurvivorPreviewDisplayController CSSPreviewDisplayConroller = displayPrefab.GetComponent<CharacterSelectSurvivorPreviewDisplayController>();
            if (!CSSPreviewDisplayConroller)
            {
                Log.Error("trying to add skillChangeResponse to null CharacterSelectSurvivorPreviewDisplayController.\nMake sure you created one on your Display prefab in editor");
                return;
            }

            CSSPreviewDisplayConroller.skillChangeResponses[indexFromEditor].triggerSkillFamily = skillFamily;
            CSSPreviewDisplayConroller.skillChangeResponses[indexFromEditor].triggerSkill = skillDef;
        }

        protected virtual void AddCssPreviewSkin(int indexFromEditor, SkinDef skinDef)
        {
            CharacterSelectSurvivorPreviewDisplayController CSSPreviewDisplayConroller = displayPrefab.GetComponent<CharacterSelectSurvivorPreviewDisplayController>();
            if (!CSSPreviewDisplayConroller)
            {
                Log.Error("trying to add skinChangeResponse to null CharacterSelectSurvivorPreviewDisplayController.\nMake sure you created one on your Display prefab in editor");
                return;
            }

            CSSPreviewDisplayConroller.skinChangeResponses[indexFromEditor].triggerSkin = skinDef;
        }

        protected virtual void FinalizeCSSPreviewDisplayController()
        {
            if (!displayPrefab)
                return;

            CharacterSelectSurvivorPreviewDisplayController CSSPreviewDisplayConroller = displayPrefab.GetComponent<CharacterSelectSurvivorPreviewDisplayController>();
            if (!CSSPreviewDisplayConroller)
                return;

            //set body prefab
            CSSPreviewDisplayConroller.bodyPrefab = bodyPrefab;

            //clear list of null entries
            List<CharacterSelectSurvivorPreviewDisplayController.SkillChangeResponse> newlist = new List<CharacterSelectSurvivorPreviewDisplayController.SkillChangeResponse>();

            for (int i = 0; i < CSSPreviewDisplayConroller.skillChangeResponses.Length; i++)
            {
                if (CSSPreviewDisplayConroller.skillChangeResponses[i].triggerSkillFamily != null)
                {
                    newlist.Add(CSSPreviewDisplayConroller.skillChangeResponses[i]);
                }
            }

            CSSPreviewDisplayConroller.skillChangeResponses = newlist.ToArray();
        }
        #endregion
        public override void Lang()
        {
            Modules.Language.Add(bodyInfo.bodyNameToken, CharacterName);
            Modules.Language.Add(bodyInfo.subtitleNameToken, SurvivorSubtitle);
            Modules.Language.Add(survivorTokenPrefix + "LORE", CharacterLore);
            Modules.Language.Add(survivorTokenPrefix + "DESCRIPTION", SurvivorDescription);
            Modules.Language.Add(survivorTokenPrefix + "OUTRO_FLAVOR", SurvivorOutroWin);
            Modules.Language.Add(survivorTokenPrefix + "OUTRO_FAILURE", SurvivorOutroFailure);
        }
    }
}
