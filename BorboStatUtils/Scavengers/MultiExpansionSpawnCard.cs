using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils
{
    public class MultiExpansionSpawnCard : CharacterSpawnCard
	{
		public void RegenerateSelection()
        {
			weightedMasterInfos.Clear();
			foreach(MasterInfo info in masterInfos)
            {
				if(info.requiredExpansion == null || Run.instance.IsExpansionEnabled(info.requiredExpansion))
					weightedMasterInfos.AddChoice(info, info.weight);
            }
        }
		public override void Spawn(Vector3 position, Quaternion rotation, DirectorSpawnRequest directorSpawnRequest, ref SpawnCard.SpawnResult result)
		{
			this.prefab = weightedMasterInfos.Evaluate(Run.instance.spawnRng.nextNormalizedFloat).masterPrefab;
			base.Spawn(position, rotation, directorSpawnRequest, ref result);
		}

		public WeightedSelection<MasterInfo> weightedMasterInfos = new WeightedSelection<MasterInfo>();
		public MasterInfo[] masterInfos;

		public struct MasterInfo
        {
			public GameObject masterPrefab;
			public ExpansionDef requiredExpansion;
			public float weight;
        }
	}
}
