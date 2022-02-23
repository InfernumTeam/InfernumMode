using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class PsychicEnergyField : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psychic Energy Field");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.timeLeft = 75;
            projectile.penetrate = -1;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(75f, 50f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (projectile.timeLeft == 45f)
            {
                // Play a bolt sound and release the psionic blast.
                Main.PlaySound(SoundID.Item75, projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player closestTarget = Main.player[Player.FindClosest(projectile.Center, 1, 1)];

                    for (int i = 0; i < 3; i++)
                    {
                        float shootOffsetAngle = MathHelper.Lerp(-0.4f, 0.4f, i / 2f);
                        Vector2 blastShootVelocity = projectile.SafeDirectionTo(closestTarget.Center).RotatedBy(shootOffsetAngle) * 7f;
                        Projectile.NewProjectile(projectile.Center, blastShootVelocity, ModContent.ProjectileType<PsionicRay>(), projectile.damage, 0f);
                    }
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.EnterShaderRegion();
            Texture2D noiseTexture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition2 = projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseOpacity(projectile.Opacity);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseColor(Color.Cyan);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseSecondaryColor(Color.Lerp(Color.Purple, Color.Black, 0.25f));
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].Apply();

            spriteBatch.Draw(noiseTexture, drawPosition2, null, Color.White, 0f, origin, 0.4f, SpriteEffects.None, 0f);
            spriteBatch.ExitShaderRegion();
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }

        public override bool CanDamage() => false;
    }
}
