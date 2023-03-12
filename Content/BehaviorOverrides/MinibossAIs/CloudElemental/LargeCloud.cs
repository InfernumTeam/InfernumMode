using InfernumMode.Assets.Effects;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.CloudElemental
{
    public class LargeCloud : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy CloudDrawer { get; private set; } = null;

        public ref float Timer => ref Projectile.ai[0];

        public ref float LightPower => ref Projectile.localAI[0];

        public ref float ParentIndex => ref Projectile.ai[1];

        public float CloudRadius = 1800;

        public float HailDropRate = 5;

        public SlotId WindSlot;

        public override void SetDefaults()
        {
            Projectile.width = 934;
            Projectile.height = 287;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0;
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Calculate light power. This checks below the position of the fog to check if this fog is underground.
            // Without this, it may render over the fullblack that the game renders for obscured tiles.
            float lightPowerBelow = Lighting.GetColor((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16 + 6).ToVector3().Length() / (float)Math.Sqrt(3D);
            LightPower = MathHelper.Lerp(LightPower, lightPowerBelow, 0.15f);

            // Fade in
            if (Projectile.Opacity < 1 && Projectile.timeLeft > 60)
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.03f, 0f, 1f);
            else if (Projectile.timeLeft <= 30)
                Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity - 0.03f, 0f, 1f);

            NPC parent = Main.npc[(int)ParentIndex];
            float parentLifeRatio = (float)parent.life / parent.lifeMax;
            if (parentLifeRatio <= CloudElementalBehaviorOverride.PhaseTwoLifeRatio)
                HailDropRate = 3;

            Player player = Main.player[parent.target];

            // Constantly stick above the player.
            if (player.active && !player.dead && player != null)
                Projectile.Center = player.Center + new Vector2(0, -450);

            // Periodically rain down hail.
            if (Timer % HailDropRate == HailDropRate - 1 && Main.netMode != NetmodeID.MultiplayerClient && Projectile.Opacity == 1)
            {
                Vector2 position = Projectile.Center + new Vector2(Main.rand.NextFloat(-CloudRadius * 0.9f, CloudRadius * 0.9f), Main.rand.NextFloat(-2, 2) - 20);
                float hailSpeed = Main.rand.NextFloat(5, 6);
                Utilities.NewProjectileBetter(position, (Vector2.UnitY * hailSpeed).RotatedBy(Main.rand.NextFloat(-0.2f, 0.2f)), ModContent.ProjectileType<LargeHail>(), 120, 0f, Main.myPlayer, (float)LargeHail.HailType.Fall);
            }

            // Sound stuff
            if (Timer is 0)
                WindSlot = SoundEngine.PlaySound(InfernumSoundRegistry.CloudElementalWindSound with { Volume = 0.5f, PlayOnlyIfFocused = true }, player.Center);

            if (SoundEngine.TryGetActiveSound(WindSlot, out var s))
            {
                if (s.Position != player.Center && s.IsPlaying)
                    s.Position = player.Center;
            }

            // Spawn snow particles
            player.CreateCinderParticles(1f, new SnowflakeCinder());

            Timer++;
        }

        public override bool? CanDamage() => false;
        
        public float WidthFunction(float _) => Projectile.width * Projectile.scale * 0.3f;

        public Color ColorFunction(float _)
        {
            // Use the lightpower to set the opacity of the color.
            float opacity = Utils.GetLerpValue(0f, 0.08f, LightPower, true) * Projectile.Opacity;
            Color color = Color.Lerp(Color.Gray, Color.White, 0.5f) * opacity * Projectile.Opacity;
            color.A = 0;
            return color;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            CloudDrawer ??= new(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.CloudVertexShader);
            Asset<Texture2D> texture = TextureAssets.Projectile[Projectile.type];

            // Set the draw points for the trail using the radius.
            Vector2 startPos = new(Projectile.Center.X - CloudRadius, Projectile.Center.Y);
            Vector2 endPos = new(Projectile.Center.X + CloudRadius, Projectile.Center.Y);
            Vector2[] baseDrawPoints = new Vector2[8];
            for (int i = 0; i < baseDrawPoints.Length; i++)
                baseDrawPoints[i] = Vector2.Lerp(startPos, endPos, i / (float)(baseDrawPoints.Length - 1f));

            // Set the shader fademap.
            InfernumEffectsRegistry.CloudVertexShader.SetShaderTexture(texture);
            CloudDrawer.DrawPixelated(baseDrawPoints, -Main.screenPosition, 20);
        }
    }
}
