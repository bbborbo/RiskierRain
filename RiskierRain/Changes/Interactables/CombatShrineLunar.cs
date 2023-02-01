using BepInEx.Configuration;
using RiskierRain.Interactables;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.Changes.Interactables
{
    class CombatShrineLunar : InteractableBase
    {
        public override string interactableName => "Lunar Gallery";

        public override string interactableContext => "eat my transgendence nerd";

        public override string interactableLangToken => "LUNAR_GALLERY";

        public override GameObject interactableModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/RandomlyLunar/PickupDomino.prefab").WaitForCompletion();

        public override bool modelIsCloned => true;

        public override float voidSeedWeight => 0;

        public override int normalWeight => 50; //make this 1

        public override int spawnCost => 3;

        public override CostTypeDef costTypeDef => CostTypeCatalog.GetCostTypeDef(CostTypeIndex.None);

        public override int costTypeIndex => 0;

        public override int costAmount => 0;

        public override int interactableMinimumStageCompletions => 0;

        public override bool automaticallyScaleCostWithDifficulty => false;

        public override bool setUnavailableOnTeleporterActivated => false;

        public override bool isShrine => true;

        public override bool orientToFloor => true;

        public override bool skipSpawnWhenSacrificeArtifactEnabled => false;

        public override float weightScalarWhenSacrificeArtifactEnabled => 1;

        public override int maxSpawnsPerStage => 5;

        public override string modelName => throw new NotImplementedException();

        public override string prefabName => throw new NotImplementedException();

        public override void Init(ConfigFile config)
        {
           return;
        }
    }
}
