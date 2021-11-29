using CalamityMod.Events;
using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.BehaviorOverrides.BossAIs.KingSlime
{
	public class KingSlimeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.KingSlime;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        public enum KingSlimeAttackType
        {
            SmallJump,
            LargeJump,
            SlamJump,
            Teleport,
        }
		#endregion

		#region AI

		internal static readonly KingSlimeAttackType[] AttackPattern = new KingSlimeAttackType[]
        {
            KingSlimeAttackType.SmallJump,
            KingSlimeAttackType.SmallJump,
            KingSlimeAttackType.LargeJump,
            KingSlimeAttackType.Teleport,
            KingSlimeAttackType.LargeJump,
        };

        public override bool PreAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;
            npc.TargetClosest();

            ref float attackTimer = ref npc.ai[2];
            ref float hasSummonedNinjaFlag = ref npc.localAI[0];
            ref float hasSummonedJewelFlag = ref npc.localAI[1];
            ref float teleportDirection = ref npc.Infernum().ExtraAI[6];

            bool shouldNotChangeScale = false;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (!Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, 4700f))
            {
                npc.TargetClosest();
                if (!Main.player[npc.target].active || Main.player[npc.target].dead)
                {
                    npc.velocity.X *= 0.8f;
                    if (Math.Abs(npc.velocity.X) < 0.1f)
                        npc.velocity.X = 0f;

                    npc.dontTakeDamage = true;
                    npc.damage = 0;

                    // Release slime dust to accompany the teleport
                    for (int i = 0; i < 30; i++)
                    {
                        Dust slime = Dust.NewDustDirect(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, 4, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                        slime.noGravity = true;
                        slime.velocity *= 0.5f;
                    }

                    npc.scale *= 0.97f;
                    if (npc.timeLeft > 30)
                        npc.timeLeft = 30;
                    npc.position.X += npc.width / 2;
                    npc.position.Y += npc.height / 2;
                    npc.width = (int)(108f * npc.scale);
                    npc.height = (int)(88f * npc.scale);
                    npc.position.X -= npc.width / 2;
                    npc.position.Y -= npc.height / 2;

                    if (npc.scale < 0.7f || !npc.WithinRange(Main.player[npc.target].Center, 4700f))
                    {
                        npc.active = false;
                        npc.netUpdate = true;
                    }
                    return false;
                }
            }
            else
                npc.timeLeft = 3600;

            float oldScale = npc.scale;
            float idealScale = MathHelper.Lerp(1.85f, 3f, lifeRatio);
            npc.scale = idealScale;

            if (npc.localAI[2] == 0f)
            {
                npc.timeLeft = 3600;
                npc.localAI[2] = 1f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && npc.life < npc.lifeMax * 0.3f && hasSummonedNinjaFlag == 0f)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Ninja>());
                hasSummonedNinjaFlag = 1f;
            }

            if (npc.life < npc.lifeMax * 0.75f && hasSummonedJewelFlag == 0f && npc.scale >= 0.8f)
            {
                Vector2 jewelSpawnPosition = target.Center - Vector2.UnitY * 350f;
                Main.PlaySound(SoundID.Item67, target.Center);
                Dust.QuickDustLine(npc.Top + Vector2.UnitY * 60f, jewelSpawnPosition, 150f, Color.Red);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.NewNPC((int)jewelSpawnPosition.X, (int)jewelSpawnPosition.Y, ModContent.NPCType<KingSlimeJewel>());
                hasSummonedJewelFlag = 1f;
            }

            void goToNextAIState()
            {
                // You cannot use ref locals inside of a delegate context.
                // You should be able to find most important, universal locals above, anyway.
                // Any others that don't have an explicit reference above are exclusively for
                // AI state manipulation.

                npc.ai[3]++;

                KingSlimeAttackType[] patternToUse = AttackPattern;
                KingSlimeAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

                // Going to the next AI state.
                npc.ai[1] = (int)nextAttackType;

                // Resetting the attack timer.
                npc.ai[2] = 0f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                    npc.Infernum().ExtraAI[i] = 0f;

                if (npc.velocity.Y < 0f)
                    npc.velocity.Y = 0f;
                npc.netUpdate = true;
            }

            // Enforce slightly stronger gravity.
            if (npc.velocity.Y > 0f)
            {
                npc.velocity.Y += MathHelper.Lerp(0.05f, 0.25f, 1f - lifeRatio);
                if (BossRushEvent.BossRushActive && npc.velocity.Y > 4f)
                    npc.position.Y += 4f;
            }

            switch ((KingSlimeAttackType)(int)npc.ai[1])
            {
                case KingSlimeAttackType.SmallJump:
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity.X *= 0.8f;
                        if (Math.Abs(npc.velocity.X) < 0.1f)
                            npc.velocity.X = 0f;

                        if (attackTimer == 25f && npc.collideY)
                        {
                            npc.TargetClosest();
                            target = Main.player[npc.target];
                            float jumpSpeed = MathHelper.Lerp(8.25f, 11.6f, Utils.InverseLerp(40f, 700f, Math.Abs(target.Center.Y - npc.Center.Y), true));
                            jumpSpeed *= Main.rand.NextFloat(1f, 1.15f);

                            npc.velocity = new Vector2(npc.direction * 8.5f, -jumpSpeed);
                            if (BossRushEvent.BossRushActive)
                                npc.velocity *= 2.4f;

                            npc.netUpdate = true;
                        }

                        if (attackTimer > 25f && (npc.collideY || attackTimer >= 180f))
                            goToNextAIState();
                    }
                    else
                        attackTimer--;
                    break;
                case KingSlimeAttackType.LargeJump:
                    if (npc.velocity.Y == 0f)
                    {
                        npc.velocity.X *= 0.8f;
                        if (Math.Abs(npc.velocity.X) < 0.1f)
                            npc.velocity.X = 0f;

                        if (attackTimer == 35f)
                        {
                            npc.TargetClosest();
                            target = Main.player[npc.target];
                            float jumpSpeed = MathHelper.Lerp(10f, 23f, Utils.InverseLerp(40f, 360f, Math.Abs(target.Center.Y - npc.Center.Y), true));
                            jumpSpeed *= Main.rand.NextFloat(1f, 1.15f);

                            npc.velocity = new Vector2(npc.direction * 10.25f, -jumpSpeed);
                            if (BossRushEvent.BossRushActive)
                                npc.velocity *= 1.5f;
                            npc.netUpdate = true;
                        }

                        if (attackTimer > 35f && (npc.collideY || attackTimer >= 180f))
                            goToNextAIState();
                    }
                    else
                        attackTimer--;
                    break;
                case KingSlimeAttackType.Teleport:
                    int digTime = 60;
                    int reappearTime = 30;

                    ref float digXPosition = ref npc.Infernum().ExtraAI[0];
                    ref float digYPosition = ref npc.Infernum().ExtraAI[1];

                    if (attackTimer < digTime)
                    {
                        npc.velocity.X *= 0.8f;
                        if (Math.Abs(npc.velocity.X) < 0.1f)
                            npc.velocity.X = 0f;

                        npc.scale = MathHelper.Lerp(idealScale, 0.2f, MathHelper.Clamp((float)Math.Pow(attackTimer / digTime, 3D), 0f, 1f));
                        npc.Opacity = Utils.InverseLerp(0.7f, 1f, npc.scale, true) * 0.7f;
                        npc.dontTakeDamage = true;
                        npc.damage = 0;

                        // Release slime dust to accompany the teleport
                        for (int i = 0; i < 30; i++)
                        {
                            Dust slime = Dust.NewDustDirect(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, 4, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                            slime.noGravity = true;
                            slime.velocity *= 0.5f;
                        }
                    }

                    if (attackTimer == digTime)
                    {
                        if (teleportDirection == 0f)
                            teleportDirection = 1f;

                        digXPosition = target.Center.X + 600f * teleportDirection;
                        digYPosition = target.Top.Y - 800f;
                        if (digYPosition < 100f)
                            digYPosition = 100f;

                        Gore.NewGore(npc.Center + new Vector2(-40f, npc.height * -0.5f), npc.velocity, 734, 1f);
                        WorldUtils.Find(new Vector2(digXPosition, digYPosition).ToTileCoordinates(), Searches.Chain(new Searches.Down(200), new GenCondition[]
                        {
                            new Conditions.IsSolid(),
                            new CustomTileConditions.ActiveAndNotActuated()
                        }), out Point newBottom);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            npc.Bottom = newBottom.ToWorldCoordinates(8, -16);
                            teleportDirection *= -1f;
                            npc.netUpdate = true;
                        }
                        npc.Opacity = 0.7f;
                    }

                    if (attackTimer > digTime && attackTimer <= digTime + reappearTime)
                    {
                        npc.scale = MathHelper.Lerp(0.2f, idealScale, Utils.InverseLerp(digTime, digTime + reappearTime, attackTimer, true));
                        npc.Opacity = 0.7f;
                        npc.dontTakeDamage = true;
                        npc.damage = 0;
                    }

                    if (attackTimer > digTime + reappearTime + 25)
                        goToNextAIState();
                    break;
            }

            if (!shouldNotChangeScale && oldScale != npc.scale)
            {
                npc.position = npc.Bottom;
                npc.width = (int)(108f * npc.scale);
                npc.height = (int)(88f * npc.scale);
                npc.Bottom = npc.position;
            }

            if (npc.Opacity > 0.7f)
                npc.Opacity = 0.7f;

            npc.gfxOffY = (int)(-42 * npc.scale / 3f);

            attackTimer++;
            return false;
        }

        #endregion AI

        #region Draw Code

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D kingSlimeTexture = Main.npcTexture[npc.type];
            Vector2 kingSlimeDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;

            // Draw the ninja, if it's still stuck.
            if (npc.life > npc.lifeMax * 0.3f)
            {
                Vector2 drawOffset = Vector2.Zero;
                float ninjaRotation = npc.velocity.X * 0.05f;
                drawOffset.Y -= npc.velocity.Y;
                drawOffset.X -= npc.velocity.X * 2f;
                if (npc.frame.Y == 120)
                    drawOffset.Y += 2f;
                if (npc.frame.Y == 360)
                    drawOffset.Y -= 2f;
                if (npc.frame.Y == 480)
                    drawOffset.Y -= 6f;

                Vector2 ninjaDrawPosition = npc.Center - Main.screenPosition + drawOffset;
                spriteBatch.Draw(Main.ninjaTexture, ninjaDrawPosition, null, lightColor, ninjaRotation, Main.ninjaTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(kingSlimeTexture, kingSlimeDrawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);

            float verticalCrownOffset = 0f;
            switch (npc.frame.Y / (Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type]))
            {
                case 0:
                    verticalCrownOffset = 2f;
                    break;
                case 1:
                    verticalCrownOffset = -6f;
                    break;
                case 2:
                    verticalCrownOffset = 2f;
                    break;
                case 3:
                    verticalCrownOffset = 10f;
                    break;
                case 4:
                    verticalCrownOffset = 2f;
                    break;
                case 5:
                    verticalCrownOffset = 0f;
                    break;
            }
            Texture2D crownTexture = Main.extraTexture[39];
            Vector2 crownDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * (npc.gfxOffY - (56f - verticalCrownOffset) * npc.scale);
            spriteBatch.Draw(crownTexture, crownDrawPosition, null, lightColor, 0f, crownTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            return false;
        }
        #endregion Drawcode
    }
}