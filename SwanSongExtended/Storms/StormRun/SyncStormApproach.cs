using R2API.Networking.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SwanSongExtended.Storms
{
    public class SyncStormApproach : INetMessage
    {
        GameObject stormControllerObject;
        float stormDelayTime;
        float stormWarningTime;
        public SyncStormApproach()
        {
        }
        public SyncStormApproach(GameObject stormControllerObject, float stormDelayTime, float stormWarningTime)
        {
            this.stormControllerObject = stormControllerObject;
            this.stormDelayTime = stormDelayTime;
            this.stormWarningTime = stormWarningTime;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.stormControllerObject);
            writer.Write((double)this.stormDelayTime);
            writer.Write((double)this.stormWarningTime);
        }
        public void Deserialize(NetworkReader reader)
        {
            this.stormControllerObject = reader.ReadGameObject();
            this.stormDelayTime = (float)reader.ReadDouble();
            this.stormWarningTime = (float)reader.ReadDouble();
        }

        public void OnReceived()
        {
            if (!NetworkClient.active)
                return;
            StormController stormController;
            if (stormControllerObject != null)
                stormController = stormControllerObject.GetComponent<StormController>();
            else
                stormController = StormRunBehavior.instance.stormControllerInstance;

            if (stormController != null)
                stormController.BeginStormApproach(stormDelayTime, stormWarningTime);
        }
    }
}
