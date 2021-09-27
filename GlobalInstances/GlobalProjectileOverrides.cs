using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public class GlobalProjectileOverrides : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public float[] ExtraAI = new float[100];
        public override void SetDefaults(Projectile projectile)
        {
            for (int i = 0; i < ExtraAI.Length; i++)
            {
                ExtraAI[i] = 0f;
            }
        }

        public override bool PreAI(Projectile projectile)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                if (OverridingListManager.InfernumProjectilePreAIOverrideList.ContainsKey(projectile.type))
                    return (bool)OverridingListManager.InfernumProjectilePreAIOverrideList[projectile.type].DynamicInvoke(projectile);
            }
            return base.PreAI(projectile);
        }
    }
}