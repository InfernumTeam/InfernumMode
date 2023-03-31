using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class OtherworldlyBolt : ModProjectile
    {
        public enum OtherwordlyBoltAttackState
        {
            LockIntoPosition,
            FlyIntoBackground,
            AccelerateFromBelow,
            ArcAndAccelerate
        }

        public float ArcAngularVelocity
        {
            get;
            set;
        }

        public OtherwordlyBoltAttackState AttackState
        {
            get => (OtherwordlyBoltAttackState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public ref float Time => ref Projectile.ai[1];

        public static int LockIntoPositionTime => 12;

        public static int DisappearIntoBackgroundTime => 90;

        public static Vector2 AimDirection => new Vector2(-1f, -1f).SafeNormalize(Vector2.UnitY);

        public static NPC CeaselessVoid => Main.npc[CalamityGlobalNPC.voidBoss];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Otherwordly Bolt");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 240;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(ArcAngularVelocity);

        public override void ReceiveExtraAI(BinaryReader reader) => ArcAngularVelocity = reader.ReadSingle();

        public override void AI()
        {
            // Disappear if the Ceaseless Void is not present.
            if (CalamityGlobalNPC.voidBoss == -1)
            {
                Projectile.Kill();
                return;
            }

            switch (AttackState)
            {
                case OtherwordlyBoltAttackState.LockIntoPosition:
                    DoBehavior_LockIntoPosition();
                    break;
                case OtherwordlyBoltAttackState.FlyIntoBackground:
                    DoBehavior_FlyIntoBackground();
                    break;
                case OtherwordlyBoltAttackState.AccelerateFromBelow:
                    DoBehavior_AccelerateFromBelow();
                    break;
                case OtherwordlyBoltAttackState.ArcAndAccelerate:
                    DoBehavior_ArcAndAccelerate();
                    break;
            }

            Time++;
        }

        public void DoBehavior_LockIntoPosition()
        {
            // Hover into position, offset from the ceaseless void.
            Vector2 hoverDestination = CeaselessVoid.Center + 120f * Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Projectile.Center = Vector2.Lerp(Projectile.Center, hoverDestination, 0.08f).MoveTowards(hoverDestination, 2f);

            // Aim in the direction that the bolt will fire in.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Begin flying into the background once ready.
            if (Time >= LockIntoPositionTime)
            {
                AttackState = OtherwordlyBoltAttackState.FlyIntoBackground;
                Time = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_FlyIntoBackground()
        {
            Projectile.velocity *= new Vector2(1.1f, 1.076f);
            Projectile.velocity.Y -= 0.26f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Prepare death effects before disappearing into the background.
            Projectile.scale = Utils.GetLerpValue(0f, -15f, Time - DisappearIntoBackgroundTime, true);
            if (Time >= DisappearIntoBackgroundTime)
                Projectile.Kill();
        }

        public void DoBehavior_AccelerateFromBelow()
        {
            // Accelerate.
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, 24f * Projectile.velocity.SafeNormalize(Vector2.UnitY), 0.0425f);

            // Aim in the direction that the bolt is accelerating.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public void DoBehavior_ArcAndAccelerate()
        {
            // Arc and accelerate.
            float maxSpeed = 25f;
            float acceleration = 1.032f;
            Projectile.velocity = Projectile.velocity.RotatedBy(ArcAngularVelocity);
            if (Projectile.velocity.Length() < maxSpeed)
                Projectile.velocity *= acceleration;

            // Aim in the direction that the bolt is accelerating.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool? CanDamage() => Projectile.Opacity >= 0.9f && (AttackState == OtherwordlyBoltAttackState.AccelerateFromBelow || AttackState == OtherwordlyBoltAttackState.ArcAndAccelerate);

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool ShouldUpdatePosition() => AttackState != OtherwordlyBoltAttackState.LockIntoPosition;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 scale = new(Projectile.scale, 1f);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < 24; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(texture, drawPosition + drawOffset, null, Color.MediumPurple with { A = 160 } * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, scale, 0, 0);
            }
            for (int i = 0; i < 15; i++)
            {
                float scaleFactor = 1f - i / 16f;
                Vector2 drawOffset = Projectile.velocity * i * -0.18f;
                Main.EntitySpriteDraw(texture, drawPosition + drawOffset, null, drawColor with { A = 160 } * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, scale * scaleFactor, 0, 0);
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor with { A = 130 }, Projectile.rotation, texture.Size() * 0.5f, scale, 0, 0);
            return false;
        }
    }
}
