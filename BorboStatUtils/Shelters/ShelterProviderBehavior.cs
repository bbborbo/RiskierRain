using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils.Shelters
{
    public class ShelterProviderBehavior : MonoBehaviour
    {
        private static List<ShelterProviderBehavior> instancesList = new List<ShelterProviderBehavior>();
        public static ReadOnlyCollection<ShelterProviderBehavior> readOnlyInstancesList = new ReadOnlyCollection<ShelterProviderBehavior>(ShelterProviderBehavior.instancesList);


        public HoldoutZoneController holdoutZoneController;
        public bool isSuperShelter = false;
        public bool isHazardZone = false;
        public float _fallbackRadius;
        public float fallbackRadius
        {
            get
            {
                if (!holdoutZoneController)
                    return _fallbackRadius;
                if (holdoutZoneController.charge <= 0 || holdoutZoneController.charge >= 1 || !holdoutZoneController.isActiveAndEnabled)
                    return 0;
                return holdoutZoneController.currentRadius;
            }
            set
            {
                _fallbackRadius = value;
            }
        }
        public IZone zoneBehavior;

        public bool IsInBounds(Vector3 position, float radius = 0)
        {
            Vector3 adjustedPosition = position;
            if(radius > 0)
            {
                Vector3 dir = position - base.transform.position;
                adjustedPosition = position - (dir.normalized * radius);
            }

            if (zoneBehavior != null)
            {
                return zoneBehavior.IsInBounds(adjustedPosition);
            }

            if (fallbackRadius < 1)
                return false;
            Vector3 vector = adjustedPosition - base.transform.position;
            return vector.sqrMagnitude <= this.fallbackRadius * this.fallbackRadius;
        }

        void OnEnable()
        {
            if(!ShelterUtilsModule.UseGlobalShelters && !ShelterUtilsModule.UseCustomShelters)
            {
                Debug.LogError("Shelter Provider cannot initialize: Shelter Module not enabled. (Set UseGlobalShelters or UseCustomShelters to true!)");
                Destroy(this);
            }
            instancesList.Add(this);
        }
        void OnDisable()
        {
            if(instancesList.Contains(this))
                instancesList.Remove(this);
        }
    }
}
