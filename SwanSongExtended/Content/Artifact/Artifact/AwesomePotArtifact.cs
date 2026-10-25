using RoR2;
using RoR2.Artifacts;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Artifacts
{
    public class AwesomePotArtifact : ArtifactBase<AwesomePotArtifact>
    {
        public static bool BodyIsPot(CharacterBody body)
        {
            return body.bodyIndex == BodyCatalog.FindBodyIndexCaseInsensitive("ExplosivePotBody");
        }
        public override string ArtifactName => "Volatility";

        public override string ArtifactDescription => "Explosive barrels and pots are Elite.";

        public override string ArtifactLangTokenName => "AWESOMEPOT";

        public override Sprite ArtifactSelectedIcon => LoadArtifactIcon();

        public override Sprite ArtifactDeselectedIcon => LoadArtifactIcon(fallBackOnWrench:true);

        public override void Hooks()
        {

        }

        public override void OnArtifactDisabledServer()
        {
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        public override void OnArtifactEnabledServer()
        {
            CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        private void CharacterBody_onBodyStartGlobal(CharacterBody potBody)
        {
            if (Run.instance == false)
                return;
            if (BodyIsPot(potBody) == false)
                return;
            EliteDef[] eliteDefs = EliteOnlyArtifactManager.eliteDefs;
            float count = eliteDefs.Length;

            EliteDef eliteDef = eliteDefs[Mathf.FloorToInt(Run.instance.spawnRng.nextNormalizedFloat * count)];
            if(eliteDef != null && eliteDef.eliteEquipmentDef != null && eliteDef.eliteEquipmentDef.passiveBuffDef != null)
            {
                potBody.AddBuff(eliteDef.eliteEquipmentDef.passiveBuffDef);
            }
        }
    }
}
