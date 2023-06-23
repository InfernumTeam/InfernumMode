using CalamityMod.DataStructures;
using CalamityMod.Sounds;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DoG
{
    public class RealityBreakPortalLaserWall : ModProjectile, IAdditiveDrawer
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Portal");

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Time++;

            // Release the laser burst a second after spawning.
            if (Time == 60f)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                    float shootInterpolant = Utils.GetLerpValue(600f, 1450f, Projectile.Distance(target.Center), true);

                    int laserCount = (int)Lerp(5f, 12f, shootInterpolant);
                    float shootSpeed = Lerp(15f, 25f, shootInterpolant);
                    for (int i = 0; i < laserCount; i++)
                    {
                        Vector2 shootVelocity = Projectile.SafeDirectionTo(target.Center).RotatedBy(Lerp(-0.6f, 0.6f, i / (float)(laserCount - 1f))) * shootSpeed;

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                        {
                            laser.MaxUpdates = 2;
                        });
                        Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<DoGDeathInfernum>(), DoGPhase1HeadBehaviorOverride.DeathLaserDamage, 0f, Projectile.owner);
                    }
                }
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 50f, Time, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            Projectile.rotation += Projectile.Opacity * 0.15f;
        }

        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D portalTexture = TextureAssets.Projectile[Projectile.type].Value;
            Texture2D lightTexture = InfernumTextureRegistry.LaserCircle.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = portalTexture.Size() * 0.5f;
            Color baseColor = Color.White;

            // Black portal.
            Color portalColor = baseColor * Projectile.Opacity;

            for (int i = 0; i < 2; i++)
            {
                spriteBatch.Draw(portalTexture, drawPosition, null, portalColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
                spriteBatch.Draw(portalTexture, drawPosition, null, portalColor, -Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }

            // Point of light.
            spriteBatch.Draw(lightTexture, drawPosition, null, baseColor * 0.8f, -Projectile.rotation, lightTexture.Size() * 0.5f, Projectile.scale * Projectile.Opacity * 0.85f, 0, 0f);
        }
    }
}
