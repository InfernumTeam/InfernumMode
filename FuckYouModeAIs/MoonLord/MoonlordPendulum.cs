using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Enums;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.MoonLord
{
    public class MoonlordPendulum : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Phantasmal Deathray");
        }

        public override void SetDefaults()
        {
            projectile.width = 48;
            projectile.height = 48;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = 300;
            cooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
            writer.Write(projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
            projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            Vector2? vector78 = null;

            if (projectile.velocity.HasNaNs() || projectile.velocity == Vector2.Zero)
            {
                projectile.velocity = -Vector2.UnitY;
            }

            if (Main.npc[(int)projectile.ai[1]].active && (Main.npc[(int)projectile.ai[1]].type == NPCID.MoonLordFreeEye || Main.npc[(int)projectile.ai[1]].type == ModContent.NPCType<EldritchSeal>()))
            {
                Vector2 value21 = new Vector2(27f, 59f);
                Vector2 fireFrom = new Vector2(Main.npc[(int)projectile.ai[1]].Center.X, Main.npc[(int)projectile.ai[1]].Center.Y - 32f);
                Vector2 value22 = Utils.Vector2FromElipse(Main.npc[(int)projectile.ai[1]].localAI[0].ToRotationVector2(), value21 * Main.npc[(int)projectile.ai[1]].localAI[1]);
                projectile.Center = Main.npc[(int)projectile.ai[1]].Center + value22;
            }

            if (projectile.velocity.HasNaNs() || projectile.velocity == Vector2.Zero)
            {
                projectile.velocity = -Vector2.UnitY;
            }

            float num801 = 0.5f;
            projectile.localAI[0] += 1f;
            if (projectile.localAI[0] >= (Main.npc[(int)projectile.ai[1]].type == ModContent.NPCType<EldritchSeal>() ? 570f : 300))
            {
                projectile.Kill();
                return;
            }
            if (Main.npc[(int)projectile.ai[1]].type == ModContent.NPCType<EldritchSeal>())
            {
                projectile.damage = projectile.localAI[0] >= 30f ? 70 : 0;
            }

            projectile.scale = (float)Math.Sin(projectile.localAI[0] * MathHelper.Pi / (Main.npc[(int)projectile.ai[1]].type == ModContent.NPCType<EldritchSeal>() ? 570f : 300)) * 10f * num801;
            if (projectile.scale > num801)
            {
                projectile.scale = num801;
            }
            float num804 = projectile.velocity.ToRotation();
            num804 += projectile.ai[0];
            projectile.rotation = num804 - MathHelper.PiOver2;
            projectile.velocity = num804.ToRotationVector2();

            float num805 = 3f; //3f
            float num806 = projectile.width;

            Vector2 samplingPoint = projectile.Center;
            if (vector78.HasValue)
            {
                samplingPoint = vector78.Value;
            }

            float[] array3 = new float[(int)num805];
            Collision.LaserScan(samplingPoint, projectile.velocity, num806 * projectile.scale, 3000f, array3);

            float amount = 0.5f; //0.5f
            projectile.localAI[1] = MathHelper.Lerp(projectile.localAI[1], 3000f, amount); //length of laser, linear interpolation

            DelegateMethods.v3_1 = new Vector3(0.3f, 0.65f, 0.7f);
            Utils.PlotTileLine(projectile.Center, projectile.Center + projectile.velocity * projectile.localAI[1], projectile.width * projectile.scale, new Utils.PerLinePoint(DelegateMethods.CastLight));
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (projectile.velocity == Vector2.Zero)
            {
                return false;
            }
            Texture2D texture2D19 = ModContent.GetTexture("InfernumMode/FuckYouModeAIs/DoG/DoGDeathray");
            Texture2D texture2D20 = Main.extraTexture[21];
            Texture2D texture2D21 = Main.extraTexture[22];
            float num228 = projectile.localAI[1];
			Color color44 = new Color(200, 200, 200, 0) * 0.9f;
            SpriteBatch arg_AF92_0 = Main.spriteBatch;
            Texture2D arg_AF92_1 = texture2D19;
            Vector2 arg_AF92_2 = projectile.Center - Main.screenPosition;
			Rectangle? sourceRectangle2 = null;
            arg_AF92_0.Draw(arg_AF92_1, arg_AF92_2, sourceRectangle2, color44, projectile.rotation, texture2D19.Size() / 2f, projectile.scale, SpriteEffects.None, 0f);
            num228 -= (texture2D19.Height / 2 + texture2D21.Height) * projectile.scale;
            Vector2 value20 = projectile.Center;
            value20 += projectile.velocity * projectile.scale * texture2D19.Height / 2f;
            if (num228 > 0f)
            {
                float num229 = 0f;
				Rectangle rectangle7 = new Rectangle(0, 16 * (projectile.timeLeft / 3 % 5), texture2D20.Width, 16);
                while (num229 + 1f < num228)
                {
                    if (num228 - num229 < rectangle7.Height)
                    {
                        rectangle7.Height = (int)(num228 - num229);
                    }
                    Main.spriteBatch.Draw(texture2D20, value20 - Main.screenPosition, new Microsoft.Xna.Framework.Rectangle?(rectangle7), color44, projectile.rotation, new Vector2(rectangle7.Width / 2, 0f), projectile.scale, SpriteEffects.None, 0f);
                    num229 += rectangle7.Height * projectile.scale;
                    value20 += projectile.velocity * rectangle7.Height * projectile.scale;
                    rectangle7.Y += 16;
                    if (rectangle7.Y + rectangle7.Height > texture2D20.Height)
                    {
                        rectangle7.Y = 0;
                    }
                }
            }
            SpriteBatch arg_B1F8_0 = Main.spriteBatch;
            Texture2D arg_B1F8_1 = texture2D21;
            Vector2 arg_B1F8_2 = value20 - Main.screenPosition;
            sourceRectangle2 = null;
            arg_B1F8_0.Draw(arg_B1F8_1, arg_B1F8_2, sourceRectangle2, color44, projectile.rotation, texture2D21.Frame(1, 1, 0, 0).Top(), projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 unit = projectile.velocity;
            Utils.PlotTileLine(projectile.Center, projectile.Center + unit * projectile.localAI[1], projectile.width * projectile.scale, new Utils.PerLinePoint(DelegateMethods.CutTiles));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
            {
                return true;
            }
            float num6 = 0f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projectile.Center, projectile.Center + projectile.velocity * projectile.localAI[1], 22f * projectile.scale, ref num6))
            {
                return true;
            }
            return false;
        }
    }
}
