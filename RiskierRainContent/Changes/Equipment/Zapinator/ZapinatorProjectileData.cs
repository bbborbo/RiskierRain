using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Equipment.Zapinator
{
    public struct ZapinatorProjectileData
    {
        public GameObject prefab;
        public ZapinatorProjectileType type;

        public ZapinatorModifiers[] possibleModifiers;
        public int maxModifiers;
        public int bonusModifiers;
    }
}
