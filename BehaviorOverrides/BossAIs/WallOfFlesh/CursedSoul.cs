using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class CursedSoul : ModProjectile
    {
        public bool SoulOfNight
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }
        public bool ShouldFloatUpward => Projectile.localAI[1] == 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.WhiteSmoke.ToVector3());
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

            Projectile.frameCounter++;
            Projectile.frame = (Projectile.frameCounter / 4) % Main.projFrames[Projectile.type];

            if (Projectile.ai[1] == 0f)
            {
                Projectile.spriteDirection = Main.rand.NextBool(2).ToDirectionInt();
                SoulOfNight = Main.rand.NextBool(2);
                Projectile.ai[1] = 1f;
                Projectile.netUpdate = true;
            }

            if (ShouldFloatUpward)
            {
                Projectile.velocity.X *= MathHelper.Lerp(0.982f, 0.974f, Projectile.identity % 8f / 8f);
                if (Projectile.velocity.Y > -20f)
                    Projectile.velocity.Y -= 0.24f;
            }
            else
            {
                if (Projectile.timeLeft > 190f)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, -Vector2.UnitY * 8f, 0.05f);
                    Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
                }
                else if (Projectile.timeLeft > 160f)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.Zero, 0.05f).MoveTowards(Vector2.Zero, 0.1f);
                    Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(target.Center) - MathHelper.PiOver2, 0.12f);
                }

                float maxSpeed = SoulOfNight ? 14.25f : 12f;
                float acceleration = SoulOfNight ? 1.025f : 1.03f;
                if (Projectile.timeLeft == 160f)
                    Projectile.velocity = Projectile.SafeDirectionTo(target.Center) * 3f;
                if (Projectile.timeLeft < 160f && Projectile.velocity.Length() < maxSpeed)
                    Projectile.velocity *= acceleration;
            }

            if (Projectile.timeLeft < 25)
                Projectile.alpha = Utils.Clamp(Projectile.alpha + 13, 0, 255);
            else
                Projectile.alpha = Utils.Clamp(Projectile.alpha - 9, 0, 255);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return (SoulOfNight ? Color.Lerp(Color.MediumPurple, Color.Black, 0.6f) : Color.Wheat) * Projectile.Opacity;
        }

        public override void Kill(int timeLeft)
        {
            CalamityGlobalProjectile.ExpandHitboxBy(Projectile, 60);
            Projectile.alpha = 0;
            Projectile.Damage();

            SoundEngine.PlaySound(SoundID.NPCDeath39, Projectile.position);
            for (int i = 0; i < 36; i++)
            {
                Dust ectoplasm = Dust.NewDustPerfect(Projectile.Center, 267);
                ectoplasm.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 8f;
                ectoplasm.scale = 1.5f;
                ectoplasm.noGravity = true;
                ectoplasm.color = Projectile.GetAlpha(Color.White);
            }
        }
    }
}
