using InfernumMode.Miscellaneous;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.Leviathan
{
    public class LeviathanMinionSpawner : ModProjectile
    {
        internal ref float Time => ref projectile.ai[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spawner");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 20;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
            projectile.timeLeft = 150;
        }

        public override void AI()
        {
            Time++;
            CreateVisuals();
        }

        internal void CreateVisuals()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            WorldUtils.Find((projectile.Center - Vector2.UnitY * 1200f).ToTileCoordinates(), Searches.Chain(new Searches.Down(150), new CustomTileConditions.IsWater()), out Point waterTop);

            // Making bubbling water as an indicator.
            if (Time % 4f == 3f && Time > 50f)
            {
                float xArea = MathHelper.Lerp(150f, 270f, Time / 150f);
                Vector2 dustSpawnPosition = waterTop.ToWorldCoordinates() + Vector2.UnitY * 25f;
                dustSpawnPosition.X += Main.rand.NextFloatDirection() * xArea * 0.4f;
                Dust bubble = Dust.NewDustPerfect(dustSpawnPosition, 267, Vector2.UnitY * -12f);
                bubble.noGravity = true;
                bubble.scale = 1.9f;    
                bubble.color = Color.CornflowerBlue;

                for (float x = -xArea; x <= xArea; x += 50f)
                {
                    // As well as liquid disruption.
                    float ripplePower = MathHelper.Lerp(20f, 50f, (float)Math.Sin(Main.GlobalTime + x / xArea * MathHelper.TwoPi) * 0.5f + 0.5f);
                    ripplePower *= MathHelper.Lerp(0.5f, 1f, Time / 150f);

                    WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                    Vector2 ripplePos = waterTop.ToWorldCoordinates() + new Vector2(x, 32f) + Main.rand.NextVector2CircularEdge(15f, 15f);
                    ripple.QueueRipple(ripplePos, Color.White, Vector2.One * ripplePower, RippleShape.Circle, Main.rand.NextFloat(-0.5f, 0.5f) + MathHelper.PiOver2);
                }
            }
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Zombie, projectile.Center, 40);
            if (Main.netMode != NetmodeID.Server)
            {
                WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                Vector2 ripplePos = projectile.Center;

                for (int i = 0; i < 7; i++)
                    ripple.QueueRipple(ripplePos, Color.White, Vector2.One * 500f, RippleShape.Square, Main.rand.NextFloat(MathHelper.TwoPi));
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int spawnedNPC = NPC.NewNPC((int)projectile.Center.X, (int)projectile.Center.Y, (int)projectile.ai[0]);
            if (Main.npc.IndexInRange(spawnedNPC))
                Main.npc[spawnedNPC].velocity = Vector2.UnitY.RotatedByRandom(0.4f) * -12f;
        }
    }
}
