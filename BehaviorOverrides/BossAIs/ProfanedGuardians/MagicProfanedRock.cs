using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs.ProfanedGuardians;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class MagicProfanedRock : ModProjectile
    {
        public ref float GeneralTimer => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Profaned Rock");
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 32;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 600;
        }

        public override void AI()
        {
            // Disappear if the defender guardian is gone.
            int defenderGuardianIndex = NPC.FindFirstNPC(ModContent.NPCType<ProfanedGuardianBoss2>());
            if (defenderGuardianIndex == -1)
            {
                projectile.Kill();
                return;
            }

            NPC defenderGuardian = Main.npc[defenderGuardianIndex];
            Player target = Main.player[defenderGuardian.target];

            // Emit light.
            Lighting.AddLight(projectile.Center, Color.Yellow.ToVector3() * 0.45f);

            if (GeneralTimer < 120f)
            {
                // Orbit around the guardian.
                float hoverOffset = (float)Math.Sin(GeneralTimer / 300f + projectile.identity) * 40f + 120f;
                Vector2 hoverDestination = defenderGuardian.Center;
                hoverDestination += (GeneralTimer / 120f * MathHelper.Pi * 4f + projectile.identity).ToRotationVector2() * hoverOffset;
                Vector2 idealVelocity = projectile.SafeDirectionTo(hoverDestination) * MathHelper.Min(projectile.Distance(hoverDestination), 30f);
                projectile.velocity = (projectile.velocity * 19f + idealVelocity) / 20f;
                projectile.velocity = projectile.velocity.MoveTowards(idealVelocity, 3.5f);
                projectile.Center = projectile.Center.MoveTowards(hoverDestination, 7f);

                bool aimedAtTarget = projectile.velocity.AngleBetween(projectile.SafeDirectionTo(target.Center)) < 0.2f;

                // Fling the rock at the target if aimed at them.
                if ((aimedAtTarget && GeneralTimer > 65f) || GeneralTimer >= 115f)
                {
                    Main.PlaySound(SoundID.Item73, projectile.Center);

                    GeneralTimer = 120f;
                    projectile.velocity = projectile.SafeDirectionTo(target.Center) * 11f;
                    projectile.netUpdate = true;
                }
            }

            // Accelerate after being launched.
            else if (projectile.velocity.Length() < 30f)
                projectile.velocity *= 1.025f;

            projectile.rotation += projectile.velocity.X * 0.006f;
            projectile.alpha = Utils.Clamp(projectile.alpha - 30, 0, 255);
            GeneralTimer++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 baseDrawPosition = projectile.Center - Main.screenPosition;
            Color afterimageColor = projectile.GetAlpha(Color.Lerp(Color.Orange, Color.Yellow, 0.56f)) * 0.4f;
            afterimageColor.A = 24;

            for (int i = 0; i < 12; i++)
            {
                float drawOffset = MathHelper.Lerp(16f, 1f, projectile.Opacity);
                drawOffset = MathHelper.Lerp(drawOffset, 6f, Utils.InverseLerp(120f, 135f, GeneralTimer, true));
                Vector2 drawPosition = baseDrawPosition + (MathHelper.TwoPi * i / 12f + Main.GlobalTime * 0.47f).ToRotationVector2() * drawOffset;
                spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(afterimageColor), projectile.rotation, origin, projectile.scale, 0, 0f);
            }

            spriteBatch.Draw(texture, baseDrawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, origin, projectile.scale, 0, 0f);
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);
    }
}
