using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class AresDeathBeamTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float TelegraphDelay => ref Projectile.ai[0];

        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[1]) ? Main.npc[(int)Projectile.ai[1]] : null;

        public ref float TelegraphLifetime => ref Projectile.localAI[0];

        public Vector2 OldVelocity;
        public const float TelegraphFadeTime = 8f;
        public const float TelegraphWidth = 4000f;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Exo Overload Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
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
                Projectile.Kill();
                return;
            }

            // Set start of telegraph to the npc center.
            Projectile.Center = ThingToAttachTo.Center + new Vector2(-1f, 23f) + (OldVelocity.SafeNormalize(Vector2.Zero) * 17f);

            // Be sure to save the velocity the projectile started with. It will be set again when the telegraph is over.
            if (OldVelocity == Vector2.Zero)
            {
                OldVelocity = Projectile.velocity;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
                Projectile.rotation = OldVelocity.ToRotation() + MathHelper.PiOver2;
            }

            TelegraphDelay++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TelegraphDelay >= TelegraphLifetime)
                return true;

            Texture2D laserTelegraph = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam").Value;
            float yScale = 2f;
            if (TelegraphDelay < TelegraphFadeTime)
                yScale = MathHelper.Lerp(0f, 2f, TelegraphDelay / 15f);
            if (TelegraphDelay > TelegraphLifetime - TelegraphFadeTime)
                yScale = MathHelper.Lerp(2f, 0f, (TelegraphDelay - (TelegraphLifetime - TelegraphFadeTime)) / 15f);

            Vector2 scaleInner = new(TelegraphWidth / laserTelegraph.Width, yScale);
            Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
            Vector2 scaleOuter = scaleInner * new Vector2(1f, 1.6f);

            Color colorOuter = CalamityUtils.MulticolorLerp(Utils.GetLerpValue(-MathHelper.Pi, MathHelper.Pi, OldVelocity.ToRotation(), true), CalamityUtils.ExoPalette);
            colorOuter.A = 36;
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.4f);

            colorOuter *= 0.7f;
            colorInner *= 0.7f;

            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, OldVelocity.ToRotation(), origin, scaleInner, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, OldVelocity.ToRotation(), origin, scaleOuter, SpriteEffects.None, 0f);

            return false;
        }
    }
}
