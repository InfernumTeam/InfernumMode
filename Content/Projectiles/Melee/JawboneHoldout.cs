using CalamityMod;
using CalamityMod.Items.Weapons.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.Items.Weapons.Melee.Jawbone;

namespace InfernumMode.Content.Projectiles.Melee
{
    public class JawboneHoldout : ModProjectile
    {
        #region Fields/Properties
        public SwingType CurrentSwing
        {
            get => (SwingType)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        public ref float Timer => ref Projectile.ai[1];

        public Player Owner => Main.player[Projectile.owner];

        public float Lifetime;

        private float SwingWidth = 0.7f;

        public float SwingCompletion => MathHelper.Clamp(Timer / Lifetime, 0f, 1f);

        public Vector2 InitialDirection;

        public float SwingDirection => Projectile.ai[0] * Math.Sign(InitialDirection.X);

        #endregion

        #region Curve Stuff
        public static float SwingCompletionRatio => 0f;

        public static float RecoveryCompletionRatio => 0.85f;

        public static CalamityUtils.CurveSegment DownSwing => new(CalamityUtils.EasingType.PolyOut, SwingCompletionRatio, 0, 2.93f, 5);

        public static CalamityUtils.CurveSegment DownRecovery => new(CalamityUtils.EasingType.ExpIn, RecoveryCompletionRatio, DownSwing.EndingHeight, 0.97f, 3);

        public static CalamityUtils.CurveSegment UpSwing => new(CalamityUtils.EasingType.PolyOut, SwingCompletionRatio, 0, 2.93f, 5);

        public static CalamityUtils.CurveSegment UpRecovery => new(CalamityUtils.EasingType.PolyOut, RecoveryCompletionRatio, UpSwing.EndingHeight, 0.97f, 3);

        public CalamityUtils.CurveSegment thrust = new(CalamityUtils.EasingType.PolyInOut, 0.1f, 0.15f, 0.85f, 3);

        public CalamityUtils.CurveSegment hold = new(CalamityUtils.EasingType.Linear, 0.5f, 1f, 0.2f);

        public float GetSwingOffsetAngle(float completion)
        {
            return CalamityUtils.PiecewiseAnimation(completion, DownSwing, DownRecovery);
        }
        #endregion

        #region Methods
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Brimlash";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Jawbone");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 30;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 2000;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0)
                Initialize();

            // Die if the owner cannot use the item.
            if (Owner.dead || !Owner.active || Owner.noItems)
            {
                Projectile.Kill();
                return;
            }

            AdjustPlayerValues();
            Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X.DirectionalSign();
            Projectile.Center = Owner.Center;// - InitialDirection * 1f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Lerp(SwingWidth / 2f * SwingDirection, (0f - SwingWidth) / 2f * SwingDirection, GetSwingOffsetAngle(SwingCompletion));
            Timer++;
        }

        private void Initialize()
        {
            Lifetime = Projectile.timeLeft = CurrentSwing switch
            {
                SwingType.Upward => 25,
                _ => 25
            };
            InitialDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Projectile.rotation = InitialDirection.ToRotation();
            Projectile.localAI[0] = 1;
        }

        public void AdjustPlayerValues()
        {
            Owner.heldProj = Projectile.whoAmI;
            Owner.direction = Math.Sign(Projectile.velocity.X);
            Owner.itemRotation = Projectile.rotation;
            if (Owner.direction != 1)
            {
                Owner.itemRotation -= (float)Math.PI;
            }
            Owner.itemRotation = MathHelper.WrapAngle(Owner.itemRotation);
            float rotation = Projectile.rotation;
            //Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation - MathHelper.PiOver2);
        }

        #endregion

        #region Drawing
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            SpriteEffects spriteEffects = (Projectile.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
            // If we are facing the left, set extraAngle to PI, or 180 degrees to flip the rotation.
            float extraAngle = ((Projectile.direction < 0) ? ((float)Math.PI / 2f) : 0f);
            // Set the base draw angle to the rotation.
            float drawAngle = Projectile.rotation;
            // And set the final rotation, by adding PiOverTwo (90 degrees) and the extra angle (if not 0) to the drawAngle.
            float drawRotation = drawAngle + (float)Math.PI / 4f + extraAngle;
            Vector2 drawOffset = Projectile.Center + drawAngle.ToRotationVector2() - Main.screenPosition;
            Vector2 origin = new((Owner.direction < 0) ? texture.Width : 0f, texture.Height);

            Main.EntitySpriteDraw(texture, drawOffset, null, Projectile.GetAlpha(lightColor), drawRotation, origin, Projectile.scale, spriteEffects, 0);
            return false;
        }
        #endregion
    }
}
