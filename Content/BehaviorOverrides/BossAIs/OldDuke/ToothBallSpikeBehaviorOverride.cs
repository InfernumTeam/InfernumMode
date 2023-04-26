using CalamityMod.CalPlayer;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.OldDuke
{
    public class ToothBallSpikeBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ModContent.ProjectileType<TrilobiteSpike>();

        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectilePreDraw | ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            if (CalamityPlayer.areThereAnyDamnBosses)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (projectile.identity % 2 == 1)
                        Utilities.NewProjectileBetter(projectile.Center, projectile.velocity, ModContent.ProjectileType<OldDukeTooth>(), OldDukeBehaviorOverride.HomingToothDamage, 0f);
                    projectile.Kill();
                }
                return false;
            }
            return true;
        }
    }
}