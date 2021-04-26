using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Clone
{
	public class BrimstoneRay2 : BaseLaserbeamProjectile
    {
        public ref float OwnerIndex => ref projectile.ai[1];
        public const int LaserLifetime = 90;

        public override float Lifetime => LaserLifetime;
        public override Color LightCastColor => Color.Red;
        public override Texture2D LaserBeginTexture => Main.projectileTexture[projectile.type];
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/BrimstoneRayMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/BrimstoneRayEnd");
        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Ray");

        public override void SetDefaults()
        {
			projectile.width = projectile.height = 10;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = 300;
        }

        public override void AttachToSomething()
        {
            if (!Main.projectile.IndexInRange((int)OwnerIndex) || Main.npc[(int)OwnerIndex].type != ModContent.NPCType<CalamitasRun3>())
            {
                projectile.Kill();
                return;
            }

            projectile.Center = Main.npc[(int)OwnerIndex].Center + (Main.npc[(int)OwnerIndex].rotation + MathHelper.PiOver2).ToRotationVector2() * 56f;
        }

		public override void Kill(int timeLeft)
		{
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (float offset = 30f; offset <= 1800f; offset += 80f)
			{
                Vector2 spawnPosition = projectile.Center + projectile.velocity * offset;
                Utilities.NewProjectileBetter(spawnPosition, projectile.velocity * 0.4f, ModContent.ProjectileType<HomingBrimstoneDart>(), 70, 0f);
			}
		}

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 240);

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
