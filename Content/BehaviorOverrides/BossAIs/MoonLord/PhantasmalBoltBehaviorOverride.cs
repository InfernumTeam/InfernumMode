using InfernumMode.Core;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class PhantasmalBoltBehaviorOverride : ProjectileBehaviorOverride
    {
        public override int ProjectileOverrideType => ProjectileID.PhantasmalBolt;

        public override ProjectileOverrideContext ContentToOverride => ProjectileOverrideContext.ProjectileAI;

        public override bool PreAI(Projectile projectile)
        {
            projectile.Infernum().ExtraAI[0]++;
            if (projectile.localAI[0] == 0f)
            {
                if (Main.rand.NextBool(2))
                    SoundEngine.PlaySound(SoundID.Item124, projectile.position);
                else
                    SoundEngine.PlaySound(SoundID.Item125, projectile.position);
                projectile.localAI[0] = 1f;
            }

            projectile.alpha = Utils.Clamp(projectile.alpha - 40, 0, 255);
            projectile.rotation = projectile.velocity.ToRotation() + PiOver2;
            projectile.velocity = Vector2.Clamp(projectile.velocity, new Vector2(-10f), new Vector2(10f));
            projectile.timeLeft = (int)MathF.Min(250 * projectile.MaxUpdates, projectile.timeLeft);

            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                projectile.active = false;
                return false;
            }
            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];
            Rectangle collisionArea = core.Infernum().Arena;
            collisionArea.Inflate(-600, -600);

            // Determine whether the bolt should collide with tiles.
            projectile.tileCollide = projectile.Hitbox.Intersects(collisionArea);

            // Determine frames.
            projectile.frameCounter++;
            if (projectile.frameCounter >= 9)
            {
                projectile.frameCounter = 0;
                projectile.frame++;
                if (projectile.frame >= 5)
                    projectile.frame = 0;
            }

            if (InfernumConfig.Instance.ReducedGraphicsConfig)
                return false;

            Dust electricity = Dust.NewDustDirect(projectile.Center, 0, 0, DustID.Vortex, 0f, 0f, 100, default, 1f);
            electricity.noLight = true;
            electricity.noGravity = true;
            electricity.velocity = projectile.velocity;
            electricity.position -= Vector2.One * 4f;
            electricity.scale = 0.8f;
            return false;
        }
    }
}