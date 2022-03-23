using CalamityMod;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class BirbThunderAuraFlare : ModProjectile
    {
        public ref float Time => ref projectile.localAI[0];
        public ref float PulsationFactor => ref projectile.localAI[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Draconic Aura Flare");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 1200;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(PulsationFactor);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Time = reader.ReadSingle();
            PulsationFactor = reader.ReadSingle();
        }

        public override void AI()
        {
            if (projectile.ai[1] > 0f)
            {
                int targetIndex = (int)projectile.ai[1] - 1;
                if (targetIndex < 255)
                {
                    Time++;
                    if (Time > 10f)
                    {
                        // Dust pulse effect
                        PulsationFactor = (float)Math.Abs(Math.Cos(MathHelper.ToRadians(Time * 2f)));
                        EmitDust();
                    }

                    projectile.velocity = projectile.SafeDirectionTo(Main.player[targetIndex].Center) * (Time / 8f + 7f);
                    if (projectile.WithinRange(Main.player[targetIndex].Center, 32f))
                        projectile.Kill();
                }
            }
        }

        public void EmitDust()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Dust redLightning = Dust.NewDustPerfect(projectile.Center, 267);
                redLightning.velocity = Main.rand.NextVector2CircularEdge(2f, 1.6f).RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(0.6f, 1f);
                redLightning.velocity += projectile.velocity;
                redLightning.color = Color.Red;
                redLightning.noGravity = true;
                redLightning.scale = Main.rand.NextFloat(0.85f, 1.25f);
            }
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.DD2_BetsyFireballImpact, (int)projectile.position.X, (int)projectile.position.Y);

            if (projectile.owner != Main.myPlayer)
                return;

            int type = ModContent.ProjectileType<BirbAura>();
            if (CalamityUtils.CountProjectiles(type) >= 2)
            {
                Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightningCloud>(), 0, 0f);
                return;
            }
            float x = Utils.Clamp((int)(projectile.Center.Y / 16f), 10, Main.maxTilesX - 110) * 16;
            float y = Utils.Clamp((int)(projectile.Center.X / 16f), 10, Main.maxTilesX - 110) * 16 + 900;
            Vector2 laserVelocity;

            x += 1000f;
            if ((int)(x / 16f) > Main.maxTilesX - 10)
                x = (Main.maxTilesX - 10) * 16f;
            laserVelocity = new Vector2(x, 160f) - new Vector2(x, y);
            laserVelocity.Normalize();
            int auraLaser = Utilities.NewProjectileBetter(x, y, 0f, laserVelocity.Y, type, 495, 0f, Main.myPlayer, x, y);
            Main.projectile[auraLaser].timeLeft = 600;
            Main.projectile[auraLaser].netUpdate = true;

            x -= 2000f;
            if ((int)(x / 16f) < 10)
                x = 160f;
            laserVelocity = new Vector2(x, 160f) - new Vector2(x, y);
            laserVelocity.Normalize();
            auraLaser = Utilities.NewProjectileBetter(x, y, 0f, laserVelocity.Y, type, 495, 0f, Main.myPlayer, x, y);
            Main.projectile[auraLaser].timeLeft = 600;
            Main.projectile[auraLaser].netUpdate = true;
        }
    }
}
