using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class BirbThunderAuraFlare : ModProjectile
    {
        public ref float Time => ref Projectile.localAI[0];
        public ref float PulsationFactor => ref Projectile.localAI[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Draconic Aura Flare");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1200;
            
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
            if (Projectile.ai[1] > 0f)
            {
                int targetIndex = (int)Projectile.ai[1] - 1;
                if (targetIndex < 255)
                {
                    Time++;
                    if (Time > 10f)
                    {
                        // Dust pulse effect.
                        PulsationFactor = Math.Abs(Cos(ToRadians(Time * 2f)));
                        EmitDust();
                    }

                    Projectile.velocity = Projectile.SafeDirectionTo(Main.player[targetIndex].Center) * (Time / 8f + 7f);
                    if (Projectile.WithinRange(Main.player[targetIndex].Center, 32f))
                        Projectile.Kill();
                }
            }
        }

        public void EmitDust()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Dust redLightning = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2);
                redLightning.velocity = Main.rand.NextVector2CircularEdge(2f, 1.6f).RotatedByRandom(TwoPi) * Main.rand.NextFloat(0.6f, 1f);
                redLightning.velocity += Projectile.velocity;
                redLightning.color = Color.Red;
                redLightning.noGravity = true;
                redLightning.scale = Main.rand.NextFloat(0.85f, 1.25f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact, Projectile.Center);

            if (Projectile.owner != Main.myPlayer)
                return;

            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightningCloud>(), 0, 0f);
        }
    }
}
