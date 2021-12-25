using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public class AresUnstablePulseBlast : ModProjectile
    {
        public bool ShouldExplodeDiagonally => projectile.ai[0] == 1f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exopulse Energy Burst");
        }

        public override void SetDefaults()
        {
            projectile.width = 128;
            projectile.height = 128;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.timeLeft = 240;
            cooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
        }

        public override void AI()
        {
            int pulseArm = NPC.FindFirstNPC(ModContent.NPCType<AresPulseCannon>());

            // Die if the pulse arm is not present.
            if (pulseArm == -1)
            {
                projectile.Kill();
                return;
            }

            if (projectile.velocity.Length() > 1f)
            {
                projectile.velocity *= 1.085f;
                projectile.rotation = projectile.velocity.ToRotation();
                projectile.scale = MathHelper.Lerp(projectile.scale, 1f, 0.1f);
            }
            else
            {
                // Bulge rapidly while growing.
                projectile.scale = MathHelper.Lerp(0.7f, 1.3f, (float)Math.Cos(projectile.timeLeft * 0.24f + projectile.identity) * 0.5f + 0.5f) * projectile.Opacity;

                // Stay in position before being fired.
                Vector2 holdPosition = Main.npc[pulseArm].Center + (Main.npc[pulseArm].Infernum().ExtraAI[0] + projectile.ai[0]).ToRotationVector2() * 200f;
                projectile.Center = Vector2.Lerp(projectile.Center, projectile.Center, 0.15f).MoveTowards(holdPosition, 15f);
            }

            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.04f, 0f, 1f);
        }

        public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f && projectile.velocity.Length() > 1f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 scale = new Vector2(projectile.velocity.Length() * 0.12f + projectile.scale, 1f) / texture.Size() * projectile.Size;
            Color color = projectile.GetAlpha(Color.Lerp(Color.Violet, Color.White, 0.45f)) * 0.45f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 1.6f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, color, projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, null, color, projectile.rotation, origin, scale, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(projectile.Center, projectile.Size.Length() / 1.414f, targetHitbox);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
