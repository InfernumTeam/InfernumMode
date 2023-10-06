using CalamityMod;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using LeviathanNPC = CalamityMod.NPCs.Leviathan.Leviathan;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Leviathan
{
    public class LeviathanSpawner : ModProjectile
    {
        internal ref float Time => ref Projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Water");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 450;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = Sin(Projectile.timeLeft / 120f * Pi) * 4f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            // Play a rumble sound.
            if (Projectile.timeLeft == 340)
                SoundEngine.PlaySound(InfernumSoundRegistry.LeviathanRumbleSound with { Volume = 1.5f }, Projectile.Center);

            // Shake the screen.
            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = Pow(Utils.GetLerpValue(180f, 290f, Time, true), 0.3f) * 20f;
            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower += CalamityUtils.Convert01To010(Pow(Utils.GetLerpValue(300f, 440f, Time, true), 0.5f)) * 35f;

            if (Projectile.timeLeft == 45)
            {
                SoundEngine.PlaySound(LeviathanNPC.RoarChargeSound, Projectile.Center);
                if (Main.netMode != NetmodeID.Server)
                {
                    WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                    Vector2 ripplePos = Projectile.Center;

                    for (int i = 0; i < 7; i++)
                        ripple.QueueRipple(ripplePos, Color.White, Vector2.One * 4000f, RippleShape.Square, Main.rand.NextFloat(TwoPi));
                }
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return;

                for (int i = -1; i <= 1; i += 2)
                {
                    int wave = Utilities.NewProjectileBetter(Projectile.Center, Vector2.UnitX * 15f * i, ModContent.ProjectileType<LeviathanSpawnWave>(), 0, 0f);
                    Main.projectile[wave].Bottom = Projectile.Center + Vector2.UnitY * 700f;
                }

                int leviathan = NPC.NewNPC(Projectile.GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Center.Y, ModContent.NPCType<LeviathanNPC>());
                if (Main.npc.IndexInRange(leviathan))
                    Main.npc[leviathan].velocity = Vector2.UnitY * -7f;
            }

            Time++;
            CreateVisuals();
        }

        internal void CreateVisuals()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            WorldUtils.Find((Projectile.Center - Vector2.UnitY * 1200f).ToTileCoordinates(), Searches.Chain(new Searches.Down(150), new CustomTileConditions.IsWater()), out Point waterTop);

            // Making bubbling water as an indicator.
            if (Time % 4f == 3f && Time > 90f)
            {
                float xArea = Lerp(400f, 1150f, Time / 300f);
                Vector2 dustSpawnPosition = waterTop.ToWorldCoordinates() + Vector2.UnitY * 25f;
                dustSpawnPosition.X += Main.rand.NextFloatDirection() * xArea * 0.35f;
                Dust bubble = Dust.NewDustPerfect(dustSpawnPosition, 267, Vector2.UnitY * -12f);
                bubble.noGravity = true;
                bubble.scale = 1.9f;
                bubble.color = Color.CornflowerBlue;

                for (float x = -xArea; x <= xArea; x += 110f)
                {
                    // As well as liquid disruption.
                    float ripplePower = Lerp(60f, 90f, Sin(Main.GlobalTimeWrappedHourly + x / xArea * TwoPi) * 0.5f + 0.5f);
                    ripplePower *= Lerp(0.5f, 1f, Time / 300f);

                    WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                    Vector2 ripplePos = waterTop.ToWorldCoordinates() + new Vector2(x, 32f) + Main.rand.NextVector2CircularEdge(50f, 50f);
                    ripple.QueueRipple(ripplePos, Color.White, Vector2.One * ripplePower, RippleShape.Circle, Main.rand.NextFloat(-0.7f, 0.7f) + PiOver2);
                }
            }
        }
    }
}
