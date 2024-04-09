using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.CrimsonMimic
{
    public class FetidBaghnakhs : ModProjectile
    {
        public float SpinOffsetAngle;

        public int OwnerIndex => (int)Projectile.ai[1];

        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Fetid Baghnakhs");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 900;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(SpinOffsetAngle);

        public override void ReceiveExtraAI(BinaryReader reader) => SpinOffsetAngle = reader.ReadSingle();

        public override void AI()
        {
            Time++;
            SpinOffsetAngle += TwoPi / 60f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            Projectile.Center = Main.npc[OwnerIndex].Center + SpinOffsetAngle.ToRotationVector2() * Projectile.Opacity * 120f;

            if (!Main.npc[OwnerIndex].active)
                Projectile.Kill();
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool? CanDamage() => Projectile.Opacity >= 1f;

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 3);
            return false;
        }
    }
}
