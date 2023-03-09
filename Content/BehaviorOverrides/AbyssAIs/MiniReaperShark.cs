using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class MiniReaperShark : ModProjectile
    {
        public float SpinOffsetAngle;

        public NPC ReaperShark => Main.npc[(int)Projectile.ai[0]];

        public Player Target => Main.player[ReaperShark.target];

        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Reaper Shark");
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 480;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(new BitsByte(Projectile.tileCollide));
            writer.Write(SpinOffsetAngle);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.tileCollide = ((BitsByte)reader.ReadByte())[0];
            SpinOffsetAngle = reader.ReadSingle();
        }

        public override void AI()
        {
            // Fade in.
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.02f, 0f, 1f);

            // Disappear if there is no reaper shark.
            if (!ReaperShark.active)
            {
                Projectile.Kill();
                return;
            }

            // Determine frames.
            Projectile.frameCounter++;
            Projectile.frame = ((Projectile.frameCounter + Projectile.identity * 2) / 5) % Main.projFrames[Type];

            // Spin around the reaper shark.
            if (Projectile.velocity == Vector2.Zero)
            {
                float spinAcceleration = Utils.GetLerpValue(135f, 60f, Time, true) * MathHelper.Pi / 70f;
                Projectile.Center = ReaperShark.Center + SpinOffsetAngle.ToRotationVector2() * 240f;
                SpinOffsetAngle = MathHelper.WrapAngle(SpinOffsetAngle + spinAcceleration);

                // Look at the target.
                Projectile.rotation = ReaperShark.AngleTo(Target.Center);
                Projectile.spriteDirection = (Target.Center.X < Projectile.Center.X).ToDirectionInt();
                if (Projectile.spriteDirection == 1)
                    Projectile.rotation += MathHelper.Pi;
                return;
            }

            // Collide with tiles after charging.
            if (!Projectile.tileCollide)
            {
                Projectile.tileCollide = true;
                Projectile.netUpdate = true;
            }

            // Accelerate.
            Projectile.velocity = (Projectile.velocity * 1.024f).ClampMagnitude(7f, 29f);

            // Rotate based on direction.
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = (Projectile.velocity.X < 0f).ToDirectionInt();
            if (Projectile.spriteDirection == 1)
                Projectile.rotation += MathHelper.Pi;

            Time++;
        }

        public override bool? CanDamage() => Projectile.velocity == Vector2.Zero ? false : null;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color color = Color.Lerp(lightColor, Color.White, 0.65f) * Projectile.Opacity;

            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0));
            return false;
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;
    }
}
