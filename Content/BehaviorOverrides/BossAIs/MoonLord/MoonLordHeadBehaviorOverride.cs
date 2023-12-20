using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Sounds;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord.MoonLordCoreBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.MoonLordHead;

        public override int? NPCTypeToDeferToForTips => NPCID.MoonLordCore;

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

            // Hacky workaround to problems with popping.
            // The system regarding ML's death is held together with duct tape, broken promises, and three daily prayers. Do not question it, for your own safety.
            if (npc.life < npc.lifeMax * 0.18)
                npc.life = (int)(npc.lifeMax * 0.18);

            npc.target = core.target;
            npc.dontTakeDamage = false;

            float attackTimer = core.ai[1];
            bool hasPopped = npc.ai[0] == -2f;
            Player target = Main.player[npc.target];
            ref float pupilRotation = ref npc.localAI[0];
            ref float pupilOutwardness = ref npc.localAI[1];
            ref float pupilScale = ref npc.localAI[2];
            ref float eyeAnimationFrameCounter = ref npc.localAI[3];
            ref float leechCreationCounter = ref npc.Infernum().ExtraAI[5];
            ref float mouthFrame = ref npc.Infernum().ExtraAI[6];

            int idealFrame = 0;

            // Glue the head above the body.
            npc.velocity = Vector2.Zero;
            npc.Center = core.Center - Vector2.UnitY * 400f;

            switch ((MoonLordAttackState)(int)core.ai[0])
            {
                // Have the head use the exposed eye frames, not take damage, and rotate in such a way that it looks like the neck was snapped when dying.
                case MoonLordAttackState.DeathEffects:
                    idealFrame = 3;
                    npc.dontTakeDamage = true;
                    npc.rotation = npc.rotation.AngleLerp(Pi / 12f, 0.1f);
                    break;
                case MoonLordAttackState.PhantasmalBoltEyeBursts:
                    DoBehavior_PhantasmalBoltEyeBursts(npc, core, target, attackTimer, ref pupilRotation, ref pupilOutwardness, ref pupilScale, ref idealFrame);
                    break;
                case MoonLordAttackState.PhantasmalDeathrays:
                    DoBehavior_PhantasmalDeathrays(npc, core, target, attackTimer, ref pupilRotation, ref pupilOutwardness, ref pupilScale, ref idealFrame);
                    break;
                default:
                    pupilOutwardness = Lerp(pupilOutwardness, 0f, 0.125f);
                    pupilRotation = pupilRotation.AngleLerp(0f, 0.2f);
                    pupilScale = Lerp(pupilScale, 1f, 0.1f);
                    idealFrame = 3;
                    npc.dontTakeDamage = true;
                    break;
            }

            if (hasPopped)
            {
                idealFrame = 0;
                npc.life = 1;
                npc.dontTakeDamage = true;
            }

            // Idly create the leech from the mouth.
            if (!Utilities.AnyProjectiles(ProjectileID.MoonLeech))
            {
                leechCreationCounter++;
                mouthFrame = (int)Math.Round(Utils.GetLerpValue(270f, 290f, leechCreationCounter, true) * 2f);
            }
            else
                mouthFrame = 2f;

            if (leechCreationCounter >= 300f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 mouthPosition = npc.Center + Vector2.UnitY * 216f;
                    Vector2 leechVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY) * 7f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), mouthPosition, leechVelocity, ProjectileID.MoonLeech, 0, 0f, Main.myPlayer, npc.whoAmI + 1, npc.target);
                }
                leechCreationCounter = 0f;
                npc.netUpdate = true;
            }

            // Handle frames.
            int idealFrameCounter = idealFrame * 5;
            if (idealFrameCounter > eyeAnimationFrameCounter)
                eyeAnimationFrameCounter += 1f;
            if (idealFrameCounter < eyeAnimationFrameCounter)
                eyeAnimationFrameCounter -= 1f;
            eyeAnimationFrameCounter = Clamp((float)eyeAnimationFrameCounter, 0f, 15f);

            return false;
        }

        public static void DoBehavior_PhantasmalBoltEyeBursts(NPC npc, NPC core, Player target, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {
            int initialShootDelay = 72;
            int boltBurstCount = 8;
            int boltShootDelay = 32;
            int circularSpreadBoltCount = 12;
            int randomBurstBoltCount = 6;
            float boltShootSpeed = 4.25f;
            if (IsEnraged)
            {
                boltShootDelay -= 14;
                boltShootSpeed += 5f;
                circularSpreadBoltCount += 8;
                randomBurstBoltCount += 7;
            }

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
                int dustCount = (int)Lerp(1f, 4f, attackTimer / boltShootDelay / 0.7f);
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

            // Calculate pupil variables.
            float pupilDilationInterpolant = Utils.GetLerpValue(0f, 0.7f, attackTimer, true) * 0.5f + Utils.GetLerpValue(0.7f, 1f, attackTimer, true) * 0.5f;
            pupilRotation = pupilRotation.AngleLerp(npc.AngleTo(target.Center), 0.1f);
            pupilScale = Lerp(0.4f, 1f, pupilDilationInterpolant);
            pupilOutwardness = Lerp(pupilOutwardness, 0.65f, 0.1f);

            // Create a burst of phantasmal bolts after the telegraph completes.
            if (wrappedAttackTimer == boltShootDelay - 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float circularSpreadOffsetAngle = Main.rand.NextFloat(TwoPi);
                    for (int i = 0; i < circularSpreadBoltCount; i++)
                    {
                        Vector2 boltShootVelocity = (TwoPi * i / circularSpreadBoltCount + circularSpreadOffsetAngle).ToRotationVector2() * boltShootSpeed;
                        Utilities.NewProjectileBetter(pupilPosition, boltShootVelocity, ProjectileID.PhantasmalBolt, PhantasmalBoltDamage, 0f);
                    }

                    for (int i = 0; i < randomBurstBoltCount; i++)
                    {
                        Vector2 boltShootVelocity = npc.SafeDirectionTo(target.Center) * boltShootSpeed * Main.rand.NextFloat(1.4f, 1.55f);
                        boltShootVelocity += Main.rand.NextVector2Circular(1.9f, 1.9f);
                        Utilities.NewProjectileBetter(pupilPosition, boltShootVelocity, ProjectileID.PhantasmalBolt, PhantasmalBoltDamage, 0f);
                    }
                }
            }

            if (attackTimer >= boltShootDelay * boltBurstCount || !EyeIsActive)
                core.Infernum().ExtraAI[5] = 1f;
        }

        public static void DoBehavior_PhantasmalDeathrays(NPC npc, NPC core, Player target, float attackTimer, ref float pupilRotation, ref float pupilOutwardness, ref float pupilScale, ref int idealFrame)
        {
            int idealDeathrayTelegraphTime = 110;
            int idealDeathrayLifetime = 90;
            int deathrayShootCount = 3;
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float angularOffset = ref npc.Infernum().ExtraAI[1];
            ref float deathrayTelegraphTime = ref npc.Infernum().ExtraAI[2];
            ref float deathrayLifetime = ref npc.Infernum().ExtraAI[3];

            if (IsEnraged)
            {
                idealDeathrayTelegraphTime -= 45;
                idealDeathrayLifetime -= 25;
            }
            if (deathrayTelegraphTime == 0f || deathrayLifetime == 0f)
            {
                deathrayTelegraphTime = idealDeathrayTelegraphTime;
                deathrayLifetime = idealDeathrayLifetime;
                angularOffset = Main.rand.NextFloat(TwoPi);

                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.MoonLordLaserTip");
            }

            float wrappedAttackTimer = attackTimer % (deathrayTelegraphTime + deathrayLifetime);

            idealFrame = 0;

            // Determine the size of the telegraph.
            telegraphInterpolant = 0f;
            if (wrappedAttackTimer < deathrayTelegraphTime)
            {
                if (wrappedAttackTimer == 25f)
                    SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Volume = 1.85f, Pitch = -0.2f }, target.Center);

                telegraphInterpolant = Utils.GetLerpValue(0f, deathrayTelegraphTime, wrappedAttackTimer, true);
                angularOffset += CalamityUtils.Convert01To010(telegraphInterpolant) * TwoPi / 300f;
            }
            else
                core.velocity *= 0.9f;

            // Calculate pupil variables.
            pupilScale = Lerp(0.35f, 1f, Utils.GetLerpValue(0f, deathrayTelegraphTime, wrappedAttackTimer, true));
            pupilRotation = npc.AngleTo(target.Center).AngleLerp(angularOffset, Utils.GetLerpValue(35f, deathrayTelegraphTime * 0.3f, wrappedAttackTimer, true));
            pupilOutwardness = Lerp(pupilOutwardness, 0.4f, 0.1f);
            Vector2 pupilPosition = npc.Center + Utils.Vector2FromElipse(pupilRotation.ToRotationVector2(), new Vector2(27f, 59f) * pupilOutwardness);

            // Fire lasers.
            if (wrappedAttackTimer == deathrayTelegraphTime)
            {
                // Make some strong sounds.
                SoundEngine.PlaySound(CommonCalamitySounds.FlareSound with { Volume = 1.61f }, target.Center);
                SoundEngine.PlaySound(TeslaCannon.FireSound with { Pitch = -0.21f }, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Release a spread of bolts. They do not fire if the target is close to the eye.
                    if (!target.WithinRange(npc.Center, 270f))
                    {
                        float middleRingAngularOffset = Main.rand.NextFloat(TwoPi);
                        for (int i = 0; i < 42; i++)
                        {
                            Vector2 boltVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(TwoPi * i / 42f) * 5.5f;
                            Vector2 middleBoltVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(TwoPi * i / 42f + middleRingAngularOffset) * 3.69f;
                            Utilities.NewProjectileBetter(pupilPosition, boltVelocity, ProjectileID.PhantasmalBolt, PhantasmalBoltDamage, 0f);
                            Utilities.NewProjectileBetter(pupilPosition, middleBoltVelocity, ProjectileID.PhantasmalBolt, PhantasmalBoltDamage, 0f);
                        }
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 beamDirection = (TwoPi * i / 10f + angularOffset).ToRotationVector2();

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(deathray =>
                        {
                            deathray.ModProjectile<PhantasmalDeathray>().InitialRotationalOffset = TwoPi * i / 10f;
                            deathray.ModProjectile<PhantasmalDeathray>().OwnerIndex = npc.whoAmI + 1;
                        });
                        Utilities.NewProjectileBetter(npc.Center, beamDirection, ModContent.ProjectileType<PhantasmalDeathray>(), PhantasmalDeathrayDamage, 0f, -1, 0f, deathrayLifetime);
                    }
                }
            }

            if (attackTimer >= (deathrayTelegraphTime + deathrayLifetime) * deathrayShootCount || !EyeIsActive)
                core.Infernum().ExtraAI[5] = 1f;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D headTexture = TextureAssets.Npc[npc.type].Value;
            Vector2 headOrigin = new(191f, 130f);
            Texture2D eyeScleraTexture = TextureAssets.Extra[18].Value;
            Texture2D pupilTexture = TextureAssets.Extra[19].Value;
            Vector2 mouthOrigin = new(19f, 34f);
            Texture2D mouthTexture = TextureAssets.Extra[25].Value;
            Vector2 mouthOffset = new Vector2(0f, 214f).RotatedBy(npc.rotation);
            Rectangle mouthFrame = mouthTexture.Frame(1, 3, 0, (int)npc.Infernum().ExtraAI[6]);
            Texture2D eyeTexture = TextureAssets.Extra[29].Value;
            Vector2 eyeOffset = new Vector2(0f, 4f).RotatedBy(npc.rotation);
            Rectangle eyeFrame = eyeTexture.Frame(1, 1, 0, 0);
            eyeFrame.Height /= 4;
            eyeFrame.Y += eyeFrame.Height * (int)(npc.localAI[3] / 5f);
            Texture2D mouthOutlineTexture = TextureAssets.Extra[26].Value;
            Rectangle mouthOuterFrame = mouthOutlineTexture.Frame(1, 1, 0, 0);
            mouthOuterFrame.Height /= 4;
            Point centerTileCoords = npc.Center.ToTileCoordinates();
            Color color = npc.GetAlpha(Color.Lerp(Lighting.GetColor(centerTileCoords.X, centerTileCoords.Y), Color.White, 0.3f));

            float pupilRotation = npc.localAI[0];
            float pupilOutwardness = npc.localAI[1];
            Vector2 pupilOffset = Utils.Vector2FromElipse(pupilRotation.ToRotationVector2(), new Vector2(27f, 59f) * pupilOutwardness);

            if (npc.ai[0] < 0f)
            {
                mouthOuterFrame.Y += mouthOuterFrame.Height * (int)(Main.GlobalTimeWrappedHourly * 9.3f % 4);
                Main.spriteBatch.Draw(mouthOutlineTexture, npc.Center - Main.screenPosition, mouthOuterFrame, color, npc.rotation, mouthOrigin + new Vector2(4f, 4f), 1f, 0, 0f);
            }
            else
            {
                Main.spriteBatch.Draw(eyeScleraTexture, npc.Center - Main.screenPosition, null, Color.White * npc.Opacity * 0.7f, npc.rotation, mouthOrigin, 1f, 0, 0f);

                //Main.spriteBatch.SetBlendState(BlendState.Additive);
                Main.spriteBatch.Draw(pupilTexture, npc.Center - Main.screenPosition + pupilOffset, null, Color.White * npc.Opacity, npc.rotation, pupilTexture.Size() * 0.5f, npc.localAI[2], SpriteEffects.None, 0f);
                //Main.spriteBatch.ResetBlendState();
            }
            Main.spriteBatch.Draw(headTexture, npc.Center - Main.screenPosition, npc.frame, color, npc.rotation, headOrigin, 1f, 0, 0f);
            Main.spriteBatch.Draw(eyeTexture, (npc.Center - Main.screenPosition + eyeOffset).Floor(), eyeFrame, color, npc.rotation, eyeFrame.Size() / 2f, 1f, 0, 0f);
            Main.spriteBatch.Draw(mouthTexture, (npc.Center - Main.screenPosition + mouthOffset).Floor(), mouthFrame, color, npc.rotation, mouthFrame.Size() / 2f, 1f, 0, 0f);

            // Draw line telegraphs as necessary.
            NPC core = Main.npc[(int)npc.ai[3]];
            if (core.ai[0] == (int)MoonLordAttackState.PhantasmalDeathrays)
            {
                float lineTelegraphInterpolant = npc.Infernum().ExtraAI[0];

                if (lineTelegraphInterpolant > 0f)
                {
                    var rasterizer = Main.Rasterizer;
                    rasterizer.ScissorTestEnable = true;
                    Main.instance.GraphicsDevice.ScissorRectangle = new(-50, -50, Main.screenWidth + 100, Main.screenHeight + 100);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                    Texture2D line = InfernumTextureRegistry.BloomLineSmall.Value;

                    float angularOffset = npc.Infernum().ExtraAI[1];
                    Color outlineColor = Color.Lerp(Color.Turquoise, Color.White, lineTelegraphInterpolant);
                    Vector2 origin = new(line.Width / 2f, line.Height);
                    Vector2 beamScale = new(lineTelegraphInterpolant * 0.5f, 2.4f);
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 drawPosition = npc.Center + pupilOffset - Main.screenPosition;
                        Vector2 beamDirection = (TwoPi * i / 10f + angularOffset).ToRotationVector2();
                        float beamRotation = beamDirection.ToRotation() - PiOver2;
                        Main.spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);
                    }
                    Main.spriteBatch.ResetBlendState();
                }
            }

            return false;
        }
    }
}
