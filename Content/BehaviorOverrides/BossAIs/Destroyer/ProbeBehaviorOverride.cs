using CalamityMod.Events;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

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

            Vector2 spawnOffset = Vector2.UnitY.RotatedBy(Lerp(-0.97f, 0.97f, npc.whoAmI % 16f / 16f)) * 300f;
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
                    SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerChargeImpactSound with { Pitch = 0.5f, Volume = 0.6f }, npc.Center);
                    npc.velocity = npc.SafeDirectionTo(target.Center) * hoverSpeed;
                    npc.oldPos = new Vector2[npc.oldPos.Length];
                    npc.ai[0] = 2f;
                    npc.netUpdate = true;
                }
                npc.rotation = npc.AngleTo(target.Center);
            }

            // Charge at the target and explode once a tile is hit.
            if (npc.ai[0] == 2f)
            {
                npc.knockBackResist = 0f;
                if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                    BlowUpEffects(npc);

                npc.rotation = npc.velocity.ToRotation();
                npc.damage = 95;
            }

            npc.rotation += Pi;
            generalTimer++;
            return false;
        }

        public static void BlowUpEffects(NPC npc)
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerBombExplodeSound, npc.Center);
            for (int i = 0; i < 36; i++)
            {
                Dust energy = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.TheDestroyer);
                energy.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 7f);
                energy.noGravity = true;
            }

            npc.active = false;
            npc.netUpdate = true;
        }

        public static void KillAllProbes()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.type == NPCID.Probe)
                    BlowUpEffects(npc);
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Allow afterimages.
            NPCID.Sets.TrailingMode[npc.type] = 1;
            NPCID.Sets.TrailCacheLength[npc.type] = 6;

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
                // Draw the bloom laser line telegraph.
                float laserRotation = -npc.rotation;
                if (npc.spriteDirection == -1)
                    laserRotation += Pi;

                BloomLineDrawInfo lineInfo = new(rotation: laserRotation,
                    width: 0.002f + Pow(telegraphInterpolant, 4f) * (Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.001f + 0.001f),
                    bloom: Lerp(0.3f, 0.4f, telegraphInterpolant),
                    scale: Vector2.One * telegraphInterpolant * Clamp(npc.Distance(Main.player[npc.target].Center) * 2.4f, 10f, 1600f),
                    main: Color.Lerp(Color.Orange, Color.Red, telegraphInterpolant * 0.6f + 0.4f),
                    darker: Color.Orange,
                    opacity: Sqrt(telegraphInterpolant),
                    bloomOpacity: 0.35f,
                    lightStrength: 5f);

                Utilities.DrawBloomLineTelegraph(drawPosition, lineInfo);

                // Draw the backglow.
                Color backglowColor = Color.Red with { A = 0 } * telegraphInterpolant;
                float backglowOffset = telegraphInterpolant * 4f;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 drawOffset = (TwoPi * i / 12f).ToRotationVector2() * backglowOffset;
                    Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, npc.GetAlpha(backglowColor), npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            // Draw afterimages when charging.
            if (npc.ai[0] == 2f)
            {
                for (int i = npc.oldPos.Length - 1; i >= 0; i--)
                {
                    Vector2 drawPos = Vector2.Lerp(npc.oldPos[i], npc.position, 0.3f) + npc.Size * 0.5f - Main.screenPosition;
                    Color color = npc.GetAlpha(Color.Red with { A = 0 }) * ((float)(npc.oldPos.Length - i) / npc.oldPos.Length);
                    Main.spriteBatch.Draw(texture, drawPos, null, color, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, drawPosition, null, npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.6f)), npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }
    }
}
