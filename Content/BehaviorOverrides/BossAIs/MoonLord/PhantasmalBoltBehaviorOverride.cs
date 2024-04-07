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

        public override bool PreAI(Projectile projectile)
        {
            ref float timer = ref projectile.Infernum().ExtraAI[0];
            ref float lifetime = ref projectile.Infernum().ExtraAI[1];


            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
            {
                projectile.active = false;
                return false;
            }
            NPC core = Main.npc[NPC.FindFirstNPC(NPCID.MoonLordCore)];
            Rectangle collisionArea = core.Infernum().Arena;

            if (projectile.localAI[0] == 0f)
            {
                if (Main.rand.NextBool(2))
                    SoundEngine.PlaySound(SoundID.Item124, projectile.position);
                else
                    SoundEngine.PlaySound(SoundID.Item125, projectile.position);

                // If moonlord is doing the blender attack, mark the lifetime.
                bool deathrayAttack = (MoonLordCoreBehaviorOverride.MoonLordAttackState)core.Infernum().ExtraAI[0] == MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalDeathrays;
                if (deathrayAttack)
                    lifetime = collisionArea.Intersects(projectile.Hitbox) ? 270f : -1f;

                projectile.localAI[0] = 1f;
            }

            if (timer > lifetime && lifetime != 0f)
            {
                projectile.Kill();
                return false;
            }

            projectile.alpha = Utils.Clamp(projectile.alpha - 40, 0, 255);
            projectile.rotation = projectile.velocity.ToRotation() + PiOver2;
            projectile.velocity = Vector2.Clamp(projectile.velocity, new Vector2(-10f), new Vector2(10f));
            projectile.timeLeft = (int)MathF.Min(250 * projectile.MaxUpdates, projectile.timeLeft);

            // Determine frames.
            projectile.frameCounter++;
            if (projectile.frameCounter >= 9)
            {
                projectile.frameCounter = 0;
                projectile.frame++;
                if (projectile.frame >= 5)
                    projectile.frame = 0;
            }

            timer++;

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
