using CalamityMod;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Providence;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles
{
    public class GuardiansSummonerProjectile : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public int Lifetime => 390;

        public Vector2 MainPosition => GuardianComboAttackManager.CrystalPosition + new Vector2(500f, 150f);

        public static Player Player => Main.LocalPlayer;

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
            if (Time == 0)
                Player.Infernum_Camera().ScreenFocusHoldInPlaceTime = Lifetime;

            // Play a rumble sound.
            if (Time == 75f)
                SoundEngine.PlaySound(InfernumSoundRegistry.LeviathanRumbleSound, MainPosition);
            if (Time >= 75f)
                Player.Infernum_TempleCinder().CreateALotOfHolyCinders = true;

            if (Player.WithinRange(MainPosition, 10000))
            {
                Player.Infernum_Camera().ScreenFocusPosition = MainPosition;
                Player.Infernum_Camera().ScreenFocusInterpolant = MathHelper.Clamp(Player.Infernum_Camera().ScreenFocusInterpolant + 0.05f, 0f, 1f);

                // Disable input and UI during the animation.
                Main.blockInput = true;
                Main.hideUI = true;
            }

            if (Time >= 210f)
            {
                // Create screen shake effects.
                Player.Infernum_Camera().CurrentScreenShakePower = 6;
            }

            if (Time == 300)
            {
                Player.Infernum_Camera().CurrentScreenShakePower = Utils.GetLerpValue(2300f, 1300f, Main.LocalPlayer.Distance(Projectile.Center), true) * 16f;

                // Make the crystal shatter.
                SoundEngine.PlaySound(Providence.HurtSound, MainPosition);

                // Create an explosion and summon the Guardian Commander.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    CalamityUtils.SpawnBossBetter(GuardianComboAttackManager.CommanderStartingHoverPosition, ModContent.NPCType<ProfanedGuardianCommander>());

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<HolySunExplosion>().MaxRadius = 600f;
                    });
                    Utilities.NewProjectileBetter(MainPosition, Vector2.Zero, ModContent.ProjectileType<HolySunExplosion>(), 0, 0f);
                }
            }
            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.blockInput = false;
            Main.hideUI = false;
        }

        public override bool PreDraw(ref Color lightColor)
        {

            return false;
        }
    }
}
