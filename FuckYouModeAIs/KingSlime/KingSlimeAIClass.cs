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

namespace InfernumMode.FuckYouModeAIs.KingSlime
{
	public class KingSlimeAIClass
    {
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

		#region Main Boss
		internal static readonly KingSlimeAttackType[] AttackPattern = new KingSlimeAttackType[]
        {
            KingSlimeAttackType.SmallJump,
            KingSlimeAttackType.SmallJump,
            KingSlimeAttackType.LargeJump,
            KingSlimeAttackType.Teleport,
            KingSlimeAttackType.LargeJump,
        };

        [OverrideAppliesTo(NPCID.KingSlime, typeof(KingSlimeAIClass), "KingSlimeAI", EntityOverrideContext.NPCAI)]
        public static bool KingSlimeAI(NPC npc)
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

            float oldScale = npc.scale;
            float idealScale = MathHelper.Lerp(1f, 1.7f, lifeRatio);
            npc.scale = idealScale;

            if (Main.netMode != NetmodeID.MultiplayerClient && npc.life < npc.lifeMax * 0.6f && hasSummonedNinjaFlag == 0f)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Ninja>());
                hasSummonedNinjaFlag = 1f;
            }

            if (npc.life < npc.lifeMax * 0.35f && hasSummonedJewelFlag == 0f && npc.scale >= 0.8f)
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
                npc.velocity.Y += MathHelper.Lerp(0.05f, 0.25f, 1f - lifeRatio);

            if (!Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, 2500f))
            {
                npc.TargetClosest();
                if (!Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, 2500f))
                {
                    npc.velocity.X *= 0.8f;
                    if (Math.Abs(npc.velocity.X) < 0.1f)
                        npc.velocity.X = 0f;

                    npc.noTileCollide = true;
                    npc.dontTakeDamage = true;
                    npc.damage = 0;

                    // Release slime dust to accompany the teleport
                    for (int i = 0; i < 30; i++)
                    {
                        Dust slime = Dust.NewDustDirect(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, 4, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                        slime.noGravity = true;
                        slime.velocity *= 0.5f;
                    }

                    if (npc.timeLeft > 30)
                        npc.timeLeft = 30;
                    if (npc.scale < 0.9f)
                    {
                        npc.active = false;
                        npc.netUpdate = true;
                    }
                    return false;
                }
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
                            float jumpSpeed = MathHelper.Lerp(7f, 11.6f, Utils.InverseLerp(40f, 700f, Math.Abs(target.Center.Y - npc.Center.Y), true));
                            jumpSpeed *= Main.rand.NextFloat(1f, 1.15f);

                            npc.velocity = new Vector2(npc.direction * 6f, -jumpSpeed);
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
                            float jumpSpeed = MathHelper.Lerp(9f, 13f, Utils.InverseLerp(40f, 700f, Math.Abs(target.Center.Y - npc.Center.Y), true));
                            jumpSpeed *= Main.rand.NextFloat(1f, 1.15f);

                            npc.velocity = new Vector2(npc.direction * 8f, -jumpSpeed);
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
                        npc.Opacity = Utils.InverseLerp(0.2f, 0.3f, npc.scale, true) * 0.7f;
                        npc.dontTakeDamage = true;
                        shouldNotChangeScale = true;
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
                npc.position.X += npc.width / 2;
                npc.position.Y += npc.height;
                npc.width = (int)(108f * npc.scale);
                npc.height = (int)(88f * npc.scale);
                npc.position.X -= npc.width / 2;
                npc.position.Y -= npc.height;
            }

            if (npc.Opacity > 0.7f)
                npc.Opacity = 0.7f;

            npc.gfxOffY = -6;

            attackTimer++;
            return false;
		}

        #endregion Main Boss

        #region Jewel
        [OverrideAppliesTo("KingSlimeJewel", typeof(KingSlimeAIClass), "JewelAI", EntityOverrideContext.NPCAI)]
        public static bool JewelAI(NPC npc)
        {
            ref float time = ref npc.ai[0];

            // Disappear if the main boss is not present.
            if (!NPC.AnyNPCs(NPCID.KingSlime))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            // Idly emit dust.
            if (Main.rand.NextBool(3))
            {
                Dust shimmer = Dust.NewDustDirect(npc.position, npc.width, npc.height, 264);
                shimmer.color = Color.Red;
                shimmer.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 3f);
                shimmer.velocity -= npc.oldPosition - npc.position;
                shimmer.scale = Main.rand.NextFloat(1f, 1.2f);
                shimmer.fadeIn = 0.4f;
                shimmer.noLight = true;
                shimmer.noGravity = true;
            }

            if (!Main.player.IndexInRange(npc.type) || !Main.player[npc.target].active || Main.player[npc.target].dead)
                npc.TargetClosest();

            Player target = Main.player[npc.target];
            npc.Center = target.Center - Vector2.UnitY * (350f + (float)Math.Sin(MathHelper.TwoPi * time / 120f) * 10f);

            time++;

            if (Main.netMode != NetmodeID.MultiplayerClient && time % 60f == 59f)
            {
                float shootSpeed = 7f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * 70f);
                Utilities.NewProjectileBetter(npc.Center, aimDirection * shootSpeed, ModContent.ProjectileType<JewelBeam>(), 50, 0f);
            }

            return false;
        }
        #endregion Jewel

        #endregion AI

        #region Frames and Drawcode

        [OverrideAppliesTo(NPCID.KingSlime, typeof(KingSlimeAIClass), "KingSlimePreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool KingSlimePreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D kingSlimeTexture = Main.npcTexture[npc.type];
            Vector2 kingSlimeDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;

            // Draw the ninja, if it's still stuck.
            if (npc.life > npc.lifeMax * 0.6f)
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
            Vector2 crownDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * (npc.gfxOffY - (60f - verticalCrownOffset) * npc.scale);
            spriteBatch.Draw(crownTexture, crownDrawPosition, null, lightColor, 0f, crownTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}