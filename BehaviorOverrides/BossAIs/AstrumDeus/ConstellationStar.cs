using CalamityMod.Dusts;
using CalamityMod.NPCs.AstrumDeus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class ConstellationStar : ModProjectile
    {
        public ref float Index => ref Projectile.ai[0];
        public ref float Time => ref Projectile.ai[1];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Star");

        public override void SetDefaults()
        {
            Projectile.scale = Main.rand?.NextFloat(0.55f, 1f) ?? 1f;
            Projectile.width = Projectile.height = (int)(Projectile.scale * 64f);
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 90000;
        }

        public override void AI()
        {
            if (Projectile.timeLeft > 60 && !NPC.AnyNPCs(ModContent.NPCType<AstrumDeusHead>()))
                Projectile.timeLeft = 60;

            if (Projectile.timeLeft < 60)
                Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 0.002f, 0.1f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile projectileToConnectTo = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type != Projectile.type || !Main.projectile[i].active || Main.projectile[i].timeLeft < 50 || Main.projectile[i].ai[0] != Index - 1f)
                    continue;

                projectileToConnectTo = Main.projectile[i];
                break;
            }

            Color starColor = Projectile.identity % 3 == 0 ? new Color(109, 242, 196) : new Color(255, 132, 66);
            starColor.A = 0;

            Texture2D starTexture = TextureAssets.Projectile[Projectile.type].Value;
            float scaleFactor = Utils.GetLerpValue(0f, 35f, Time, true);

            for (int i = 0; i < 8; i++)
            {
                Vector2 drawPosition = Projectile.Center - Main.screenPosition + (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2f;
                Main.spriteBatch.Draw(starTexture, drawPosition, null, starColor, 0f, starTexture.Size() * 0.5f, Projectile.scale * scaleFactor, SpriteEffects.None, 0f);
            }

            if (projectileToConnectTo != null)
            {
                float projectileToConnectToScaleFactor = Utils.GetLerpValue(0f, 35f, projectileToConnectTo.ai[0], true);
                Texture2D lineTexture = TextureAssets.Extra[47].Value;
                Vector2 connectionDirection = Projectile.SafeDirectionTo(projectileToConnectTo.Center);
                Vector2 start = Projectile.Center + connectionDirection * Projectile.scale * 24f;
                Vector2 end = projectileToConnectTo.Center - connectionDirection * Projectile.scale * projectileToConnectToScaleFactor * 24f;
                Vector2 scale = new(scaleFactor * 1.5f, (start - end).Length() / lineTexture.Height);
                Vector2 origin = new(lineTexture.Width * 0.5f, 0f);
                Color drawColor = Color.White;
                drawColor.A = 0;

                float rotation = (end - start).ToRotation() - MathHelper.PiOver2;

                Main.spriteBatch.Draw(lineTexture, start - Main.screenPosition, null, drawColor, rotation, origin, scale, SpriteEffects.None, 0f);
            }

            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item91, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Dust astralFire = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? ModContent.DustType<AstralBlue>() : ModContent.DustType<AstralOrange>());
                astralFire.position += Main.rand.NextVector2Circular(15f, 15f);
                astralFire.velocity = Main.rand.NextVector2Circular(4f, 4f);
                astralFire.scale = Main.rand.NextFloat(1.2f, 1.6f);
                astralFire.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < Main.rand.Next(2, 4); i++)
            {
                Vector2 starVelocity = -Vector2.UnitY.RotatedByRandom(0.37f) * Main.rand.NextFloat(8f, 10f);
                int star = Utilities.NewProjectileBetter(Projectile.Center + starVelocity * 1.5f, starVelocity, ModContent.ProjectileType<AstralStar>(), 165, 0f);
                if (Main.projectile.IndexInRange(star))
                    Main.projectile[star].ai[1] = 0.4f;
            }
        }
    }
}
