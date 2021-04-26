using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.WallOfFlesh
{
    public class CursedSoul : ModProjectile
    {
        public bool SoulOfNight
		{
            get => projectile.ai[0] == 1f;
            set => projectile.ai[0] = value.ToInt();
        }
        public bool ShouldFloatUpward => projectile.localAI[1] == 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Lighting.AddLight(projectile.Center, Color.WhiteSmoke.ToVector3());
            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

            projectile.frameCounter++;
            projectile.frame = (projectile.frameCounter / 4) % Main.projFrames[projectile.type];

            if (projectile.ai[1] == 0f)
            {
                projectile.spriteDirection = Main.rand.NextBool(2).ToDirectionInt();
                SoulOfNight = Main.rand.NextBool(2);
                projectile.ai[1] = 1f;
                projectile.netUpdate = true;
            }

            if (ShouldFloatUpward)
			{
                projectile.velocity.X *= MathHelper.Lerp(0.982f, 0.974f, projectile.identity % 8f / 8f);
                if (projectile.velocity.Y > -20f)
                    projectile.velocity.Y -= 0.24f;
			}
            else if (SoulOfNight)
            {
                projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(projectile.velocity.Length(), 7f, 0.1f);
                projectile.velocity = projectile.velocity.RotatedBy(projectile.spriteDirection * MathHelper.TwoPi / 120f);

                if (!projectile.WithinRange(target.Center, 60f))
                    projectile.Center += projectile.DirectionTo(target.Center) * 4f;
            }
            else
            {
                projectile.velocity.X += Math.Sign(target.Center.X - projectile.Center.X) * 0.2f;
                projectile.velocity.Y += Math.Sign(target.Center.Y - projectile.Center.Y) * 0.2f;
                projectile.velocity = Vector2.Clamp(projectile.velocity, Vector2.One * -7f, Vector2.One * 7f);
            }

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (projectile.timeLeft < 25)
                projectile.alpha = Utils.Clamp(projectile.alpha + 13, 0, 255);
            else
                projectile.alpha = Utils.Clamp(projectile.alpha - 9, 0, 255);
        }

		public override Color? GetAlpha(Color lightColor)
		{
			return (SoulOfNight ? Color.Lerp(Color.MediumPurple, Color.Black, 0.6f) : Color.Wheat) * projectile.Opacity;
		}

		public override void Kill(int timeLeft)
        {
            CalamityGlobalProjectile.ExpandHitboxBy(projectile, 60);
            projectile.alpha = 0;
            projectile.Damage();

            Main.PlaySound(SoundID.NPCDeath39, projectile.position);
            for (int i = 0; i < 36; i++)
            {
                Dust ectoplasm = Dust.NewDustPerfect(projectile.Center, 267);
                ectoplasm.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 8f;
                ectoplasm.scale = 1.5f;
                ectoplasm.noGravity = true;
                ectoplasm.color = projectile.GetAlpha(Color.White);
            }
        }
	}
}
