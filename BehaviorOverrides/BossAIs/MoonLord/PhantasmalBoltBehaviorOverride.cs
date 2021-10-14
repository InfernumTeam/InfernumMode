using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class PhantasmalBoltBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.PhantasmalBolt;
        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            projectile.Infernum().ExtraAI[5] += 1f;
            if (projectile.localAI[0] == 0f)
            {
                if (Main.rand.Next(2) == 0)
                    Main.PlaySound(SoundID.Item124, projectile.position);
                else
                    Main.PlaySound(SoundID.Item125, projectile.position);
                projectile.localAI[0] = 1f;
            }
            projectile.alpha -= 40;
            if (projectile.alpha < 0)
            {
                projectile.alpha = 0;
            }
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.velocity = Vector2.Clamp(projectile.velocity, new Vector2(-10f), new Vector2(10f));

            // Has 3 extra updates
            projectile.timeLeft = (int)MathHelper.Min(250 * projectile.extraUpdates, projectile.timeLeft);

            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                projectile.active = false;
                return false;
            }
            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];
            projectile.tileCollide = projectile.Hitbox.Intersects(core.Infernum().arenaRectangle);

            Dust lunarVortexDust = Dust.NewDustDirect(projectile.Center, 0, 0, 229, 0f, 0f, 100, default, 1f);
            lunarVortexDust.noLight = true;
            lunarVortexDust.noGravity = true;
            lunarVortexDust.velocity = projectile.velocity;
            lunarVortexDust.position -= Vector2.One * 4f;
            lunarVortexDust.scale = 0.8f;

            projectile.frameCounter++;
            if (projectile.frameCounter >= 9)
            {
                projectile.frameCounter = 0;
                projectile.frame++;
                if (projectile.frame >= 5)
                    projectile.frame = 0;
            }
            return false;
        }
    }
}
