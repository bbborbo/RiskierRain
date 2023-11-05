using EntityStates.Engi.EngiWeapon;
using RiskierRain.Skills;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRain.States
{
    class PlaceFlamer : PlaceTurret
    {
        public override void OnEnter()
        {
            //base.wristDisplayPrefab = Utils.Paths.GameObject.EngiTurretWristDisplay.Load<GameObject>();
            //base.blueprintPrefab = Utils.Paths.GameObject.EngiTurretBlueprints.Load<GameObject>();
            base.turretMasterPrefab = PlaceFlamerTurret.FlamerTurretMaster;
            base.OnEnter();
        }
    }
}
