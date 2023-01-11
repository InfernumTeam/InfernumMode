using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresBeamTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[0]) ? Main.npc[(int)Projectile.ai[0]] : null;
        
        public float ConvergenceRatio => MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(Lifetime * 0.2f, Lifetime * 0.66f, Time, true));

        public ref float StartingRotationalOffset => ref Projectile.ai[1];

        public ref float ConvergenceAngle => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];

        public const int Lifetime = 40;

        public const float TelegraphWidth = 3600f;

        public const float BeamPosOffset = 16f;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Gamma Disintegration Beam Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 4;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
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
                Projectile.Kill();
                return;
            }

            // Set the starting location of the beam to the center of the NPC.
            Projectile.rotation = StartingRotationalOffset.AngleLerp(ConvergenceAngle, ConvergenceRatio);
            Projectile.Center = ThingToAttachTo.Center + Vector2.UnitY * 20f + Projectile.rotation.ToRotationVector2() * 18f;
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D laserTelegraph = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam").Value;

            float verticalScale = Utils.GetLerpValue(0f, 10f, Time, true) * Utils.GetLerpValue(0f, 7f, Projectile.timeLeft, true) * 4f;

            Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
            Vector2 scaleInner = new(TelegraphWidth / laserTelegraph.Width, verticalScale);
            Vector2 scaleOuter = scaleInner * new Vector2(1f, 2.2f);

            Color colorOuter = Color.Lerp(Color.Red, Color.Crimson, Time / Lifetime * 2f % 1f); // Iterate through crimson and red twice and then flash.
            colorOuter = Color.Lerp(colorOuter, Color.White, Utils.GetLerpValue(12f, 0f, Projectile.timeLeft, true) * 0.8f);
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.5f);

            colorInner *= 0.85f;
            colorOuter *= 0.7f;

            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, Projectile.rotation, origin, scaleOuter, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, Projectile.rotation, origin, scaleInner, SpriteEffects.None, 0f);
            return false;
        }
    }
}
