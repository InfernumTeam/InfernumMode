using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
	public class ShadowflameOrb : ModNPC
    {
        public PrimitiveTrailCopy FireDrawer;
        public Player Target => Main.player[npc.target];
        public NPC Owner => Main.npc[(int)npc.ai[0]];
        public ref float Time => ref npc.ai[2];
        public ref float StunTimer => ref npc.ai[3];
        internal ref float OwnerAttackTimer => ref Owner.Infernum().ExtraAI[10];
        internal TwinsAttackSynchronizer.SpazmatismAttackState OwnerAttackState => (TwinsAttackSynchronizer.SpazmatismAttackState)(int)Owner.Infernum().ExtraAI[11];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadowflame Orb");
			NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
        }

        public override void SetDefaults()
        {
			npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.width = npc.height = 22;
            npc.damage = 5;
            npc.lifeMax = 5000;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.netAlways = true;
            npc.scale = 1.15f;
        }

		public override void AI()
        {
            if (!Main.npc.IndexInRange((int)npc.ai[0]) || !Owner.active)
			{
				npc.active = false;
				npc.netUpdate = true;
				return;
			}

            npc.timeLeft = 3600;
            npc.target = Owner.target;

            if (StunTimer > 0f)
			{
                StunTimer--;
                npc.velocity *= 0.9f;
                return;
			}

            Vector2 flyDestination = Target.Center;
            float flySpeed = MathHelper.SmoothStep(8f, 23f, Utils.InverseLerp(400f, 1500f, npc.Distance(flyDestination), true));

            if (!npc.WithinRange(flyDestination, 105f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(flyDestination) * flySpeed, 0.08f);

            npc.rotation += MathHelper.ToRadians(10f * Math.Sign(npc.velocity.X));

            if (Main.netMode != NetmodeID.MultiplayerClient && Time % 90f == 89f)
                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ShadowflameExplosion>(), 120, 0f);

            Time++;
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.InverseLerp(0f, 0.27f, completionRatio, true), 0.4f) * Utils.InverseLerp(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(3f, npc.width * 0.6f, squeezeInterpolant);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.White, Color.DarkMagenta, 0.25f);
            color *= 1f - 0.5f * (float)Math.Pow(completionRatio, 6D);
            return color * npc.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2[] baseOldPositions = npc.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToArray();
            if (baseOldPositions.Length <= 2)
                return true;

            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(2f);
            GameShaders.Misc["Infernum:Fire"].UseImage("Images/Misc/Perlin");
            FireDrawer.Draw(npc.oldPos, npc.Size * 0.5f - Main.screenPosition + npc.velocity * 1.6f, 47);
            return true;
        }

		public override bool CheckDead()
		{
            npc.life = npc.lifeMax;
            StunTimer = 300f;
            npc.netUpdate = true;
            return false;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Shadowflame>(), 150);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.timeLeft > 10 && projectile.damage > 0)
                projectile.Kill();
        }

        public override bool CheckActive() => false;
    }
}
