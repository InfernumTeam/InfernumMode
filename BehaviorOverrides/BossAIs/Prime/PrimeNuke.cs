using CalamityMod.Events;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeNuke : ModProjectile
    {
        public const int Lifetime = 240;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Nuke");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 52;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(240f, 210f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            float movementSpeed = BossRushEvent.BossRushActive ? 21f : 10f;
            if (!Projectile.WithinRange(closestPlayer.Center, 180) && Projectile.timeLeft > 70)
                Projectile.velocity = (Projectile.velocity * 34f + Projectile.SafeDirectionTo(closestPlayer.Center) * movementSpeed) / 35f;

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawCircularTelegraph();

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            float fadeToRed = Utils.GetLerpValue(65f, 10f, Projectile.timeLeft, true) * 0.8f;
            Color redFade = Color.Red * 0.67f;
            redFade.A = 0;

            Color drawColor = Projectile.GetAlpha(Color.Lerp(lightColor, redFade, fadeToRed));
            float outwardness = fadeToRed * 3f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * outwardness;
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + drawOffset, null, drawColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public void DrawCircularTelegraph()
        {
            Main.spriteBatch.EnterShaderRegion();
            Texture2D telegraphBase = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;

            float lifetimeCompletion = 1f - Projectile.timeLeft / (float)Lifetime;
            GameShaders.Misc["CalamityMod:CircularAoETelegraph"].UseOpacity(MathHelper.Clamp(lifetimeCompletion * 1.6f, 0f, 0.2f));
            GameShaders.Misc["CalamityMod:CircularAoETelegraph"].UseColor(Color.Lerp(Color.OrangeRed, Color.Red, 0.7f * (float)Math.Pow(Math.Sin(Main.GlobalTimeWrappedHourly) * 0.5f + 0.5f, 3)));
            GameShaders.Misc["CalamityMod:CircularAoETelegraph"].UseSecondaryColor(Color.Lerp(Color.Red, Color.White, 0.5f));
            GameShaders.Misc["CalamityMod:CircularAoETelegraph"].UseSaturation(lifetimeCompletion);
            GameShaders.Misc["CalamityMod:CircularAoETelegraph"].Apply();

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            float maxRadius = (NuclearExplosion.BaseScale + NuclearExplosion.ScaleExpandRate * NuclearExplosion.Lifetime) * NuclearExplosion.RadiusScaleFactor + 54f;
            Main.EntitySpriteDraw(telegraphBase, drawPosition, null, Color.White, 0, telegraphBase.Size() / 2f, maxRadius, 0, 0);
            Main.spriteBatch.ExitShaderRegion();
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(GaussRifle.FireSound, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<NuclearExplosion>(), 220, 0f);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => false;
    }
}
