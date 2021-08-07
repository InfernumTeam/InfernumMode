using CalamityMod.Projectiles.DraedonsArsenal;
using CalamityMod.World;
using InfernumMode.Miscellaneous;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;
using LeviathanNPC = CalamityMod.NPCs.Leviathan.Leviathan;

namespace InfernumMode.FuckYouModeAIs.Leviathan
{
    public class LeviathanSpawner : ModProjectile
    {
        internal ref float Time => ref projectile.ai[0];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Fountain of Pain");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 20;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
            projectile.timeLeft = 450;
        }

        public override void AI()
        {
            projectile.Opacity = (float)Math.Sin(projectile.timeLeft / 120f * MathHelper.Pi) * 4f;
            if (projectile.Opacity > 1f)
                projectile.Opacity = 1f;

            if (projectile.timeLeft == 340)
			{
                var sound = Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LeviathanSummonBase"), projectile.Center);
                if (sound != null)
                    sound.Volume = MathHelper.Clamp(sound.Volume * 1.5f, 0f, 1f);
            }

            Main.LocalPlayer.Infernum().CurrentScreenShakePower = (float)Math.Pow(Utils.InverseLerp(180f, 290f, Time, true), 0.3D) * 20f;
            Main.LocalPlayer.Infernum().CurrentScreenShakePower += (float)Math.Sin(MathHelper.Pi * Math.Pow(Utils.InverseLerp(300f, 440f, Time, true), 0.5D)) * 35f;

            if (projectile.timeLeft == 45)
			{
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LeviathanRoarCharge"), projectile.Center);
                if (Main.netMode != NetmodeID.Server)
                {
                    WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                    Vector2 ripplePos = projectile.Center;

                    for (int i = 0; i < 7; i++)
                        ripple.QueueRipple(ripplePos, Color.White, Vector2.One * 4000f, RippleShape.Square, Main.rand.NextFloat(MathHelper.TwoPi));
                }
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return;

                for (int i = -1; i <= 1; i += 2)
                {
                    int wave = Utilities.NewProjectileBetter(projectile.Center, Vector2.UnitX * 15f * i, ModContent.ProjectileType<LeviathanSpawnWave>(), 150, 0f);
                    Main.projectile[wave].Bottom = projectile.Center + Vector2.UnitY * 700f;
                }

                int leviathan = NPC.NewNPC((int)projectile.Center.X, (int)projectile.Center.Y, ModContent.NPCType<LeviathanNPC>());
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

            WorldUtils.Find((projectile.Center - Vector2.UnitY * 1200f).ToTileCoordinates(), Searches.Chain(new Searches.Down(150), new CustomTileConditions.IsWater()), out Point waterTop);

            // Making bubbling water as an indicator.
            if (Time % 4f == 3f && Time > 90f)
            {
                float xArea = MathHelper.Lerp(400f, 1150f, Time / 300f);
                Vector2 dustSpawnPosition = waterTop.ToWorldCoordinates() + Vector2.UnitY * 25f;
                dustSpawnPosition.X += Main.rand.NextFloatDirection() * xArea * 0.35f;
                Dust bubble = Dust.NewDustPerfect(dustSpawnPosition, 267, Vector2.UnitY * -12f);
                bubble.noGravity = true;
                bubble.scale = 1.9f;    
                bubble.color = Color.CornflowerBlue;

                for (float x = -xArea; x <= xArea; x += 110f)
                {
                    // As well as liquid disruption.
                    float ripplePower = MathHelper.Lerp(60f, 90f, (float)Math.Sin(Main.GlobalTime + x / xArea * MathHelper.TwoPi) * 0.5f + 0.5f);
                    ripplePower *= MathHelper.Lerp(0.5f, 1f, Time / 300f);

                    WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                    Vector2 ripplePos = waterTop.ToWorldCoordinates() + new Vector2(x, 32f) + Main.rand.NextVector2CircularEdge(50f, 50f);
                    ripple.QueueRipple(ripplePos, Color.White, Vector2.One * ripplePower, RippleShape.Circle, Main.rand.NextFloat(-0.7f, 0.7f) + MathHelper.PiOver2);
                }
            }
        }

        public override void Kill(int timeLeft)
        {
        }
    }
}
