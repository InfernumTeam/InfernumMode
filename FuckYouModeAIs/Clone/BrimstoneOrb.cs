using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Clone
{
	public class BrimstoneOrb : ModProjectile
    {
        public bool AttachedToCal
        {
            get => projectile.ai[0] == 0f;
            set => projectile.ai[0] = 1f - value.ToInt();
        }
        public ref float OffsetAngle => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brimstone Dart");
            Main.projFrames[projectile.type] = 4;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 900;
            projectile.alpha = 255;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.alpha = Utils.Clamp(projectile.alpha - 30, 0, 255);

            if (!Main.npc.IndexInRange(CalamityGlobalNPC.calamitas) || !Main.npc[CalamityGlobalNPC.calamitas].active)
			{
                projectile.Kill();
                return;
			}

            if (AttachedToCal)
                projectile.Center = Main.npc[CalamityGlobalNPC.calamitas].Center + OffsetAngle.ToRotationVector2() * 160f;
            OffsetAngle += MathHelper.ToRadians(5f);

            Lighting.AddLight(projectile.Center, Color.Red.ToVector3() * 0.825f);
        }

		public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f && !AttachedToCal;

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			if (projectile.Opacity != 1f || AttachedToCal)
				return;

            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D orbTexture = Main.projectileTexture[projectile.type];
            Vector2 origin = projectile.Size * 0.5f;
            Color drawColor = projectile.GetAlpha(Color.White) * 0.2f;
            drawColor.A = 0;
            for (int i = 0; i < 7; i++)
			{
                Vector2 drawPosition = projectile.Center + (MathHelper.TwoPi * i / 7f + Main.GlobalTime * 3.3f).ToRotationVector2() * 6f - Main.screenPosition;
                spriteBatch.Draw(orbTexture, drawPosition, null, drawColor, 0f, origin, projectile.scale, SpriteEffects.None, 0f);
			}
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
