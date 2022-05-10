using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
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
            else
            {
                if (projectile.timeLeft > 190f)
                {
                    projectile.velocity = Vector2.Lerp(projectile.velocity, -Vector2.UnitY * 8f, 0.05f);
                    projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
                }
                else if (projectile.timeLeft > 160f)
                {
                    projectile.velocity = Vector2.Lerp(projectile.velocity, Vector2.Zero, 0.05f).MoveTowards(Vector2.Zero, 0.1f);
                    projectile.rotation = projectile.rotation.AngleLerp(projectile.AngleTo(target.Center) - MathHelper.PiOver2, 0.12f);
                }

                float maxSpeed = SoulOfNight ? 14.25f : 12f;
                float acceleration = SoulOfNight ? 1.025f : 1.03f;
                if (projectile.timeLeft == 160f)
                    projectile.velocity = projectile.SafeDirectionTo(target.Center) * 3f;
                if (projectile.timeLeft < 160f && projectile.velocity.Length() < maxSpeed)
                    projectile.velocity *= acceleration;
            }

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
