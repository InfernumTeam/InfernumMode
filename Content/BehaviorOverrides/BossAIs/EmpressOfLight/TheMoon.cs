using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class TheMoon : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public static bool MoonIsNotInSky => Utilities.AnyProjectiles(ModContent.ProjectileType<TheMoon>()) && !Main.dayTime;
        
        public override void SetStaticDefaults() => DisplayName.SetDefault("The Moon");

        public override void SetDefaults()
        {
            Projectile.width = 942;
            Projectile.height = 942;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 72000;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
        }

        public override void AI()
        {
            // Disappear if the empress is not present.
            if (!NPC.AnyNPCs(NPCID.HallowBoss))
                Projectile.Kill();

            if (Projectile.timeLeft < 90)
                Projectile.damage = 0;

            Time++;

            // Slowly spin around.
            float angularVelocity = MathHelper.Clamp(Time / 240f, 0f, 1f) * MathHelper.Pi * 0.005f;
            Projectile.rotation += angularVelocity;
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * 0.44f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloomFlare = ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/GreyscaleObjects/BloomFlare").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            Color bloomFlareColor = Color.Lerp(Color.Wheat, Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.2f % 1f, 1f, 0.7f), 0.1f);
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 0.93f;
            float bloomFlareScale = Projectile.scale * 3f;
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, -bloomFlareRotation, bloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Color color = Projectile.GetAlpha(Color.Wheat);
            color = Color.Lerp(color, Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.2f % 1f, 1f, 0.8f), 0.7f) * 0.9f;
            color.A = 127;
            Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale, 0, 0f);

            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
    }
}
