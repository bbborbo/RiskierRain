using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskierRainContent.Changes.Components
{
    public class CustomChestBehavior : ChestBehavior
    {
        public virtual void OnInteractionBegin(Interactor activator)
        {
            this.ItemDrop();
        }
    }
}
