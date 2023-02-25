using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Destroyer
{
    public class ProbeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Probe;

        public static int ReelBackTime => BossRushEvent.BossRushActive ? 30 : 60;

        public override bool PreAI(NPC npc)
        {
            if (npc.scale != 1f)
            {
                npc.Size /= npc.scale;
                npc.scale = 1f;
            }

            npc.TargetClosest();
            Player target = Main.player[npc.target];

            Vector2 spawnOffset = Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.97f, 0.97f, npc.whoAmI % 16f / 16f)) * 300f;
            if (npc.whoAmI * 113 % 2 == 1)
                spawnOffset *= -1f;

            Vector2 destination = target.Center + spawnOffset;

            ref float generalTimer = ref npc.ai[2];
            Lighting.AddLight(npc.Center, Color.Red.ToVector3() * 1.6f);

            // Have a brief moment of no damage.
            npc.damage = npc.ai[0] == 2f ? npc.defDamage : 0;

            float hoverSpeed = 22f;
            if (BossRushEvent.BossRushActive)
                hoverSpeed *= 1.5f;
            ref float attackTimer = ref npc.ai[1];

            // Hover into position and look at the target. Once reached, reel back.
            if (npc.ai[0] == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * hoverSpeed, 0.1f);
                if (npc.WithinRange(destination, npc.velocity.Length() * 1.35f))
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * -7f;
                    npc.ai[0] = 1f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }

            // Reel back and decelerate.
            if (npc.ai[0] == 1f)
            {
                npc.velocity *= 0.975f;
                attackTimer++;

                if (attackTimer >= ReelBackTime)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * hoverSpeed;

                    npc.ai[0] = 2f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }

            // Charge at the target and explode once a tile is hit.
            if (npc.ai[0] == 2f)
            {
                npc.knockBackResist = 0f;
                if (Collision.SolidCollision(npc.position, npc.width, npc.height) && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                    for (int i = 0; i < 36; i++)
                    {
                        Dust energy = Dust.NewDustDirect(npc.position, npc.width, npc.height, 182);
                        energy.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 7f);
                        energy.noGravity = true;
                    }

                    npc.active = false;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.velocity.ToRotation();
                npc.damage = 95;
            }

            npc.rotation += MathHelper.Pi;
            generalTimer++;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;

            float telegraphInterpolant = 0f;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (npc.ai[0] == 1f)
            {
                float reelBackInterpolant = Utils.GetLerpValue(0f, ReelBackTime, npc.ai[1], true);
                telegraphInterpolant = Utils.GetLerpValue(0f, 0.3f, reelBackInterpolant, true) * Utils.GetLerpValue(1f, 0.67f, reelBackInterpolant, true);
            }

            // Draw a backglow and laser telegraph before doing the kamikaze charge.
            if (telegraphInterpolant > 0f)
            {
                Texture2D invisible = InfernumTextureRegistry.Invisible.Value;
                Effect laserScopeEffect = Filters.Scene["CalamityMod:PixelatedSightLine"].GetShader().Shader;

                float laserRotation = -npc.rotation;
                if (npc.spriteDirection == -1)
                    laserRotation += MathHelper.Pi;

                laserScopeEffect.Parameters["sampleTexture2"].SetValue(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/CertifiedCrustyNoise").Value);
                laserScopeEffect.Parameters["noiseOffset"].SetValue(Main.GameUpdateCount * -0.004f);
                laserScopeEffect.Parameters["mainOpacity"].SetValue((float)Math.Sqrt(telegraphInterpolant));
                laserScopeEffect.Parameters["Resolution"].SetValue(new Vector2(425f));
                laserScopeEffect.Parameters["laserAngle"].SetValue(laserRotation);
                laserScopeEffect.Parameters["laserWidth"].SetValue(0.002f + (float)Math.Pow(telegraphInterpolant, 4D) * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f));
                laserScopeEffect.Parameters["laserLightStrenght"].SetValue(5f);
                laserScopeEffect.Parameters["color"].SetValue(Color.Lerp(Color.Orange, Color.Red, telegraphInterpolant * 0.6f + 0.4f).ToVector3());
                laserScopeEffect.Parameters["darkerColor"].SetValue(Color.Orange.ToVector3());
                laserScopeEffect.Parameters["bloomSize"].SetValue(0.3f + (1f - telegraphInterpolant) * 0.1f);
                laserScopeEffect.Parameters["bloomMaxOpacity"].SetValue(0.45f);
                laserScopeEffect.Parameters["bloomFadeStrenght"].SetValue(3f);

                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

                laserScopeEffect.CurrentTechnique.Passes[0].Apply();

                float telegraphScale = telegraphInterpolant * MathHelper.Clamp(npc.Distance(Main.player[npc.target].Center) * 2.4f, 10f, 1600f);
                Main.spriteBatch.Draw(invisible, drawPosition, null, Color.White, 0f, invisible.Size() * 0.5f, telegraphScale, SpriteEffects.None, 0f);
                Main.spriteBatch.ExitShaderRegion();

                // Draw the backglow.
                Color backglowColor = Color.Red with { A = 0 } * telegraphInterpolant;
                float backglowOffset = telegraphInterpolant * 4f;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * backglowOffset;
                    Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, npc.GetAlpha(backglowColor), npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, drawPosition, null, npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.6f)), npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }
    }
}