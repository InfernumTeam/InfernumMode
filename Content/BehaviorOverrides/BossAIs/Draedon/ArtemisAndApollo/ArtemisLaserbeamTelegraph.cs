using CalamityMod;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ArtemisLaserbeamTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public NPC Artemis => Main.npc.IndexInRange((int)Projectile.ai[0]) ? Main.npc[(int)Projectile.ai[0]] : null;

        public float ConvergenceRatio => MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(25f, 120f, Time, true));

        public ref float StartingRotationalOffset => ref Projectile.ai[1];

        public ref float ConvergenceAngle => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];

        public static int TrueLifetime => RawLifetime / TotalUpdates;

        public const int TotalUpdates = 5;

        public const int RawLifetime = 180;

        public const float TelegraphWidth = 3600f;

        public const float BeamPosOffset = 16f;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Laserbeam Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 4;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = TotalUpdates;
            Projectile.timeLeft = RawLifetime;
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
            if (Artemis is null || !Artemis.active)
            {
                Projectile.Kill();
                return;
            }

            // Offset to move the beam forward so that it starts at the edge of Artemis' laser firing mechanism.
            float beamStartForwardsOffset = ExoMechManagement.ExoTwinsAreInSecondPhase ? 104f : 72f;

            // Set the starting location of the beam to the center of the NPC.
            Projectile.Center = Artemis.Center;

            // Add the forwards offset, measured in pixels.
            float normalizedArtemisDirection = Artemis.rotation - MathHelper.PiOver2;
            Projectile.position += normalizedArtemisDirection.ToRotationVector2() * beamStartForwardsOffset;
            Projectile.rotation = StartingRotationalOffset.AngleLerp(ConvergenceAngle, ConvergenceRatio) + normalizedArtemisDirection;

            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.03f, 0f, 1f);

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D laserTelegraph = InfernumTextureRegistry.BloomLineSmall.Value;

            float verticalScale = Utils.GetLerpValue(0f, 20f, Time, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true) * 0.5f;
            verticalScale += CalamityUtils.Convert01To010(Utils.GetLerpValue(20f, 67f, Projectile.timeLeft, true)) * 0.5f;

            Vector2 origin = laserTelegraph.Size() * new Vector2(0.5f, 0f);
            Vector2 scaleInner = new(verticalScale, TelegraphWidth / laserTelegraph.Height);
            Vector2 scaleOuter = scaleInner * new Vector2(2.2f, 1f);

            Color colorOuter = Color.Lerp(Color.Orange, Color.White, Utils.GetLerpValue(67f, 0f, Projectile.timeLeft, true) * 0.8f);
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.75f);

            Main.EntitySpriteDraw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, Projectile.rotation - MathHelper.PiOver2, origin, scaleOuter, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, Projectile.rotation - MathHelper.PiOver2, origin, scaleInner, SpriteEffects.None, 0);
            Main.spriteBatch.ResetBlendState();
            return false;
        }
    }
}
