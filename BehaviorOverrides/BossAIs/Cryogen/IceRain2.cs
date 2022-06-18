using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
            projectile.width = 19;
            projectile.height = 19;
            projectile.scale = 1.3f;
            projectile.hostile = true;
            projectile.penetrate = -1;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(projectile.localAI[0]);

        public override void ReceiveExtraAI(BinaryReader reader) => projectile.localAI[0] = reader.ReadSingle();

        public override void AI()
        {
            Lighting.AddLight((int)(projectile.Center.X / 16f), (int)(projectile.Center.Y / 16f), 0f, 0.38f, 0.38f);

            if (projectile.ai[0] != 2f)
                projectile.aiStyle = 1;

            if (projectile.ai[0] == 0f)
                projectile.velocity.Y += 0.36f;
            else if (projectile.ai[0] == 2f)
            {
                projectile.velocity.Y += 0.2f;
                projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

                if (projectile.velocity.Y > 6f)
                    projectile.velocity.Y = 6f;
            }

            if (projectile.localAI[0] == 0f)
            {
                projectile.scale += 0.01f;
                projectile.alpha -= 20;
                if (projectile.alpha <= 0)
                {
                    projectile.localAI[0] = 1f;
                    projectile.alpha = 0;
                }
            }
            else
            {
                projectile.scale -= 0.01f;
                projectile.alpha += 20;
                if (projectile.alpha >= 145)
                {
                    projectile.localAI[0] = 0f;
                    projectile.alpha = 145;
                }
            }
        }

        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 60) * projectile.Opacity;

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item27, projectile.Center);
            for (int i = 0; i < 3; i++)
            {
                Dust snow = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 76, 0f, 0f, 0, default, 1f);
                snow.noGravity = true;
                snow.noLight = true;
                snow.scale = 0.7f;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Frostburn, 60, true);
    }
}
