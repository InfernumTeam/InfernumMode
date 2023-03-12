using CalamityMod;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class EnergyTelegraph : ModProjectile
    {
        public PrimitiveTrailCopy TelegraphDrawer = null;

        public Vector2[] TelegraphPoints;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Energy Shard");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 9;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 32;
            Projectile.scale = 2f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(TelegraphPoints.Length);
            for (int i = 0; i < TelegraphPoints.Length; i++)
                writer.WritePackedVector2(TelegraphPoints[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int telegraphPointCount = reader.ReadInt32();

            TelegraphPoints = new Vector2[telegraphPointCount];
            for (int i = 0; i < telegraphPointCount; i++)
                TelegraphPoints[i] = reader.ReadPackedVector2();
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(32f, 27f, Projectile.timeLeft) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true) * 0.5f;
            Projectile.scale = CalamityUtils.Convert01To010(Projectile.timeLeft / 32f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.Cyan, Color.Fuchsia, Projectile.ai[0]) with { A = 100 } * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            TelegraphDrawer ??= new(_ => Projectile.scale * 2f, completionRatio =>
            {
                float opacity = Utils.GetLerpValue(0f, 0.15f, completionRatio, true);
                return Projectile.GetAlpha(Color.White) * opacity;
            });
            TelegraphDrawer.Draw(TelegraphPoints, -Main.screenPosition, 43);
            return false;
        }
    }
}
