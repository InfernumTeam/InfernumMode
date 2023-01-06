using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class TwinsShield : ModProjectile
    {
        public NPC Owner => Main.npc[(int)OwnerIndex];

        public ref float OwnerIndex => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public const float MaxRadius = 180f;

        public const int HealTime = 180;

        public const int Lifetime = HealTime + HealTime / 3;

        public override string Texture => "InfernumMode/Assets/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shield");
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Owner.Center;

            Radius = (float)Math.Sin(Projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * MaxRadius * 4f;
            if (Radius > MaxRadius)
                Radius = MaxRadius;
            Projectile.scale = 2f;

            bool alone = !NPC.AnyNPCs(NPCID.Spazmatism) && Owner.type == NPCID.Retinazer || !NPC.AnyNPCs(NPCID.Retinazer) && Owner.type == NPCID.Spazmatism;
            if (!alone && Projectile.timeLeft < HealTime)
                Projectile.timeLeft = HealTime;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!Main.projectile[i].active || !Main.projectile[i].friendly || !Projectile.WithinRange(Main.projectile[i].Center, Radius * 0.9f))
                    continue;

                Projectile proj = Main.projectile[i];
                if (proj.timeLeft > 10 && proj.damage > 0)
                {
                    proj.friendly = false;
                    proj.velocity = Vector2.Reflect(proj.velocity, Projectile.SafeDirectionTo(Main.player[Player.FindClosest(proj.Center, 1, 1)].Center));
                    proj.penetrate = 1;
                    proj.netUpdate = true;
                }
            }

            Projectile.ExpandHitboxBy((int)(Radius * Projectile.scale), (int)(Radius * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
                return false;

            Main.spriteBatch.EnterShaderRegion();

            Vector2 scale = new(1.5f, 1f);
            DrawData drawData = new(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value,
                Projectile.Center - Main.screenPosition + Projectile.Size * scale * 0.5f,
                new Rectangle(0, 0, Projectile.width, Projectile.height),
                new Color(new Vector4(1f)) * 0.7f * Projectile.Opacity,
                Projectile.rotation,
                Projectile.Size,
                scale,
                SpriteEffects.None, 0);

            GameShaders.Misc["ForceField"].UseColor(Owner.type == NPCID.Spazmatism ? Color.LimeGreen : Color.Red);
            GameShaders.Misc["ForceField"].Apply(drawData);
            drawData.Draw(Main.spriteBatch);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
