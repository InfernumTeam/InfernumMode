using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using TwinsRedLightning = InfernumMode.Content.BehaviorOverrides.BossAIs.Twins.RedLightning;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class LightningCloud2 : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lightning");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 64;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(PlasmaGrenade.ExplosionSound, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }
            for (int i = 0; i < 16; i++)
            {
                Dust redLightning = Dust.NewDustPerfect(Projectile.Center, 60, Main.rand.NextVector2Circular(3f, 3f));
                redLightning.velocity *= Main.rand.NextFloat(1f, 1.9f);
                redLightning.scale *= Main.rand.NextFloat(1.85f, 2.25f);
                redLightning.color = Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0.5f, 1f));
                redLightning.fadeIn = 1f;
                redLightning.noGravity = true;
            }
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color lineColor = Color.Red;
            float lineWidth = Lerp(0.25f, 3f, Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 22f, Time, true));
            Main.spriteBatch.DrawLineBetter(Projectile.Center - Vector2.UnitY * 1900f, Projectile.Center + Vector2.UnitY * 1900f, lineColor, lineWidth);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(HolyBlast.ImpactSound, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < Main.rand.Next(1, 3 + 1); i++)
            {
                Vector2 spawnPosition = Projectile.Center + Vector2.UnitX * Main.rand.NextFloat(-7f, 7f);
                spawnPosition.Y -= 2800f;

                Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * 15f, ModContent.ProjectileType<TwinsRedLightning>(), DragonfollyBehaviorOverride.RedLightningDamage, 0f, -1, PiOver2, Main.rand.Next(100));
            }
        }
    }
}
