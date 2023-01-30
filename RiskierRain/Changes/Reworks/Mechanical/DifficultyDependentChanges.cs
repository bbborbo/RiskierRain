using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RiskierRain.Components;
using static R2API.RecalculateStatsAPI;
using EntityStates;
using BepInEx;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static float drizzleDifficultyBoost = 0;
        public static float rainstormDifficultyBoost = 3;
        public static float monsoonDifficultyBoost = 6;
        public static float eclipseDifficultyBoost = 6;

        public static float timeDifficultyScaling = 1.8f; //1f
        public static float stageDifficultyScaling = 1.0f; //1.15f

        public static float easyTeleParticleRadius = 1f;
        public static float normalTeleParticleRadius = 0.8f;
        public static float hardTeleParticleRadius = 0.4f;
        public static float eclipseTeleParticleRadius = 0f;
        public static float defaultTeleParticleRadius = 0.8f;

        #region all enemies
        //enemies
        CharacterBody VultureBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/VultureBody");
        CharacterBody BeetleBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/BeetleBody");
        CharacterBody BeetleGuardBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/BeetleGuardBody");
        CharacterBody BisonBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/BisonBody");
        CharacterBody BellBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/BellBody");
        CharacterBody ClayTemplarBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/ClayBruiserBody");
        CharacterBody ElderLemurianBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/LemurianBruiserBody");
        CharacterBody GreaterWispBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/GreaterWispBody");
        CharacterBody HermitCrabBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/HermitCrabBody");
        CharacterBody ImpBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/ImpBody");
        CharacterBody JellyfishBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/JellyfishBody");
        CharacterBody LemurianBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/LemurianBody");
        CharacterBody LesserWispBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/WispBody");
        CharacterBody LunarExploderBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/LunarExploderBody");
        CharacterBody LunarGolemBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/LunarGolemBody");
        CharacterBody LunarWispBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/LunarWispBody");
        CharacterBody MiniMushroomBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/MiniMushroomBody");
        CharacterBody ParentBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/ParentBody");
        CharacterBody SolusProbeBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/RoboBallMiniBody");
        CharacterBody StoneGolemBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/GolemBody");
        CharacterBody VoidReaverBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/NullifierBody");

        //bosses
        CharacterBody BeetleQueenBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/BeetleQueen2Body");
        CharacterBody ClayBossBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/ClayBossBody");
        CharacterBody GrandParentBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/GrandParentBody");
        CharacterBody GrovetenderBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/GravekeeperBody");
        CharacterBody ImpBossBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/ImpBossBody");
        CharacterBody MagmaWormBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/MagmaWormBody");
        CharacterBody ScavBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/ScavBody");
        CharacterBody SolusControlUnitBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/RoboBallBossBody");
        CharacterBody StoneTitanBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/TitanBody");
        CharacterBody VagrantBody = LegacyResourcesAPI.Load<CharacterBody>("prefabs/characterbodies/VagrantBody");
        #endregion

        void AmbientLevelDifficulty()
        {
            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += AmbientLevelChanges;
        }

        private void DifficultyDependentTeleParticles()
        {
            drizzleDesc += $"\n>Teleporter Visuals: <style=cIsHealing>+{Tools.ConvertDecimal(easyTeleParticleRadius / normalTeleParticleRadius - 1)}</style> ";
            rainstormDesc += $"\n>Teleporter Visuals: +{Tools.ConvertDecimal(normalTeleParticleRadius / normalTeleParticleRadius - 1)}</style> ";
            monsoonDesc += $"\n>Teleporter Visuals: <style=cIsHealth>{Tools.ConvertDecimal(1 - hardTeleParticleRadius / normalTeleParticleRadius)}</style> ";

            On.RoR2.TeleporterInteraction.BaseTeleporterState.OnEnter += TeleporterParticleScale;
        }

        private void MonsoonStatBoost()
        {
            monsoonDesc += $"\n>Most Enemies have <style=cIsHealth>unique scaling</style></style>";

            GiveMonstersMonsoonStatBuffers();
            GiveBossesMonsoonStatBuffers();
            GetStatCoefficients += this.MonsoonPlusStatBuffs;
        }

        private void TeleporterParticleScale(On.RoR2.TeleporterInteraction.BaseTeleporterState.orig_OnEnter orig, BaseState self)
        {
            orig(self);
            float particleScale = 1f;
            switch (Run.instance.selectedDifficulty)
            {
                default:
                    particleScale = eclipseTeleParticleRadius;
                    break;
                case RoR2.DifficultyIndex.Hard:
                    particleScale = hardTeleParticleRadius;
                    break;
                case RoR2.DifficultyIndex.Normal:
                    particleScale = normalTeleParticleRadius;
                    break;
                case RoR2.DifficultyIndex.Easy:
                    particleScale = easyTeleParticleRadius;
                    break;
                case RoR2.DifficultyIndex.Count:
                    break;
                case RoR2.DifficultyIndex.Invalid:
                    break;
            }


            TeleporterInteraction component = self.GetComponent<TeleporterInteraction>();
            bool flag5 = component && component.modelChildLocator;
            if (flag5)
            {
                Transform transform = component.transform.Find("TeleporterBaseMesh/BuiltInEffects/PassiveParticle, Sphere");
                if (transform)
                {
                    //Debug.Log(transform.localScale);
                    transform.localScale = Vector3.one * defaultTeleParticleRadius * particleScale;
                }
            }
        }

        #region monsoon stats
        #region monsoon stat buffers
        private void GiveMonstersMonsoonStatBuffers()
            {
                StatBuffer vulture = VultureBody?.gameObject.AddComponent<StatBuffer>();
                vulture.levelShield = 10f;
            
                StatBuffer beetle = BeetleBody?.gameObject.AddComponent<StatBuffer>();
                beetle.levelMoveSpeed = 0.1f;
                beetle.levelOffset = (int)-monsoonDifficultyBoost;

                StatBuffer beetleguard = BeetleGuardBody?.gameObject.AddComponent<StatBuffer>();
                beetleguard.levelAttackSpeed = 0.03f;
            
                StatBuffer bison = BisonBody?.gameObject.AddComponent<StatBuffer>();
                bison.levelMoveSpeed = 0.05f;
            
                StatBuffer brass = BellBody?.gameObject.AddComponent<StatBuffer>();
                brass.levelAttackSpeed = 0.03f;
            
                StatBuffer templar = ClayTemplarBody?.gameObject.AddComponent<StatBuffer>();
                templar.levelAttackSpeed = 0.03f;
            
                StatBuffer elderlemurian = ElderLemurianBody?.gameObject.AddComponent<StatBuffer>();
                elderlemurian.levelAttackSpeed = 0.05f;
            
                StatBuffer gwisp = GreaterWispBody?.gameObject.AddComponent<StatBuffer>();
                gwisp.levelAttackSpeed = 0.05f;
            
                StatBuffer crab = HermitCrabBody?.gameObject.AddComponent<StatBuffer>();
                crab.levelMoveSpeed = 0.12f;
            
                StatBuffer imp = ImpBody?.gameObject.AddComponent<StatBuffer>();
                imp.levelMoveSpeed = 0.05f;
            
                StatBuffer jellyfish = JellyfishBody?.gameObject.AddComponent<StatBuffer>();
                jellyfish.levelMoveSpeed = 0.08f;
            
                StatBuffer lemurian = LemurianBody?.gameObject.AddComponent<StatBuffer>();
                lemurian.levelAttackSpeed = 0.06f;
            
                StatBuffer lwisp = LesserWispBody?.gameObject.AddComponent<StatBuffer>();
                lwisp.levelArmor = 3f;

                StatBuffer chimeraexploder = LunarExploderBody?.gameObject.AddComponent<StatBuffer>();
                chimeraexploder.levelMoveSpeed = 0.1f;

                StatBuffer chimeragolem = LunarGolemBody?.gameObject.AddComponent<StatBuffer>();
                chimeragolem.levelShield = 300f;
            
                StatBuffer chimerawisp = LunarWispBody?.gameObject.AddComponent<StatBuffer>();
                chimerawisp.levelAttackSpeed = 0.03f;
            
                StatBuffer mushrum = MiniMushroomBody?.gameObject.AddComponent<StatBuffer>();
                mushrum.levelArmor = 1f;
            
                StatBuffer parent = ParentBody?.gameObject.AddComponent<StatBuffer>();
                parent.levelAttackSpeed = 0.03f;
            
                StatBuffer probe = SolusProbeBody?.gameObject.AddComponent<StatBuffer>();
                probe.levelArmor = 1f;
            
                StatBuffer golem = StoneGolemBody?.gameObject.AddComponent<StatBuffer>();
                golem.levelArmor = 2f;
            
                StatBuffer reaver = VoidReaverBody?.gameObject.AddComponent<StatBuffer>();
                reaver.levelShield = 500f;
                reaver.levelOffset = (int)-monsoonDifficultyBoost;
            }

            private void GiveBossesMonsoonStatBuffers()
            {
                StatBuffer queen = BeetleQueenBody?.gameObject.AddComponent<StatBuffer>();
                queen.levelArmor = 2f;

                StatBuffer dunestrider = ClayBossBody?.gameObject.AddComponent<StatBuffer>();
                dunestrider.levelArmor = 2f;

                StatBuffer grandpa = GrandParentBody?.gameObject.AddComponent<StatBuffer>();
                grandpa.levelAttackSpeed = 0.04f;
                grandpa.levelOffset = -3;

                StatBuffer grovetender = GrovetenderBody?.gameObject.AddComponent<StatBuffer>();
                grovetender.levelMoveSpeed = 0.06f;
                grovetender.levelOffset = -3;

                StatBuffer overlord = ImpBossBody?.gameObject.AddComponent<StatBuffer>();
                overlord.levelArmor = 2f;
                overlord.levelOffset = -3;

                StatBuffer worm = MagmaWormBody?.gameObject.AddComponent<StatBuffer>();
                worm.levelMoveSpeed = 0.06f;
                worm.levelOffset = -3;

                StatBuffer scav = ScavBody?.gameObject.AddComponent<StatBuffer>();
                scav.levelShield = 800f;
                scav.levelOffset = -6;

                StatBuffer solus = SolusControlUnitBody?.gameObject.AddComponent<StatBuffer>();
                solus.levelShield = 600f;
                solus.levelOffset = -3;

                StatBuffer titan = StoneTitanBody?.gameObject.AddComponent<StatBuffer>();
                titan.levelArmor = 2f;

                StatBuffer vagrant = VagrantBody?.gameObject.AddComponent<StatBuffer>();
                vagrant.levelMoveSpeed = 0.06f;
            }
            #endregion

            private void MonsoonPlusStatBuffs(CharacterBody sender, StatHookEventArgs args)
            {
                if(Run.instance.selectedDifficulty >= DifficultyIndex.Hard && sender.teamComponent.teamIndex != TeamIndex.Player)
                {
                    StatBuffer sb = sender.gameObject.GetComponent<StatBuffer>();
                    if(sb != null)
                    {
                        args.baseAttackSpeedAdd += Mathf.Max(sb.levelAttackSpeed * (sender.level + sb.levelOffset - monsoonDifficultyBoost), 0);
                        args.baseMoveSpeedAdd += Mathf.Max(sb.levelMoveSpeed * (sender.level + sb.levelOffset - monsoonDifficultyBoost), 0);
                        args.baseShieldAdd += Mathf.Max(sb.levelShield * (sender.level + sb.levelOffset - monsoonDifficultyBoost), 0);
                        args.armorAdd += Mathf.Max((int)(sb.levelArmor * (sender.level + sb.levelOffset - monsoonDifficultyBoost)), 0);
                    }
                }
            }
        #endregion

        #region ambient level
        internal float GetAmbientLevelBoost()
        {
            float difficultyBoost = 0f;

            switch (Run.instance.selectedDifficulty)
            {
                default:
                    difficultyBoost = eclipseDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Hard:
                    difficultyBoost = monsoonDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Normal:
                    difficultyBoost = rainstormDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Easy:
                    difficultyBoost = drizzleDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Count:
                    break;
                case RoR2.DifficultyIndex.Invalid:
                    break;
            }

            return difficultyBoost;
        }

        private void AmbientLevelChanges(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //num2 (difficulty coefficient)
            int timeLoc = 2;
            int timeMul = 2;
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out timeLoc),
                x => x.MatchLdcR4(out _),
                x => x.MatchMul(),
                x => x.MatchCallOrCallvirt<Mathf>("Floor")
                );
            c.Emit(OpCodes.Ldc_R4, timeDifficultyScaling);
            c.Emit(OpCodes.Mul);

            //num9 (difficulty coefficient)
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<RoR2.Run>("stageClearCount")
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, stageDifficultyScaling);

            //num10 (ambient level)
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(out timeLoc),
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            c.Emit(OpCodes.Ldc_R4, timeDifficultyScaling);
            c.Emit(OpCodes.Mul);

            //num10 (ambient level)
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<RoR2.Run>("stageClearCount")
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, stageDifficultyScaling);


            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld<RoR2.Run>("ambientLevelCap")
                );
            c.EmitDelegate<Func<float, float>>((levelIn) =>
            {
                float difficultyBoost = GetAmbientLevelBoost();

                Run.instance.compensatedDifficultyCoefficient += difficultyBoost * 0.05f; //stage 3 spawnrates at stage 0 monsoon, stage 2 spawnrates at stage 0 rainstorm
                //Run.instance.difficultyCoefficient += difficultyBoost / 2;
                float levelOut = levelIn + difficultyBoost;
                return levelOut;
            });
        }

        private void DifficultyCoefficientChanges(On.RoR2.Run.orig_RecalculateDifficultyCoefficentInternal orig, RoR2.Run self)
        {
            orig(self);
            float difficultyBoost = 0f;

            switch (self.selectedDifficulty)
            {
                default:
                    difficultyBoost = eclipseDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Hard:
                    difficultyBoost = monsoonDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Normal:
                    difficultyBoost = rainstormDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Easy:
                    difficultyBoost = drizzleDifficultyBoost;
                    break;
                case RoR2.DifficultyIndex.Count:
                    break;
                case RoR2.DifficultyIndex.Invalid:
                    break;
            }

            self.ambientLevel = Mathf.Min(self.ambientLevel + difficultyBoost, Run.ambientLevelCap);
            //self.compensatedDifficultyCoefficient += difficultyBoost;
            //self.difficultyCoefficient += difficultyBoost;
        }
        #endregion
    }
}