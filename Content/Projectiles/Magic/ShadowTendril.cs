using CalamityMod;
using CalamityMod.DataStructures;
using InfernumMode.Content.Items.Weapons.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Magic
{
    public class ShadowTendril : ModProjectile, IAdditiveDrawer
    {
        public bool ReachedTarget
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public ref float Time => ref Projectile.ai[1];

        public const int Lifetime = 180;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LaserCircle";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Tendril");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = Lifetime;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.MaxUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 8;
            Projectile.hide = true;
            Projectile.scale = 0.4f;
            Projectile.Infernum().DrawAsShadow = true;
        }

        public override void AI()
        {
            // Arc towards the nearest target if it hasn't already been found.
            bool slowDown = ReachedTarget;
            if (!ReachedTarget)
            {
                NPC potentialTarget = Projectile.Center.ClosestNPCAt(EyeOfMadness.TargetingDistance);
                if (potentialTarget is not null)
                {
                    float angularVelocity = Utils.Remap(Projectile.Distance(potentialTarget.Center), 600f, 200f, Pi / 42f, Pi / 7f);
                    float angularOffset = Cos(Time / 7f + Projectile.identity) * Utils.GetLerpValue(150f, 450f, Projectile.Distance(potentialTarget.Center), true) * 0.9f;

                    Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(potentialTarget.Center) + angularOffset, angularVelocity);
                    if (Projectile.velocity.Length() <= 12f)
                        Projectile.velocity *= 1.016f;

                    if (Projectile.WithinRange(potentialTarget.Center, 50f))
                    {
                        ReachedTarget = true;
                        Projectile.netUpdate = true;
                    }
                }
                else
                    slowDown = true;
            }

            if (slowDown)
            {
                float angularOffset = CalamityUtils.AperiodicSin(Time / 15f + Projectile.identity) * 0.4f;
                Projectile.velocity = Projectile.velocity.RotatedBy(angularOffset) * 0.99f;
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 45f, Projectile.timeLeft, true);

            Time++;
        }

        public override Color? GetAlpha(Color lightColor) => lightColor * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Color baseColor = Color.Lerp(Color.White, Color.DarkBlue, (Projectile.identity * 0.13f + i * 0.027f) % 1f);
                float scale = Utils.Remap(i, 8f, 0f, 1f, 0.4f) * Projectile.scale;
                Vector2 drawPosition = Projectile.Size * 0.5f + Projectile.oldPos[i] - Main.screenPosition;
                Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(baseColor), Projectile.oldRot[i], texture.Size() * 0.5f, scale, 0, 0f);
            }
            return false;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch) { }
    }
}
