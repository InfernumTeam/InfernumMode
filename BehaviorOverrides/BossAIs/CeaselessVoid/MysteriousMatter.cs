using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class MysteriousMatter : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Mysterious Matter");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 22f, Time, true) * Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);

            // Fire a bunch of ceasless energy at the nearest target once at the apex of the projectile's lifetime.
            if (Time == 30f)
            {
                Player closestTarget = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                SoundEngine.PlaySound(SoundID.Item28, closestTarget.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int damage = CalamityGlobalNPC.DoGHead >= 0 ? 425 : 250;
                    for (int i = 0; i < 3; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.63f, 0.63f, i / 2f);
                        Vector2 shootVelocity = Projectile.SafeDirectionTo(closestTarget.Center).RotatedByRandom(offsetAngle) * 4f;
                        Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<CeaselessEnergy>(), damage, 0f);
                    }
                }
            }

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float alpha = Utils.GetLerpValue(0f, 30f, Time, true);
            return new Color(1f, 1f, 1f, alpha) * Projectile.Opacity * MathHelper.Lerp(0.6f, 1f, alpha);
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => Projectile.Opacity >= 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float scale = Projectile.scale;
            spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
