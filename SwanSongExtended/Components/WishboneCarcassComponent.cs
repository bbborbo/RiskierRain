using RoR2;
using RoR2.UI;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Components
{
    public class WishboneObjectiveTracker : ObjectivePanelController.ObjectiveTracker
    {
        public WishboneObjectiveTracker()
        {
            this.baseToken = StormsCore.wishboneObjectiveToken;
        }
    }
    class WishboneCarcassComponent : MonoBehaviour
    {
        static List<WishboneCarcassComponent> instancesList = new List<WishboneCarcassComponent>();
        public static ReadOnlyCollection<WishboneCarcassComponent> readonlyInstancesList = new ReadOnlyCollection<WishboneCarcassComponent>(WishboneCarcassComponent.instancesList);
        public static bool objectiveOn = false;
        public static void ClearAllCarcasses()
        {
            for(int i = readonlyInstancesList.Count - 1; i >= 0; i--)
            {
                WishboneCarcassComponent carcass = readonlyInstancesList[i];
                if(carcass != null)
                {
                    Destroy(carcass);
                    Destroy(carcass.gameObject);
                }
            }
        }

        void OnEnable()
        {
            if (instancesList.Count == 0)
                SetWishboneObjective(true);
            instancesList.Add(this);
        }
        void OnDisable()
        {
            if (instancesList.Contains(this))
                instancesList.Remove(this);
            if (instancesList.Count == 0)
                SetWishboneObjective(false);
        }

        void SetWishboneObjective(bool enable)
        {
            if (enable)
            {
                if (!objectiveOn)
                {
                    ObjectivePanelController.collectObjectiveSources += OnCollectObjectiveSources;
                    objectiveOn = true;
                }
            }
            else if(objectiveOn)
            {
                ObjectivePanelController.collectObjectiveSources -= OnCollectObjectiveSources;
                objectiveOn = false;
            }
        }
        private static void OnCollectObjectiveSources(CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectiveSourcesList)
        {
            objectiveSourcesList.Add(new ObjectivePanelController.ObjectiveSourceDescriptor
            {
                master = master,
                objectiveType = typeof(WishboneObjectiveTracker),
                source = StormRunBehavior.instance
            });
        }
    }
}
