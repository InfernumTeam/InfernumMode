using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class TwinsShield : ModProjectile
    {
        public ref float OwnerIndex => ref projectile.ai[0];
        public ref float Radius => ref projectile.ai[1];
        public NPC Owner => Main.npc[(int)OwnerIndex];
        public const float MaxRadius = 180f;
        public const int HealTime = 180;
        public const int Lifetime = HealTime + HealTime / 3;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shield");
        }

        public override void SetDefaults()
        {
            projectile.width = 72;
            projectile.height = 72;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.timeLeft = Lifetime;
            projectile.scale = 0.001f;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
            {
                projectile.Kill();
                return;
            }

            projectile.Center = Owner.Center;

            Radius = (float)Math.Sin(projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * MaxRadius * 4f;
            if (Radius > MaxRadius)
                Radius = MaxRadius;
            projectile.scale = 2f;

            bool alone = (!NPC.AnyNPCs(NPCID.Spazmatism) && Owner.type == NPCID.Retinazer) || (!NPC.AnyNPCs(NPCID.Retinazer) && Owner.type == NPCID.Spazmatism);
            if (!alone && projectile.timeLeft < HealTime)
                projectile.timeLeft = HealTime;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || !Main.projectile[i].friendly || !projectile.WithinRange(Main.projectile[i].Center, Radius * 0.9f))
                    continue;

                Projectile proj = Main.projectile[i];
                if (proj.timeLeft > 10 && proj.damage > 0)
                {
                    proj.friendly = false;
                    proj.velocity = Vector2.Reflect(proj.velocity, projectile.SafeDirectionTo(Main.player[Player.FindClosest(proj.Center, 1, 1)].Center));
                    proj.penetrate = 1;
                    proj.netUpdate = true;
                }
            }

            CalamityGlobalProjectile.ExpandHitboxBy(projectile, (int)(Radius * projectile.scale), (int)(Radius * projectile.scale));
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
                return false;

            spriteBatch.EnterShaderRegion();

            Vector2 scale = new Vector2(1.5f, 1f);
            DrawData drawData = new DrawData(ModContent.GetTexture("Terraria/Misc/Perlin"),
                projectile.Center - Main.screenPosition + projectile.Size * scale * 0.5f,
                new Rectangle(0, 0, projectile.width, projectile.height),
                new Color(new Vector4(1f)) * 0.7f * projectile.Opacity,
                projectile.rotation,
                projectile.Size,
                scale,
                SpriteEffects.None, 0);

            GameShaders.Misc["ForceField"].UseColor(Owner.type == NPCID.Spazmatism ? Color.LimeGreen : Color.Red);
            GameShaders.Misc["ForceField"].Apply(drawData);
            drawData.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
