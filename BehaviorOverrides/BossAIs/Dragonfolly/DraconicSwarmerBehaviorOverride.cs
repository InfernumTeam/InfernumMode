using CalamityMod.CalPlayer;
using CalamityMod.Events;
using CalamityMod.NPCs.Bumblebirb;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class DraconicSwarmerBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<Bumblefuck2>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        public enum DragonfollyAttackType
        {
            SpawnEffects,
            FeatherSpreadRelease,
            OrdinaryCharge,
            FakeoutCharge,
            ThunderCharge,
            SummonSwarmers,
            NormalLightningAura,
            PlasmaBursts,
            LightningSupercharge
        }

        public enum DragonfollyFrameDrawingType
        {
            FlapWings,
            Screm
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<Bumblefuck>()))
                return true;

            npc.damage = CalamityPlayer.areThereAnyDamnBosses ? npc.defDamage : (int)(npc.defDamage * 0.8);

            Player target = Main.player[npc.target];

            bool duringFollyFight = CalamityPlayer.areThereAnyDamnBosses;

            bool inPhase2 = npc.ai[3] == 1f;
            bool inPhase3 = npc.ai[3] == 2f;
            if (inPhase3)
                npc.damage = (int)(npc.defDamage * 1.4);
            else if (inPhase2)
                npc.damage = (int)(npc.defDamage * 1.25);

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float fadeToRed = ref npc.localAI[0];
            ref float deathTimer = ref npc.Infernum().ExtraAI[0];

            // Begin the redshift phase in phase 2 if 15 seconds have passed.
            if (inPhase2)
            {
                deathTimer++;
                if (npc.ai[0] != 3f && deathTimer >= 900f)
                {
                    npc.ai[0] = 3f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
            }

            // Despawn immediately if super far from the target.
            if (!npc.WithinRange(target.Center, 5600f))
            {
                if (npc.timeLeft > 5)
                    npc.timeLeft = 5;
            }

            npc.noTileCollide = false;
            npc.noGravity = true;

            npc.rotation = (npc.rotation * 4f + npc.velocity.X * 0.04f * 1.25f) / 10f;

            // Repel from other swarmers.
            if (attackState == 0f || attackState == 1f)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (i == npc.whoAmI || !Main.npc[i].active || Main.npc[i].type != npc.type)
                        continue;

                    if (Main.npc[i].WithinRange(npc.Center, npc.width + npc.height))
                    {
                        Vector2 repelVelocity = npc.SafeDirectionTo(Main.npc[i].Center) * -0.1f;

                        npc.velocity += repelVelocity;
                        Main.npc[i].velocity -= repelVelocity;
                    }
                }
            }

            // Attempt to find a new target. If no target is found or it's too far away, fly away.
            if (npc.target < 0 || target.dead || !target.active)
            {
                npc.TargetClosest(true);
                target = Main.player[npc.target];
                if (target.dead || !npc.WithinRange(target.Center, duringFollyFight ? 4600f : 2800f))
                    attackState = -1f;
            }
            else
            {
                if (attackState > 1f && !npc.WithinRange(target.Center, 3600f))
                    attackState = 1f;
            }

            // Fly upward and despawn.
            if (attackState == -1f)
            {
                npc.velocity = (npc.velocity * 9f + Vector2.UnitY * -16f) / 10f;
                npc.noTileCollide = true;
                npc.dontTakeDamage = true;
                if (npc.timeLeft > 240)
                    npc.timeLeft = 240;
                return false;
            }

            // Search for a player to target.
            if (attackState == 0f)
            {
                npc.TargetClosest(true);
                target = Main.player[npc.target];
                npc.spriteDirection = npc.direction;

                // Rebound and clamp movement on tile collision.
                if (npc.collideX)
                {
                    npc.velocity.X *= -npc.oldVelocity.X * 0.5f;
                    npc.velocity.X = MathHelper.Clamp(npc.velocity.X, -4f, 4f);
                }
                if (npc.collideY)
                {
                    npc.velocity.Y *= -npc.oldVelocity.Y * 0.5f;
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y, -4f, 4f);
                }

                // If the player is very far away, go to a different attack.
                if (!npc.WithinRange(target.Center, 2800f))
                {
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.ai[2] = 0f;
                }
                // Otherwise fly towards the destination if relatively far from it.
                else if (!npc.WithinRange(target.Center, 400f))
                {
                    float distanceFromPlayer = npc.Distance(target.Center);
                    float flySpeed = (duringFollyFight ? 9f : 7f) + distanceFromPlayer / 100f + attackTimer / 15f;
                    float flyInertia = 30f;
                    if (BossRushEvent.BossRushActive)
                        flySpeed += 12f;

                    npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(target.Center) * flySpeed) / flyInertia;
                }
                else if (npc.velocity.Length() > 2f)
                    npc.velocity *= 0.95f;
                else if (npc.velocity.Length() < 1f)
                    npc.velocity *= 1.05f;

                attackTimer++;
                if (attackTimer >= (duringFollyFight ? 90f : 105f))
                {
                    attackTimer = 0f;
                    attackState = 2f;
                }
            }
            else if (npc.ai[0] == 1f)
            {
                npc.collideX = false;
                npc.collideY = false;
                npc.noTileCollide = true;
                if (npc.target < 0 || !Main.player[npc.target].active || Main.player[npc.target].dead)
                {
                    npc.TargetClosest(true);
                    target = Main.player[npc.target];
                }

                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;
                npc.rotation = (npc.rotation * 4f + npc.velocity.X * 0.04f) / 10f;

                // If somewhat close to the player and not stuck, go back to picking an attack.
                if (npc.WithinRange(target.Center, 800f) && !Collision.SolidCollision(npc.position, npc.width, npc.height))
                {
                    attackState = 0f;
                    attackTimer = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
                npc.ai[2] += 0.0166666675f;
                float flyInertia = 25f;
                float baseFlySpeed = duringFollyFight ? 12f : 9f;
                if (BossRushEvent.BossRushActive)
                    baseFlySpeed += 10f;
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * (baseFlySpeed + npc.ai[2] + npc.Distance(target.Center) / 150f);
                npc.velocity = (npc.velocity * (flyInertia - 1f) + idealVelocity) / flyInertia;
                return false;
            }
            else if (attackState == 2f)
            {
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;
                npc.rotation = (npc.rotation * 4f * 0.75f + npc.velocity.X * 0.04f * 1.25f) / 8f;
                npc.noTileCollide = true;

                // Line up for a charge for a short amount of time.
                float flyInertia = 8f;
                Vector2 idealFlyVelocity = npc.SafeDirectionTo(target.Center) * (duringFollyFight ? 20.5f : 14f);
                if (BossRushEvent.BossRushActive)
                    idealFlyVelocity *= 1.6f;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + idealFlyVelocity) / flyInertia;
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;

                attackTimer++;
                int chargeDelay = inPhase2 ? 17 : 10;

                // And perform the charge.
                if (attackTimer > chargeDelay)
                {
                    npc.velocity = idealFlyVelocity;
                    npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                    attackState = 2.1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }
            else if (attackState == 2.1f)
            {
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;
                npc.velocity *= 1.01f;
                npc.noTileCollide = true;
                if (attackTimer > 30f)
                {
                    // If not stuck, just go back to picking a different attack.
                    if (!Collision.SolidCollision(npc.position, npc.width, npc.height))
                    {
                        attackState = 0f;
                        attackTimer = 0f;
                        npc.ai[2] = 0f;
                        return false;
                    }

                    // Otherwise, if stuck, wait for a little more time. If still stuck after that, 
                    // do the idle search attack.
                    if (attackTimer > 60f)
                    {
                        attackState = 1f;
                        attackTimer = 0f;
                        npc.ai[2] = 0f;
                    }

                    npc.netUpdate = true;
                }
                attackTimer++;
            }
            else if (attackState == 3f)
            {
                npc.noTileCollide = true;
                npc.direction = (npc.velocity.X > 0f).ToDirectionInt();
                npc.spriteDirection = npc.direction;

                int fadeTime = 35;
                int chargeDelay = 15;
                int chargeTime = 90;
                float flyInertia = 29f;
                float chargeSpeed = 31f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;

                // Try to go towards the player and charge, while fading red.
                fadeToRed = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, fadeTime, attackTimer, true));
                if (attackTimer >= fadeTime && attackTimer <= fadeTime + chargeDelay)
                {
                    npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;
                    if (attackTimer == fadeTime + chargeDelay)
                    {
                        npc.velocity = chargeVelocity;
                        npc.netUpdate = true;
                    }
                }

                // After charging for a certain amount of time, fade out of existance.
                if (attackTimer >= fadeTime + chargeDelay + chargeTime)
                    npc.Opacity = MathHelper.Lerp(npc.Opacity, 0f, 0.08f);

                if (npc.Opacity < 0.425f)
                {
                    npc.active = false;
                    npc.netUpdate = true;
                }
                // Release lightning clouds when charging if in phase 3.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > fadeTime + chargeDelay && attackTimer % 7f == 6f && inPhase3)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<VolatileLightning>(), 0, 0f);

                attackTimer++;
            }
            return false;
        }

        #endregion AI

        #region Frames and Drawcode

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (!NPC.AnyNPCs(ModContent.NPCType<Bumblefuck>()))
                return true;

            float fadeToRed = npc.localAI[0];
            float backgroundFadeToRed = 0f;
            int follyIndex = NPC.FindFirstNPC(ModContent.NPCType<Bumblefuck>());
            if (Main.npc.IndexInRange(follyIndex))
                backgroundFadeToRed = Main.npc[follyIndex].Infernum().ExtraAI[8];
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            int drawInstances = (int)MathHelper.Lerp(1f, 4f, fadeToRed);
            Color drawColor = Color.Lerp(lightColor, Color.Red * 0.9f, fadeToRed);
            drawColor = Color.Lerp(drawColor, Color.White, backgroundFadeToRed * 0.9f);
            drawColor *= MathHelper.Lerp(1f, 0.4f, fadeToRed);
            if (fadeToRed > 0.4f)
                drawColor.A = 0;

            Vector2 origin = npc.frame.Size() * 0.5f;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            void drawInstance(Vector2 baseDrawPosition, float scale, float opacity)
            {
                for (int i = 0; i < drawInstances; i++)
                {
                    Vector2 drawPosition = baseDrawPosition - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    if (fadeToRed > 0.4f)
                        drawPosition += (MathHelper.TwoPi * i / drawInstances + Main.GlobalTimeWrappedHourly * 5f).ToRotationVector2() * 2.5f;
                    spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(drawColor) * opacity, npc.rotation, origin, scale, spriteEffects, 0f);
                }
            }

            drawInstance(npc.Center, npc.scale, npc.Opacity);
            return false;
        }
        #endregion
    }
}
