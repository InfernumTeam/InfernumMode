using CalamityMod;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Providence;
using InfernumMode.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Projectiles
{
    public class GuardiansSummonerProjectile : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 300;

        public override string Texture => "CalamityMod/Items/SummonItems/ProfanedShard";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 1f;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Play a rumble sound.
            if (Time == 75f)
                SoundEngine.PlaySound(InfernumSoundRegistry.LeviathanRumbleSound, Projectile.Center);
            if (Time >= 75f)
                Main.LocalPlayer.Infernum().CreateALotOfHolyCinders = true;

            if (Time >= 210f)
            {
                // Create screen shake effects.
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.GetLerpValue(2300f, 1300f, Main.LocalPlayer.Distance(Projectile.Center), true) * 8f;
            }

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.GetLerpValue(2300f, 1300f, Main.LocalPlayer.Distance(Projectile.Center), true) * 16f;

            // Make the crystal shatter.
            SoundEngine.PlaySound(Providence.DeathSound, Projectile.Center);

            // Create an explosion and summon the Guardian Commander.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CalamityUtils.SpawnBossBetter(Projectile.Center - Vector2.UnitY * 250f, ModContent.NPCType<ProfanedGuardianCommander>());
                
                int explosion = Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HolySunExplosion>(), 0, 0f);
                if (Main.projectile.IndexInRange(explosion))
                    Main.projectile[explosion].ModProjectile<HolySunExplosion>().MaxRadius = 600f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            float glowInterpolant = Utils.GetLerpValue(60f, 105f, Time, true);
            if (glowInterpolant > 0f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Color color = Color.Lerp(Color.White, Color.Yellow with { A = 0 }, glowInterpolant) * Projectile.Opacity * glowInterpolant;
                    Vector2 drawOffset = (Time * MathHelper.TwoPi / 50f + MathHelper.TwoPi * i / 8f).ToRotationVector2() * glowInterpolant * 7.5f;
                    Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
                }
            }
            Main.spriteBatch.Draw(texture, drawPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);

            return false;
        }
    }
}
