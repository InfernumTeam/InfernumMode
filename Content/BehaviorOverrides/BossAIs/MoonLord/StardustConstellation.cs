using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class StardustConstellation : ModProjectile
    {
        public ref float Index => ref Projectile.ai[0];

        public ref float Time => ref Projectile.localAI[1];

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LaserCircle";

        public override void SetDefaults()
        {
            Projectile.scale = Main.rand?.NextFloat(0.8f, 1f) ?? 1f;
            Projectile.width = Projectile.height = (int)(Projectile.scale * 64f);
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 900;
            
        }

        public override void AI()
        {
            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
                Projectile.active = false;

            if (Projectile.timeLeft < 60)
                Projectile.Opacity = Lerp(Projectile.Opacity, 0.002f, 0.1f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile projectileToConnectTo = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type != Projectile.type || !Main.projectile[i].active ||
                    Main.projectile[i].timeLeft < 25f || Main.projectile[i].ai[0] != Index - 1f ||
                    Main.projectile[i].ai[1] != Projectile.ai[1])
                {
                    continue;
                }

                projectileToConnectTo = Main.projectile[i];
                break;
            }

            float fadeToOrange = Utils.GetLerpValue(50f, 0f, Projectile.timeLeft, true) * 0.4f;
            Color stardustColor = new(0, 213, 255);
            Color solarColor = new(255, 140, 0);
            Color starColor = Color.Lerp(stardustColor, solarColor, fadeToOrange);

            Texture2D starTexture = TextureAssets.Projectile[Projectile.type].Value;
            float scaleFactor = Utils.GetLerpValue(0f, 15f, Time, true) + Utils.GetLerpValue(30f, 0f, Projectile.timeLeft, true) * 2f;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            for (int i = 0; i < 16; i++)
            {
                float drawOffsetFactor = (Cos(Main.GlobalTimeWrappedHourly * 40f) * 0.5f + 0.5f) * scaleFactor * fadeToOrange * 8f + 1f;
                Vector2 drawOffset = (TwoPi * i / 16f).ToRotationVector2() * drawOffsetFactor;
                Main.spriteBatch.Draw(starTexture, drawPosition + drawOffset, null, starColor * 0.4f, 0f, starTexture.Size() * 0.5f, Projectile.scale * scaleFactor, 0, 0f);
            }
            Main.spriteBatch.Draw(starTexture, drawPosition, null, starColor * 4f, 0f, starTexture.Size() * 0.5f, Projectile.scale * scaleFactor, 0, 0f);

            if (projectileToConnectTo != null)
            {
                Texture2D lineTexture = TextureAssets.Extra[ExtrasID.StardustTowerMark].Value;
                Vector2 connectionDirection = Projectile.SafeDirectionTo(projectileToConnectTo.Center);
                Vector2 start = Projectile.Center + connectionDirection * Projectile.scale * 24f;
                Vector2 end = projectileToConnectTo.Center - connectionDirection * Projectile.scale * 24f;
                Vector2 scale = new(scaleFactor * 1.5f, (start - end).Length() / lineTexture.Height);
                Vector2 origin = new(lineTexture.Width * 0.5f, 0f);
                Color drawColor = Color.White;
                float rotation = (end - start).ToRotation() - PiOver2;

                Main.spriteBatch.Draw(lineTexture, start - Main.screenPosition, null, drawColor, rotation, origin, scale, 0, 0f);
            }
            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item91, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 initialVelocity = Vector2.UnitY * 6.5f;
            if (Projectile.identity % 2f == 1f)
                initialVelocity = initialVelocity.RotatedBy(PiOver2);

            Utilities.NewProjectileBetter(Projectile.Center, -initialVelocity, ProjectileID.CultistBossFireBall, MoonLordCoreBehaviorOverride.FireballDamage, 0f);
            Utilities.NewProjectileBetter(Projectile.Center, initialVelocity, ProjectileID.CultistBossFireBall, MoonLordCoreBehaviorOverride.FireballDamage, 0f);
            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordExplosion>(), 0, 0f);
        }
    }
}
