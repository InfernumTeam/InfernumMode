using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class SwirlingFire : ModProjectile
    {
        public ref float AngularTurnSpeed => ref Projectile.ai[0];
        public ref float Time => ref Projectile.ai[1];

        public float MaxScale = 0f;
        public const int FadeinTime = 180;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.scale = 0.04f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Time++;
            if (MaxScale == 0f)
                MaxScale = Main.rand.NextFloat(0.8f, 1.25f);

            if (Time >= FadeinTime - 45)
                Projectile.velocity *= 0.94f;
            if (Time >= FadeinTime)
            {
                // Fizzle out when close to death. 
                if (!Main.dedServ && Projectile.timeLeft < 60)
                {
                    for (int i = 1; i <= 1; i += 2)
                    {
                        Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f), !Main.dayTime ? 245 : 6);
                        fire.velocity = Main.rand.NextVector2Circular(3f, 3f);
                        fire.scale = Main.rand.NextFloat(1.3f, 1.45f);
                        fire.noGravity = true;
                    }
                    Projectile.scale *= 0.95f;
                    Projectile.width = Projectile.height = (int)(40 * Projectile.scale);
                }
                else if (Projectile.timeLeft >= 60)
                {
                    Projectile.velocity = Vector2.Zero;
                    Projectile.scale = MathHelper.Lerp(Projectile.scale, MaxScale, 0.13f);
                    Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1f, 0.25f);

                    Projectile.frameCounter++;
                    Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Projectile.type];

                    if (Projectile.scale < MaxScale)
                        Projectile.width = Projectile.height = (int)(40 * Projectile.scale);
                }

                Lighting.AddLight(Projectile.Center, Color.White.ToVector3());
            }

            // Release a bunch of fiery dust from the cinder before it burns.
            else
            {
                if (!Main.dedServ)
                {
                    for (int i = 1; i <= 1; i += 2)
                    {
                        Vector2 fireVelocity = (Time / 6f).ToRotationVector2().RotatedBy(i * MathHelper.PiOver2) * Main.rand.NextFloat(1.7f, 2.2f);
                        Dust fire = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f), !Main.dayTime ? 245 : 6);
                        fire.velocity = fireVelocity;
                        fire.scale = Main.rand.NextFloat(1.3f, 1.45f);
                        fire.noGravity = true;
                    }
                }
                Projectile.velocity = Projectile.velocity.RotatedBy(AngularTurnSpeed);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            if (!Main.dayTime)
                texture = ModContent.Request<Texture2D>($"{Texture}Night").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor * 1.3f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Time >= FadeinTime + 30f;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            
        }
    }
}
