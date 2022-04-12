using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class PsychicEnergyField : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Psychic Energy Field");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.timeLeft = 75;
            Projectile.penetrate = -1;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(75f, 50f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (Projectile.timeLeft == 45f)
            {
                // Play a bolt sound and release the psionic blast.
                SoundEngine.PlaySound(SoundID.Item75, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Player closestTarget = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

                    for (int i = 0; i < 3; i++)
                    {
                        float shootOffsetAngle = MathHelper.Lerp(-0.4f, 0.4f, i / 2f);
                        Vector2 blastShootVelocity = Projectile.SafeDirectionTo(closestTarget.Center).RotatedBy(shootOffsetAngle) * 7f;
                        Projectile.NewProjectile(new InfernumSource(), Projectile.Center, blastShootVelocity, ModContent.ProjectileType<PsionicRay>(), Projectile.damage, 0f);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();
            Texture2D noiseTexture = Utilities.ProjTexture(Projectile.type);
            Vector2 drawPosition2 = Projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseOpacity(Projectile.Opacity);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseColor(Color.Cyan);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseSecondaryColor(Color.Lerp(Color.Purple, Color.Black, 0.25f));
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].Apply();

            Main.spriteBatch.Draw(noiseTexture, drawPosition2, null, Color.White, 0f, origin, 0.4f, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }

        public override bool? CanDamage() => false ? null : false;
    }
}
