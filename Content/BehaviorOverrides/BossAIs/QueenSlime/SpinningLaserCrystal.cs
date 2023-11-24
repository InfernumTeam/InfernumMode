using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class SpinningLaserCrystal : ModProjectile
    {
        public Vector2 SpinningCenter
        {
            get;
            set;
        }

        public float AimDirection
        {
            get;
            set;
        }

        public ref float SpinOffsetAngle => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => $"Terraria/Images/Extra_{ExtrasID.QueenSlimeCrystalCore}";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hallow Crystal");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AimDirection);
            writer.WriteVector2(SpinningCenter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AimDirection = reader.ReadSingle();
            SpinningCenter = reader.ReadVector2();
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 20f, Time, true);
            SpinOffsetAngle += 0.09f * Pow(Utils.GetLerpValue(54f, 0f, Time, true), 2f);
            Projectile.Center = SpinningCenter + SpinOffsetAngle.ToRotationVector2() * 300f;

            // Look at the player.
            float aimInterpolant = Utils.GetLerpValue(40f, 6f, Time, true);
            float idealAngle = Projectile.AngleTo(Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center);
            AimDirection = AimDirection.AngleLerp(idealAngle, aimInterpolant);

            if (Time == 64f)
            {
                SoundEngine.PlaySound(SoundID.Item163, Projectile.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(Projectile.Center, AimDirection.ToRotationVector2() * 0.01f, ModContent.ProjectileType<HallowLaserbeam>(), QueenSlimeBehaviorOverride.AimedLaserbeamDamage, 0f);
            }

            Time++;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Create a crystal shatter sound.
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);

            // Create a bunch of crystal shards.
            for (int i = 0; i < 15; i++)
            {
                Dust crystalShard = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), Main.rand.Next(DustID.BlueCrystalShard, DustID.PurpleCrystalShard + 1));
                crystalShard.velocity = Main.rand.NextVector2Circular(4f, 4f) - Vector2.UnitY * 2.5f;
                crystalShard.noGravity = Main.rand.NextBool();
                crystalShard.scale = Main.rand.NextFloat(0.9f, 1.3f);
            }

            for (int i = 1; i <= 3; i++)
                Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, Main.rand.NextVector2CircularEdge(4f, 4f), Mod.Find<ModGore>($"QSCrystal{i}").Type, Projectile.scale);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float telegraphInterpolant = Utils.GetLerpValue(2f, 6f, Time, true) * Utils.GetLerpValue(54f, 48f, Time, true);
            BloomLineDrawInfo lineInfo = new(rotation: -AimDirection,
                width: 0.002f + Pow(telegraphInterpolant, 4f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f),
                bloom: Lerp(0.3f, 0.4f, telegraphInterpolant),
                scale: Vector2.One * telegraphInterpolant * 1900f,
                main: Color.Lerp(Color.Cyan, Color.SkyBlue, telegraphInterpolant * 0.6f + 0.4f),
                darker: Color.Blue,
                opacity: Sqrt(telegraphInterpolant),
                bloomOpacity: 0.35f,
                lightStrength: 5f);

            Utilities.DrawBloomLineTelegraph(Projectile.Center - Main.screenPosition, lineInfo);

            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 }, Color.White, 4f);
            return false;
        }

        public override bool? CanDamage() => Time >= 27f;

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.HotPink with { A = 0 }, Color.White, Utils.GetLerpValue(0f, 35f, Time, true)) * Projectile.Opacity;
    }
}
