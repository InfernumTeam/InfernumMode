using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class SporeGas : ModProjectile
    {
        public ref float Time => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore Gas");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            Time++;

            Projectile.Opacity = Utils.GetLerpValue(0f, 120f, Time, true) * (1f - Projectile.localAI[0] / 255f);
            if (Time > 1800f)
            {
                Projectile.localAI[0] += 10f;
                Projectile.damage = 0;
            }

            if (Projectile.localAI[0] > 255f)
            {
                Projectile.Kill();
                Projectile.localAI[0] = 255f;
            }

            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.16f / 255f, (255 - Projectile.alpha) * 0.2f / 255f, (255 - Projectile.alpha) * 0.04f / 255f);

            Projectile.rotation += Projectile.velocity.X * 0.02f;
            Projectile.rotation += Projectile.direction * 0.002f;

            if (Projectile.velocity.Length() > 0.1f)
                Projectile.velocity *= 0.98f;
        }

        public override bool CanHitPlayer(Player target) => Time is <= 1800f and > 120f;

        public override Color? GetAlpha(Color lightColor)
        {
            if (Time > 1800f)
            {
                byte b2 = (byte)((26f - (Time - 1800f)) * 10f);
                byte a2 = (byte)(Projectile.alpha * (b2 / 255f));
                return new Color(b2, b2, b2, a2) * Projectile.Opacity;
            }
            return new Color(255, 255, 255, Projectile.alpha) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Changes the texture of the projectile
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            switch ((int)Projectile.ai[0])
            {
                case 0:
                    break;
                case 1:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/SporeGasPlantera2").Value;
                    break;
                case 2:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/SporeGasPlantera3").Value;
                    break;
                default:
                    break;
            }
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1, texture);
            return false;
        }
    }
}
