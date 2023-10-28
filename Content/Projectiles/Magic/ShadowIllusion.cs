using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Content.Items.Weapons.Magic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Magic
{
    public class ShadowIllusion : ModProjectile
    {
        public bool FadingAway => Projectile.penetrate <= 20;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/LargeStar";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Lens Flare");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 25;
            Projectile.timeLeft = 360;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
            Projectile.Infernum().DrawAsShadow = true;
        }

        public override void AI()
        {
            // Release shadow particles.
            EmitShadowParticles();

            // Fade away if necessary.
            if (FadingAway)
            {
                Projectile.velocity *= 0.9f;
                Projectile.Opacity = Clamp(Projectile.Opacity - 0.04f, 0f, 1f);
                if (Projectile.Opacity <= 0f)
                    Projectile.Kill();

                return;
            }

            // Fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.05f, 0f, 1f);

            // Chase down targets.
            NPC potentialTarget = Projectile.Center.ClosestNPCAt(IllusionersReverie.TargetingDistance);
            if (potentialTarget is not null)
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, potentialTarget.Center, 0.02f);
                Projectile.velocity = (Projectile.velocity * 39f + Projectile.SafeDirectionTo(potentialTarget.Center) * 29f) / 40f;
                Projectile.rotation = Projectile.velocity.X * 0.02f;

                // Teleport near the target if very close to them, to create chained hits.
                if (Projectile.WithinRange(potentialTarget.Center, 26f))
                {
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy, Projectile.Center);
                    Projectile.Center = potentialTarget.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(135f, 166f);
                    Projectile.velocity = Projectile.SafeDirectionTo(potentialTarget.Center) * 9f;
                    Projectile.Opacity = 0.65f;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                float angularVelocity = Lerp(-0.011f, 0.011f, Projectile.identity / 13f % 1f);
                Projectile.velocity = Projectile.velocity.RotatedBy(angularVelocity) * 1.032f;
            }

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 8;
        }

        public void EmitShadowParticles()
        {
            float particleReleaseRate = Lerp(1f, 0.3f, Projectile.Opacity);
            for (int i = 0; i < 2; i++)
            {
                if (Main.rand.NextFloat() > particleReleaseRate)
                    continue;

                Color shadowMistColor = Color.Lerp(Color.Purple, Color.Red, Main.rand.NextFloat(0.72f));
                Vector2 particleSpawnCenter = Projectile.Center + new Vector2(Main.rand.NextFloatDirection() * 15f, Main.rand.NextFloatDirection() * 26f);
                Dust shadow = Dust.NewDustPerfect(particleSpawnCenter, 261);
                shadow.color = shadowMistColor;
                shadow.velocity = -Vector2.UnitY.RotatedByRandom(0.19f) * Main.rand.NextFloat(2f, 5.6f);
                shadow.scale = 0.8f;
                shadow.noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Color shadowMistColor = Color.Lerp(Color.Purple, Color.Red, Main.rand.NextFloat(0.72f));
                if (Main.rand.NextBool())
                    shadowMistColor = Color.Lerp(shadowMistColor, Color.Blue, 0.55f);

                Vector2 particleSpawnCenter = Projectile.Center + new Vector2(Main.rand.NextFloatDirection() * 15f, Main.rand.NextFloatDirection() * 26f);
                HeavySmokeParticle shadowMist = new(particleSpawnCenter, Main.rand.NextVector2Circular(5f, 5f), shadowMistColor, 45, Projectile.Opacity * 0.6f, Projectile.Opacity * 0.467f, Main.rand.NextFloat(-0.024f, 0.024f), true);
                GeneralParticleHandler.SpawnParticle(shadowMist);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => !FadingAway;

        public override bool PreDraw(ref Color lightColor)
        {
            int owner = Projectile.owner;
            Player other = Main.player[owner];

            Main.playerVisualClone[owner] ??= new();

            Player player = Main.playerVisualClone[owner];
            player.CopyVisuals(other);
            player.isFirstFractalAfterImage = true;
            player.firstFractalAfterImageOpacity = Projectile.Opacity;
            player.ResetEffects();
            player.ResetVisibleAccessories();
            player.UpdateDyes();
            player.DisplayDollUpdate();
            player.UpdateSocialShadow();
            player.Center = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * 42f;
            player.direction = (Projectile.velocity.X > 0f).ToDirectionInt();
            player.velocity.Y = 0.01f;
            player.wingFrame = (Projectile.frame + Projectile.identity) % 4;
            player.PlayerFrame();
            player.socialIgnoreLight = true;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, player, player.position, 0f, player.fullRotationOrigin, 0f, 1f);
            return false;
        }
    }
}
