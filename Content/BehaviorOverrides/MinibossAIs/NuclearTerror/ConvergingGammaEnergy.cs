using System.IO;
using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using NuclearTerrorNPC = CalamityMod.NPCs.AcidRain.NuclearTerror;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.NuclearTerror
{
    public class ConvergingGammaEnergy : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Gamma Cinder");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.Calamity().DealsDefenseDamage = true;
            
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.45f, 0.35f, 0f);

            int nuclearTerrorIndex = NPC.FindFirstNPC(ModContent.NPCType<NuclearTerrorNPC>());
            if (nuclearTerrorIndex != -1 && Projectile.WithinRange(Main.npc[nuclearTerrorIndex].Center, Projectile.velocity.Length() * 1.96f + 28f) && Projectile.ai[1] == 1f)
                Projectile.Kill();

            // Fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.04f, 0f, 1f);

            // Release acid particles.
            Color acidColor = Main.rand.NextBool() ? Color.Yellow : Color.Lime;
            CloudParticle acidCloud = new(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f), acidColor * 0.6f, Color.DarkGray * 0.45f, 27, Main.rand.NextFloat(1.1f, 1.32f))
            {
                Rotation = Main.rand.NextFloat(TwoPi)
            };
            GeneralParticleHandler.SpawnParticle(acidCloud);

            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(NuclearTerrorNPC.HitSound with { Pitch = 0.4f }, Projectile.Center);

            for (int i = 0; i < 6; i++)
            {
                Color acidColor = Main.rand.NextBool() ? Color.Yellow : Color.Lime;
                CloudParticle acidCloud = new(Projectile.Center, (TwoPi * i / 6f).ToRotationVector2() * 2f + Main.rand.NextVector2Circular(0.3f, 0.3f), acidColor, Color.DarkGray, 27, Main.rand.NextFloat(1.1f, 1.32f))
                {
                    Rotation = Main.rand.NextFloat(TwoPi)
                };
                GeneralParticleHandler.SpawnParticle(acidCloud);
            }
        }
    }
}
