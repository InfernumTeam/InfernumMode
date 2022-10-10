using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Crabulon
{
    public class SporeCloud : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore Gas");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
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
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] > 720f)
            {
                Projectile.localAI[0] += 10f;
                Projectile.damage = 0;
            }

            if (Projectile.localAI[0] > 255f)
            {
                Projectile.Kill();
                Projectile.localAI[0] = 255f;
            }

            Lighting.AddLight(Projectile.Center, Projectile.Opacity * 0.1f, Projectile.Opacity * 0.2f, Projectile.Opacity * 0.19f);

            Projectile.alpha = (int)(100.0 + Projectile.localAI[0] * 0.7);
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            Projectile.rotation += Projectile.direction * 0.002f;

            if (Projectile.velocity.Length() > 0.04f)
                Projectile.velocity *= 0.985f;
        }

        public override bool CanHitPlayer(Player target) => Projectile.ai[1] is <= 720f and >= 120f;

        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.ai[1] > 720f)
            {
                byte b2 = (byte)((26f - (Projectile.ai[1] - 720f)) * 10f);
                byte a2 = (byte)(Projectile.alpha * (b2 / 255f));
                return new Color(b2, b2, b2, a2);
            }
            return new Color(255, 255, 255, Projectile.alpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            switch ((int)Projectile.ai[0])
            {
                case 0:
                    break;
                case 1:
                    texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Crabulon/SporeCloud2").Value;
                    break;
                case 2:
                    texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Crabulon/SporeCloud3").Value;
                    break;
                default:
                    break;
            }
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);
            return false;
        }
    }
}
