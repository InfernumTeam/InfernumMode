using System;

namespace InfernumMode.Core.OverridingSystem
{
    [Flags]
    public enum ProjectileOverrideContext
    {
        ProjectileAI = 1,
        ProjectilePreDraw = 2,
    }
}
