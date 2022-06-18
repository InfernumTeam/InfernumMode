using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresBeamTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)projectile.ai[0]) ? Main.npc[(int)projectile.ai[0]] : null;
        public float ConvergenceRatio => MathHelper.SmoothStep(0f, 1f, Utils.InverseLerp(Lifetime * 0.2f, Lifetime * 0.66f, Time, true));
        public ref float StartingRotationalOffset => ref projectile.ai[1];
        public ref float ConvergenceAngle => ref projectile.localAI[0];
        public ref float Time => ref projectile.localAI[1];
        public const int Lifetime = 40;
        public const float TelegraphWidth = 3600f;
        public const float BeamPosOffset = 16f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Gamma Disintegration Beam Telegraph");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 4;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = Lifetime;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ConvergenceAngle);
            writer.Write(Time);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ConvergenceAngle = reader.ReadSingle();
            Time = reader.ReadSingle();
        }

        public override void AI()
        {
            // Die if the thing to attach to disappears.
            if (ThingToAttachTo is null || !ThingToAttachTo.active)
            {
                projectile.Kill();
                return;
            }

            // Set the starting location of the beam to the center of the NPC.
            projectile.rotation = StartingRotationalOffset.AngleLerp(ConvergenceAngle, ConvergenceRatio);
            projectile.Center = ThingToAttachTo.Center + Vector2.UnitY * 20f + projectile.rotation.ToRotationVector2() * 18f;
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D laserTelegraph = ModContent.GetTexture("CalamityMod/ExtraTextures/LaserWallTelegraphBeam");

            float verticalScale = Utils.InverseLerp(0f, 10f, Time, true) * Utils.InverseLerp(0f, 7f, projectile.timeLeft, true) * 4f;

            Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
            Vector2 scaleInner = new Vector2(TelegraphWidth / laserTelegraph.Width, verticalScale);
            Vector2 scaleOuter = scaleInner * new Vector2(1f, 2.2f);

            Color colorOuter = Color.Lerp(Color.Red, Color.Crimson, Time / Lifetime * 2f % 1f); // Iterate through crimson and red twice and then flash.
            colorOuter = Color.Lerp(colorOuter, Color.White, Utils.InverseLerp(12f, 0f, projectile.timeLeft, true) * 0.8f);
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.5f);

            colorInner *= 0.85f;
            colorOuter *= 0.7f;

            spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorOuter, projectile.rotation, origin, scaleOuter, SpriteEffects.None, 0f);
            spriteBatch.Draw(laserTelegraph, projectile.Center - Main.screenPosition, null, colorInner, projectile.rotation, origin, scaleInner, SpriteEffects.None, 0f);
            return false;
        }
    }
}
