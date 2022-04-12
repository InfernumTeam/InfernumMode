using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cryogen
{
    public class IceRain2 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ice Rain");
        }

        public override void SetDefaults()
        {
            Projectile.width = 19;
            Projectile.height = 19;
            Projectile.scale = 1.3f;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.localAI[0]);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.localAI[0] = reader.ReadSingle();

        public override void AI()
        {
            Lighting.AddLight((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f), 0f, 0.38f, 0.38f);

            if (Projectile.ai[0] != 2f)
                Projectile.aiStyle = 1;

            if (Projectile.ai[0] == 0f)
                Projectile.velocity.Y += 0.36f;
            else if (Projectile.ai[0] == 2f)
            {
                Projectile.velocity.Y += 0.2f;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                if (Projectile.velocity.Y > 6f)
                    Projectile.velocity.Y = 6f;
            }

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale += 0.01f;
                Projectile.alpha -= 20;
                if (Projectile.alpha <= 0)
                {
                    Projectile.localAI[0] = 1f;
                    Projectile.alpha = 0;
                }
            }
            else
            {
                Projectile.scale -= 0.01f;
                Projectile.alpha += 20;
                if (Projectile.alpha >= 145)
                {
                    Projectile.localAI[0] = 0f;
                    Projectile.alpha = 145;
                }
            }
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 60) * Projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            for (int i = 0; i < 3; i++)
            {
                Dust snow = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 76, 0f, 0f, 0, default, 1f);
                snow.noGravity = true;
                snow.noLight = true;
                snow.scale = 0.7f;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Frostburn, 60, true);
    }
}
