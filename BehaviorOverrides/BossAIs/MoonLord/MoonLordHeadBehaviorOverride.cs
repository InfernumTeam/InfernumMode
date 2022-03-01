using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.MoonLordHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            // Disappear if the body is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[3]) || !Main.npc[(int)npc.ai[3]].active)
            {
                npc.active = false;
                return false;
            }

            // Define the core NPC and inherit properties from it.
            NPC core = Main.npc[(int)npc.ai[3]];

            npc.target = core.target;
            npc.dontTakeDamage = false;

            float attackTimer = core.ai[1];
            Player target = Main.player[npc.target];
            ref float pupilRotation = ref npc.localAI[0];
            ref float pupilOutwardness = ref npc.localAI[1];
            ref float pupilScale = ref npc.localAI[2];
            ref float eyeAnimationFrameCounter = ref npc.localAI[3];

            int idealFrame = 0;

            // Glue the head above the body.
            npc.velocity = Vector2.Zero;
            npc.Center = core.Center - Vector2.UnitY * 400f;

            switch ((MoonLordCoreBehaviorOverride.MoonLordAttackState)(int)core.ai[0])
            {
                case MoonLordCoreBehaviorOverride.MoonLordAttackState.PhantasmalBoltEyeBursts:
                    DoBehavior_PhantasmalBoltEyeBursts(npc, core, target, attackTimer, ref pupilRotation, ref pupilOutwardness, ref pupilScale, ref idealFrame);
                    break;
                default:
                    pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0f, 0.125f);
                    pupilRotation = pupilRotation.AngleLerp(0f, 0.2f);
                    idealFrame = 3;
                    npc.dontTakeDamage = true;
                    break;
            }

            // Handle frames.
            int idealFrameCounter = idealFrame * 5;
            if (idealFrameCounter > eyeAnimationFrameCounter)
                eyeAnimationFrameCounter += 1f;
            if (idealFrameCounter < eyeAnimationFrameCounter)
                eyeAnimationFrameCounter -= 1f;
            eyeAnimationFrameCounter = MathHelper.Clamp((float)eyeAnimationFrameCounter, 0f, 15f);

            return false;
        }

        public static void DoBehavior_PhantasmalBoltEyeBursts(NPC npc, NPC core, Player target, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {
            int initialShootDelay = 72;
            int boltBurstCount = 8;
            int boltShootDelay = 32;
            int circularSpreadBoltCount = 12;
            int randomBurstBoltCount = 6;
            float boltShootSpeed = 5f;
            float wrappedAttackTimer = (attackTimer - initialShootDelay) % boltShootDelay;

            // Have a small delay prior to attacking.
            if (attackTimer < initialShootDelay)
            {
                idealFrame = 3;
                npc.dontTakeDamage = true;
                return;
            }

            idealFrame = 0;

            Vector2 pupilPosition = npc.Center + Utils.Vector2FromElipse(pupilRotation.ToRotationVector2(), new Vector2(27f, 59f) * pupilOutwardness);

            // Create dust telegraphs prior to firing.
            if (wrappedAttackTimer < boltShootDelay * 0.7f)
            {
                int dustCount = (int)MathHelper.Lerp(1f, 4f, attackTimer / boltShootDelay / 0.7f);
                for (int i = 0; i < dustCount; i++)
                {
                    if (!Main.rand.NextBool(24))
                        continue;

                    Vector2 dustMoveDirection = Main.rand.NextVector2Unit();
                    Vector2 dustSpawnPosition = pupilPosition + dustMoveDirection * 8f;
                    Dust electricity = Dust.NewDustPerfect(dustSpawnPosition, 267);
                    electricity.color = Color.Lerp(Color.Cyan, Color.Wheat, Main.rand.NextFloat());
                    electricity.velocity = dustMoveDirection * 3.6f;
                    electricity.scale = 1.25f;
                    electricity.noGravity = true;

                    if (dustCount >= 3)
                        electricity.scale *= 1.5f;
                }
            }

            float pupilDilationInterpolant = Utils.InverseLerp(0f, 0.7f, attackTimer, true) * 0.5f + Utils.InverseLerp(0.7f, 1f, attackTimer, true) * 0.5f;
            pupilRotation = pupilRotation.AngleLerp(npc.AngleTo(target.Center), 0.1f);
            pupilScale = MathHelper.Lerp(0.3f, 0.75f, pupilDilationInterpolant);
            pupilOutwardness = MathHelper.Lerp(pupilOutwardness, 0.65f, 0.1f);

            // Create a burst of phantasmal bolts after the telegraph completes.
            if (wrappedAttackTimer == boltShootDelay - 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float circularSpreadOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < circularSpreadBoltCount; i++)
                    {
                        Vector2 boltShootVelocity = (MathHelper.TwoPi * i / circularSpreadBoltCount + circularSpreadOffsetAngle).ToRotationVector2() * boltShootSpeed;
                        Utilities.NewProjectileBetter(pupilPosition, boltShootVelocity, ProjectileID.PhantasmalBolt, 200, 0f);
                    }

                    for (int i = 0; i < randomBurstBoltCount; i++)
                    {
                        Vector2 boltShootVelocity = npc.SafeDirectionTo(target.Center) * boltShootSpeed * 1.4f;
                        boltShootVelocity += Main.rand.NextVector2Circular(2.4f, 2.4f);
                        Utilities.NewProjectileBetter(pupilPosition, boltShootVelocity, ProjectileID.PhantasmalBolt, 200, 0f);
                    }
                }
            }

            if (attackTimer >= boltShootDelay * boltBurstCount)
                core.Infernum().ExtraAI[0] = 1f;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D headTexture = Main.npcTexture[npc.type];
            Texture2D headGlowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/MoonLord/MoonLordHeadGlowmask");
            Vector2 headOrigin = new Vector2(191f, 130f);
            Texture2D eyeScleraTexture = Main.extraTexture[18];
            Texture2D pupilTexture = Main.extraTexture[19];
            Vector2 mouthOrigin = new Vector2(19f, 34f);
            Texture2D mouthTexture = Main.extraTexture[25];
            Vector2 mouthOffset = new Vector2(0f, 214f).RotatedBy(npc.rotation);
            Rectangle mouthFrame = mouthTexture.Frame(1, 1, 0, 0);
            mouthFrame.Height /= 3;
            mouthFrame.Y += mouthFrame.Height * (int)(npc.localAI[2] / 7f);
            Texture2D eyeTexture = Main.extraTexture[29];
            Vector2 eyeOffset = new Vector2(0f, 4f).RotatedBy(npc.rotation);
            Rectangle eyeFrame = eyeTexture.Frame(1, 1, 0, 0);
            eyeFrame.Height /= 4;
            eyeFrame.Y += eyeFrame.Height * (int)(npc.localAI[3] / 5f);
            Texture2D mouthOutlineTexture = Main.extraTexture[26];
            Rectangle mouthOuterFrame = mouthOutlineTexture.Frame(1, 1, 0, 0);
            mouthOuterFrame.Height /= 4;
            Point centerTileCoords = npc.Center.ToTileCoordinates();
            Color color = npc.GetAlpha(Color.Lerp(Lighting.GetColor(centerTileCoords.X, centerTileCoords.Y), Color.White, 0.3f));
            if (npc.ai[0] < 0f)
            {
                mouthOuterFrame.Y += mouthOuterFrame.Height * (int)(npc.ai[1] / 8f);
                spriteBatch.Draw(mouthOutlineTexture, npc.Center - Main.screenPosition, mouthOuterFrame, color, npc.rotation, mouthOrigin + new Vector2(4f, 4f), 1f, 0, 0f);
            }
            else
            {
                spriteBatch.Draw(eyeScleraTexture, npc.Center - Main.screenPosition, null, Color.White * npc.Opacity * 0.6f, npc.rotation, mouthOrigin, 1f, 0, 0f);
                Vector2 pupilOffset = Utils.Vector2FromElipse(npc.localAI[0].ToRotationVector2(), new Vector2(27f, 59f) * npc.localAI[1]);
                spriteBatch.Draw(pupilTexture, npc.Center - Main.screenPosition + pupilOffset, null, Color.White * npc.Opacity * 0.6f, npc.rotation, pupilTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(headTexture, npc.Center - Main.screenPosition, npc.frame, color, npc.rotation, headOrigin, 1f, 0, 0f);
            spriteBatch.Draw(headGlowmask, npc.Center - Main.screenPosition, npc.frame, Color.White * npc.Opacity * 0.6f, npc.rotation, headOrigin, 1f, 0, 0f);
            spriteBatch.Draw(eyeTexture, (npc.Center - Main.screenPosition + eyeOffset).Floor(), eyeFrame, color, npc.rotation, eyeFrame.Size() / 2f, 1f, 0, 0f);
            spriteBatch.Draw(mouthTexture, (npc.Center - Main.screenPosition + mouthOffset).Floor(), mouthFrame, color, npc.rotation, mouthFrame.Size() / 2f, 1f, 0, 0f);
            return false;
        }
    }
}
