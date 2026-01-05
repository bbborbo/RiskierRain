using BepInEx;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRain.CoreModules;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static int Tier2EliteMinimumStageDefault = 6;
        public static int Tier2EliteMinimumStageDrizzle = 11;
        public static int Tier2EliteMinimumStageRainstorm = 6;
        public static int Tier2EliteMinimumStageMonsoon = 4;
        public static int Tier2EliteMinimumStageEclipse = 4;

        static string Tier2EliteName = "Rare";
    }
}
