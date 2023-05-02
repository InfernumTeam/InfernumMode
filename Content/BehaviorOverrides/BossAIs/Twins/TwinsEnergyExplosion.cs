using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class TwinsEnergyExplosion : ModProjectile
    {
        public ref float OwnerType => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public const int Lifetime = 80;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Boom");
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.scale = 0.001f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(PlasmaGrenade.ExplosionSound, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }
            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = MathF.Sin(MathHelper.Pi * Projectile.timeLeft / Lifetime) * 14f + 2f;

            Radius = MathHelper.Lerp(Radius, 3516f, 0.15f);
            Projectile.scale = MathHelper.Lerp(1.2f, 5f, Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true));
            Projectile.ExpandHitboxBy((int)(Radius * Projectile.scale), (int)(Radius * Projectile.scale));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();

            float pulseCompletionRatio = Utils.GetLerpValue(Lifetime, 0f, Projectile.timeLeft, true);
            Vector2 scale = new(1.5f, 1f);
            DrawData drawData = new(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value,
                Projectile.Center - Main.screenPosition + Projectile.Size * scale * 0.5f,
                new Rectangle(0, 0, Projectile.width, Projectile.height),
                new Color(new Vector4(1f - MathF.Sqrt(pulseCompletionRatio))) * 0.7f * Projectile.Opacity,
                Projectile.rotation,
                Projectile.Size,
                scale,
                SpriteEffects.None, 0);

            Color pulseColor = OwnerType == NPCID.Spazmatism ? Color.LimeGreen : Color.Red;
            GameShaders.Misc["ForceField"].UseColor(pulseColor);
            GameShaders.Misc["ForceField"].Apply(drawData);
            drawData.Draw(Main.spriteBatch);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
