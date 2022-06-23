using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena
{
    public class PulseBeamTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[0]) ? Main.npc[(int)Projectile.ai[0]] : null;

        public float ConvergenceRatio => MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(25f, Lifetime * 0.667f, Time, true));

        public ref float StartingRotationalOffset => ref Projectile.ai[1];

        public ref float ConvergenceAngle => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];

        public const int Lifetime = 180;

        public const float TelegraphWidth = 3600f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Pulse Disintegration Beam Telegraph");

        }

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
            if (ThingToAttachTo is null || !ThingToAttachTo.active || ThingToAttachTo.ai[0] != (int)AthenaNPC.AthenaAttackType.AimedPulseLasers)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = ThingToAttachTo.ModNPC<AthenaNPC>().MainTurretCenter;
            Projectile.rotation = StartingRotationalOffset.AngleLerp(ConvergenceAngle, ConvergenceRatio) + Projectile.velocity.ToRotation();

            Time++;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D laserTelegraph = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam").Value;

            float verticalScale = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true) * 4f;

            Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
            Vector2 scaleInner = new(TelegraphWidth / laserTelegraph.Width, verticalScale);
            Vector2 scaleOuter = scaleInner * new Vector2(1f, 1.85f);

            // Iterate through purple and fuchisa twice and then flash.
            Color colorOuter = Color.Lerp(Color.Purple, Color.Fuchsia, Time / Lifetime * 2f % 1f);
            colorOuter = Color.Lerp(colorOuter, new Color(1f, 1f, 1f, 0f), Utils.GetLerpValue(40f, 0f, Projectile.timeLeft, true) * 0.8f);
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.5f);

            colorInner *= 0.85f;
            colorOuter *= 0.7f;

            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, Projectile.rotation, origin, scaleOuter, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, Projectile.rotation, origin, scaleInner, SpriteEffects.None, 0);
            return false;
        }
    }
}
