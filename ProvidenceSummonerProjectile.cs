using CalamityMod;
using CalamityMod.NPCs.Providence;
using CalamityMod.Particles;
using CalamityMod.Tiles.FurnitureProfaned;
using InfernumMode.BehaviorOverrides.BossAIs.Yharon;
using InfernumMode.Particles;
using InfernumMode.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode
{
    public class ProvidenceSummonerProjectile : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public const int Lifetime = 375;

        public override string Texture => "CalamityMod/Items/SummonItems/ProfanedCoreUnlimited";

        public override void SetDefaults()
        {
            projectile.width = 24;
            projectile.height = 24;
            projectile.aiStyle = -1;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = Lifetime;
            projectile.Opacity = 0f;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Fade in.
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.015f, 0f, 1f);

            // Rise upward and create a spiral of fire around the core.
            if (Time >= 70f && Time < 210f)
            {
                projectile.velocity = Vector2.Lerp(projectile.velocity, -Vector2.UnitY * 1.75f, 0.025f);
                for (int i = 0; i < Math.Abs(projectile.velocity.Y) * 1.6f + 1; i++)
                {
                    if (Main.rand.NextBool(2))
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            float verticalOffset = Main.rand.NextFloat() * -projectile.velocity.Y;
                            Vector2 dustSpawnOffset = Vector2.UnitX * Main.rand.NextFloatDirection() * 0.05f;
                            dustSpawnOffset.X += (float)Math.Sin((projectile.position.Y + verticalOffset) * 0.06f + MathHelper.TwoPi * j / 3f) * 0.5f;
                            dustSpawnOffset.X = MathHelper.Lerp(Main.rand.NextFloat() - 0.5f, dustSpawnOffset.X, MathHelper.Clamp(-projectile.velocity.Y, 0f, 1f));
                            dustSpawnOffset.Y = -Math.Abs(dustSpawnOffset.X) * 0.25f;
                            dustSpawnOffset *= Utils.InverseLerp(210f, 180f, Time, true) * new Vector2(40f, 50f);
                            dustSpawnOffset.Y += verticalOffset;

                            Dust fire = Dust.NewDustPerfect(projectile.Center + dustSpawnOffset, 6, Vector2.Zero, 0, Color.White * 0.1f, 1.1f);
                            fire.velocity.Y = Main.rand.NextFloat(2f);
                            fire.fadeIn = 0.6f;
                            fire.noGravity = true;
                        }
                    }
                }
            }

            // Play a rumble sound.
            if (Time == 75f)
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LeviathanSummonBase"), projectile.Center);

            if (Time >= 210f)
            {
                float jitterFactor = MathHelper.Lerp(0.4f, 3f, Utils.InverseLerp(0f, 2f, projectile.velocity.Length(), true));

                projectile.velocity *= 0.96f;
                projectile.Center += Main.rand.NextVector2Circular(jitterFactor, jitterFactor);

                // Create screen shake effects.
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.InverseLerp(2300f, 1300f, Main.LocalPlayer.Distance(projectile.Center), true) * jitterFactor * 2f;

                // Create falling rock particles.
                if (Main.rand.NextBool(10))
                {
                    Vector2 rockSpawnPosition = projectile.Center + Vector2.UnitX * Main.rand.NextFloatDirection() * 900f;
                    rockSpawnPosition = Utilities.GetGroundPositionFrom(rockSpawnPosition, Searches.Chain(new Searches.Up(9000), new Conditions.IsSolid()));
                    StoneDebrisParticle2 rock = new StoneDebrisParticle2(rockSpawnPosition, Vector2.UnitY * 16f, Color.Brown, Main.rand.NextFloat(1f, 1.4f), 90);
                    GeneralParticleHandler.SpawnParticle(rock);
                }
            }

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Utils.InverseLerp(2300f, 1300f, Main.LocalPlayer.Distance(projectile.Center), true) * 16f;

            // Make the crystal shatter.
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/NPCKilled/ProvidenceDeath"), projectile.Center);

            for (int i = 1; i <= 4; i++)
                Gore.NewGore(projectile.Center, Main.rand.NextVector2Circular(8f, 8f), mod.GetGoreSlot($"ProfanedCoreGore{i}"), projectile.scale);

            // Emit fire.
            for (int i = 0; i < 32; i++)
            {
                Dust fire = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(24f, 25f), 6, Vector2.Zero, 0, Color.White * 0.1f, 1.1f);
                fire.velocity.Y = Main.rand.NextFloat(2f);
                fire.fadeIn = 0.6f;
                fire.scale = 1.5f;
                fire.noGravity = true;
            }

            // Create an explosion and summon Providence.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CalamityUtils.SpawnBossBetter(projectile.Center - Vector2.UnitY * 325f, ModContent.NPCType<Providence>());
                Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<ProvSummonFlameExplosion>(), 0, 0f);

                // Break existing tiles.
                // This is done to ensure that there are no unexpected tiles that may trivialize the platforming aspect of the fight.
                int[] validTiles = new int[]
                {
                    ModContent.TileType<ProfanedSlab>(),
                    ModContent.TileType<RunicProfanedBrick>(),
                    ModContent.TileType<ProvidenceSummoner>(),
                };
                for (int i = PoDWorld.ProvidenceArena.Left; i < PoDWorld.ProvidenceArena.Right; i++)
                {
                    for (int j = PoDWorld.ProvidenceArena.Top; j < PoDWorld.ProvidenceArena.Bottom; j++)
                    {
                        Tile tile = CalamityUtils.ParanoidTileRetrieval(i, j);
                        if (tile.active() && (Main.tileSolid[tile.type] || Main.tileSolidTop[tile.type]))
                        {
                            if (!validTiles.Contains(tile.type))
                                WorldGen.KillTile(i, j);
                        }
                    }
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture(Texture);

            for (int i = 0; i < 8; i++)
            {
                Color color = Color.Lerp(new Color(1f, 0.62f, 0f, 0f), Color.White, (float)Math.Pow(projectile.Opacity, 1.63)) * projectile.Opacity;
                Vector2 drawOffset = (Time * MathHelper.TwoPi / 67f + MathHelper.TwoPi * i / 8f).ToRotationVector2() * (1f - projectile.Opacity) * 75f;
                Vector2 drawPosition = projectile.Center - Main.screenPosition + drawOffset;
                Main.spriteBatch.Draw(texture, drawPosition, null, color, projectile.rotation, texture.Size() * 0.5f, projectile.scale, 0, 0f);
            }

            return false;
        }
    }
}
