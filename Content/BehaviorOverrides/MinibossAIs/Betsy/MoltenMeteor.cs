using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.Betsy
{
    public class MoltenMeteor : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Meteor");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 210;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.ai[0] = Main.rand.Next(3);
                Projectile.ai[1] = Main.rand.NextFloat(0.9f, 1.1f);
                Projectile.localAI[0] = 1f;
                Projectile.netUpdate = true;
            }

            Projectile.scale = Projectile.ai[1];
            Projectile.rotation += Projectile.velocity.X * 0.045f;
            Projectile.velocity *= 0.987f;

            if (Projectile.velocity.Length() > 5f)
            {
                Vector2 position = Projectile.Center + Vector2.Normalize(Projectile.velocity) * 10f;
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 244, 0f, 0f, 0, new Color(255, 127, 0), 1f);
                fire.position = position;
                fire.velocity = Projectile.velocity.RotatedBy(PiOver2) * 0.33f + Projectile.velocity / 4f;
                fire.position += Projectile.velocity.RotatedBy(PiOver2) + Main.rand.NextVector2Circular(8f, 8f);
                fire.fadeIn = 0.5f;
                fire.noGravity = true;

                fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 244, 0f, 0f, 0, new Color(255, 127, 0), 1f);
                fire.position = position;
                fire.velocity = Projectile.velocity.RotatedBy(-PiOver2) * 0.33f + Projectile.velocity / 4f;
                fire.position += Projectile.velocity.RotatedBy(-PiOver2) + Main.rand.NextVector2Circular(8f, 8f);
                fire.fadeIn = 0.5f;
                fire.noGravity = true;

                fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 244, 0f, 0f, 0, new Color(255, Main.DiscoG, 0), 1f);
                fire.velocity *= 0.5f;
                fire.scale *= 1.3f;
                fire.fadeIn = 1f;
                fire.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMolten").Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow").Value;
            switch ((int)Projectile.ai[0])
            {
                case 0:
                    break;
                case 1:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMolten2").Value;
                    glowmask = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow2").Value;
                    break;
                case 2:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMolten3").Value;
                    glowmask = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow3").Value;
                    break;
                case 3:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMolten4").Value;
                    glowmask = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow4").Value;
                    break;
                case 4:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMolten5").Value;
                    glowmask = null;
                    break;
                case 5:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMolten6").Value;
                    glowmask = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow6").Value;
                    break;
                default:
                    break;
            }
            Vector2 origin = texture.Size() / 2f;
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1, texture);

            if (glowmask != null)
                Main.spriteBatch.Draw(glowmask, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 cinderVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(10.5f, 14f);
                Utilities.NewProjectileBetter(Projectile.Center, cinderVelocity, ModContent.ProjectileType<MeteorCinder>(), 170, 0f);
            }
        }
    }
}
