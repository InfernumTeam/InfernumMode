using System;

namespace InfernumMode.OverridingSystem
{
    [Flags]
    public enum ProjectileOverrideContext
    {
        ProjectileAI = 1,
        ProjectilePreDraw = 2,
    }
}
