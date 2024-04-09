using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Tiles.Misc;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.CragsCutscene
{
    public class CalamitasCutsceneProj : ModProjectile
    {
        public enum AnimationState
        {
            WaitingForPlayer,
            PlaceDownFlower,
            LookAtPlayer,
            Disappear
        }

        public AnimationState CurrentAnimation
        {
            get => (AnimationState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public ref float Time => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Calamitas");
            Main.projFrames[Type] = 7;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 50;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = int.MaxValue;
        }

        public override void AI()
        {
            Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            switch (CurrentAnimation)
            {
                case AnimationState.WaitingForPlayer:
                    Projectile.frame = 6;
                    if (Projectile.WithinRange(closestPlayer.Center, 640f))
                    {
                        Time = 0f;
                        CurrentAnimation = AnimationState.PlaceDownFlower;
                        Projectile.netUpdate = true;
                    }
                    break;
                case AnimationState.PlaceDownFlower:
                    Projectile.frame = 6;
                    if (Time >= 240f)
                    {
                        SoundEngine.PlaySound(SoundID.Item30, Projectile.Center);

                        int flowerX = (int)(Projectile.spriteDirection + Projectile.Bottom.X / 16f);
                        int flowerY = (int)(Projectile.Bottom.Y / 16f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int dy = -2; dy < 15; dy++)
                            {
                                WorldGen.Place2xX(flowerX, flowerY - dy, (ushort)ModContent.TileType<BrimstoneRose>());
                            }

                            Time = 0f;
                            CurrentAnimation = AnimationState.LookAtPlayer;
                            Projectile.netUpdate = true;
                        }

                        for (int i = 0; i < 15; i++)
                        {
                            Vector2 fireSpawnPosition = new Vector2(flowerX, flowerY) * 16f + Main.rand.NextVector2Circular(50f, 50f);
                            Color fireColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.68f));
                            MediumMistParticle fire = new(fireSpawnPosition, Main.rand.NextVector2Circular(3f, 3f), fireColor, Color.Gray, 0.8f, 236f, Main.rand.NextFloat(0.0025f));
                            GeneralParticleHandler.SpawnParticle(fire);
                        }
                    }
                    break;
                case AnimationState.LookAtPlayer:
                    if (Time >= 90f)
                        Projectile.spriteDirection = (closestPlayer.Center.X > Projectile.Center.X).ToDirectionInt();
                    if (Time >= 210f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalTeleportSound, closestPlayer.Center);
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 fireSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(35f, 50f);
                            Color fireColor = Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.68f));
                            HeavySmokeParticle fire = new(fireSpawnPosition, Main.rand.NextVector2Circular(3f, 3f), fireColor, 40, 0.8f, 1f, Main.rand.NextFloat(0.0025f), true);
                            GeneralParticleHandler.SpawnParticle(fire);
                        }
                        Projectile.Kill();
                    }

                    Projectile.frame = (int)(Time / 5f) % 6;
                    break;
            }

            Projectile.gfxOffY = 6f;
            Projectile.velocity.Y = Clamp(Projectile.velocity.Y + 0.45f, 0f, 12f);
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }
    }
}
