using RoR2;
using SurvivorTweaks.SurvivorTweaks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SurvivorTweaks.Components
{
    public class MasterDesperadoTokenTracker : MonoBehaviour
    {
        public int desperadoTokenCount { get; private set; }
        public CharacterMaster master;

        public static int GetMaxPersistentTokenCountFromLevel(float level)
        {
            return BanditTweaks.desperadoTokensPerLevel * Mathf.FloorToInt(level);
        }

        public void SetTokenCount(int value)
        {
            desperadoTokenCount = value;
        }
        public void OnServerStageComplete(Stage stage)
        {
            int maxTokens = GetMaxPersistentTokenCountFromLevel(TeamManager.instance.GetTeamLevel(master.teamIndex));
            if (desperadoTokenCount > maxTokens)
                desperadoTokenCount = maxTokens;
        }
        private void OnBodyStart(CharacterBody body)
        {
            body.SetBuffCount(RoR2Content.Buffs.BanditSkull.buffIndex, desperadoTokenCount);
        }
        void OnEnable()
        {
            Stage.onServerStageComplete += OnServerStageComplete;
            if (master == null)
                master = GetComponent<CharacterMaster>();
            if (master != null)
            {
                master.onBodyStart += OnBodyStart;
            }
        }

        void OnDisable()
        {
            Stage.onServerStageComplete -= OnServerStageComplete;
            if (master == null)
                master = GetComponent<CharacterMaster>();
            if (master != null)
                master.onBodyStart -= OnBodyStart;
        }
    }
}
