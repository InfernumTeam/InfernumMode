using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresDeathBeamTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float TelegraphDelay => ref projectile.ai[0];

        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)projectile.ai[1]) ? Main.npc[(int)projectile.ai[1]] : null;

        public ref float TelegraphLifetime => ref projectile.localAI[0];

        public Vector2 OldVelocity;
        public const float TelegraphFadeTime = 8f;
        public const float TelegraphWidth = 2800f;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Exo Overload Telegraph");

        public override void SetDefaults()
        {
            projectile.width = 10;
            projectile.height = 10;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 600;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(OldVelocity);
            writer.Write(TelegraphLifetime);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OldVelocity = reader.ReadVector2();
            TelegraphLifetime = reader.ReadSingle();
        }

        public override void AI()
        {
            // Die if the thing to attach to disappears.
            if (ThingToAttachTo is null || !ThingToAttachTo.active)
            {
                projectile.Kill();
                return;
            }

            // Set start of telegraph to the npc center.
            projectile.Center = ThingToAttachTo.Center + new Vector2(-1f, 23f) + (OldVelocity.SafeNormalize(Vector2.Zero) * 17f);

            // Be sure to save the velocity the projectile started with. It will be set again when the telegraph is over.
            if (OldVelocity == Vector2.Zero)
            {
                OldVelocity = projectile.velocity;
                projectile.velocity = Vector2.Zero;
                projectile.netUpdate = true;
                projectile.rotation = OldVelocity.ToRotation() + MathHelper.PiOver2;
            }

            TelegraphDelay++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, projectile.alpha);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (TelegraphDelay >= TelegraphLifetime)
                return true;

            Texture2D laserTelegraph = ModContent.GetTexture("CalamityMod/ExtraTextures/LaserWallTelegraphBeam");
            float yScale = 2f;
            if (TelegraphDelay < TelegraphFadeTime)
                yScale = MathHelper.Lerp(0f, 2f, TelegraphDelay / 15f);
            if (TelegraphDelay > TelegraphLifetime - TelegraphFadeTime)
                yScale = MathHelper.Lerp(2f, 0f, (TelegraphDelay - (TelegraphLifetime - TelegraphFadeTime)) / 15f);

            Vector2 scaleInner = new Vector2(TelegraphWidth / laserTelegraph.Width, yScale);
            Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
            Vector2 scaleOuter = scaleInner * new Vector2(1f, 1.6f);

            Color colorOuter = CalamityUtils.MulticolorLerp(Utils.InverseLerp(-MathHelper.Pi, MathHelper.Pi, OldVelocity.ToRotation(), true), CalamityUtils.ExoPalette);
            colorOuter.A = 36;
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.4f);

            colorOuter *= 0.7f;
            colorInner *= 0.7f;

            Main.spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorInner, OldVelocity.ToRotation(), origin, scaleInner, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorOuter, OldVelocity.ToRotation(), origin, scaleOuter, SpriteEffects.None, 0f);

            return false;
        }
    }
}
