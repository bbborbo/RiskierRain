using RiskierRain.CoreModules;
using RiskierRain.Equipment;
using RiskierRain.Items;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.CoreModules
{
    internal class ContentPacks : IContentPackProvider
    {
        internal ContentPack contentPack = new ContentPack();

        public string identifier => RiskierRainContent.modName;

        public void Initialize()
        {
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(this);
        }

        public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            this.contentPack.identifier = this.identifier;

            contentPack.artifactDefs.Add(Assets.artifactDefs.ToArray());
            contentPack.buffDefs.Add(Assets.buffDefs.ToArray());
            contentPack.effectDefs.Add(Assets.effectDefs.ToArray());
            contentPack.equipmentDefs.Add(Assets.equipDefs.ToArray());
            contentPack.itemDefs.Add(Assets.itemDefs.ToArray());
            contentPack.eliteDefs.Add(Assets.eliteDefs.ToArray());
            contentPack.projectilePrefabs.Add(Assets.projectilePrefabs.ToArray());
            contentPack.bodyPrefabs.Add(Assets.bodyPrefabs.ToArray());
            contentPack.masterPrefabs.Add(Assets.masterPrefabs.ToArray());
            contentPack.entityStateTypes.Add(Assets.entityStates.ToArray());

            //contentPack.eliteDefs.Add(Assets.eliteDefs.ToArray());
            //contentPack.unlockableDefs.Add(Unlockables.unlockableDefs.ToArray());

            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(this.contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
