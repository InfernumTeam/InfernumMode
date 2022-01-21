using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.DataStructures;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class EtherealHand : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float HandSide => ref npc.ai[0];
        public ref float FingerOutwardness => ref npc.localAI[0];
        public ref float FingerSpacingOffset => ref npc.localAI[1];
        public bool UsingPointerFinger
        {
            get => npc.localAI[2] != 0f;
            set => npc.localAI[2] = value.ToInt();
        }

        public NPC AttackerGuardian => Main.npc[CalamityGlobalNPC.doughnutBoss];
        public bool ShouldBeInvisible => AttackerGuardian.localAI[2] != 0f;
        public float AttackTime => AttackerGuardian.ai[1];
        public AttackerGuardianBehaviorOverride.AttackGuardianAttackState AttackerState => (AttackerGuardianBehaviorOverride.AttackGuardianAttackState)(int)AttackerGuardian.ai[0];
        public bool PunchingTarget => AttackerState == AttackerGuardianBehaviorOverride.AttackGuardianAttackState.ThrowingHands && AttackTime > 45f && AttackerGuardian.WithinRange(Target.Center, 250f);
        public Vector2 PointerFingerPosition => npc.Center + (npc.rotation + FingerSpacingOffset * -5f).ToRotationVector2() * FingerOutwardness;

        public const float HandSize = 56f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ethereal Hand");
            NPCID.Sets.TrailingMode[npc.type] = 2;
            NPCID.Sets.TrailCacheLength[npc.type] = 15;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.damage = 230;
            npc.width = npc.height = 50;
            npc.dontTakeDamage = true;
            npc.lifeMax = 10000;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.hide = true;
            npc.alpha = 255;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.doughnutBoss < 0 || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            // Fade in and out as necessary.
            npc.alpha = Utils.Clamp(npc.alpha + (ShouldBeInvisible ? 40 : -12), 0, 255);

            // Inherit the current target from the attacker guardian.
            npc.target = AttackerGuardian.target;

            // Point away from the attacker guardian.
            npc.rotation = AttackerGuardian.AngleTo(npc.Center);

            // Reset hand attributes and the hover destination.
            UsingPointerFinger = false;
            FingerSpacingOffset = MathHelper.Lerp(FingerSpacingOffset, MathHelper.ToRadians(9f), 0.25f);
            Vector2 destination = AttackerGuardian.Center;
            destination += new Vector2(HandSide * 110f, (float)Math.Sin(AttackTime / 16f + HandSide * 2.1f) * 30f - 120f);

            switch (AttackerState)
            {
                case AttackerGuardianBehaviorOverride.AttackGuardianAttackState.MagicFingerBolts:
                    // Have the finger closest to the target use the pointer finger.
                    UsingPointerFinger = (Target.Center.X - AttackerGuardian.Center.X > 0).ToDirectionInt() == HandSide;

                    // Determine a new finger spacing offest and hover destination.
                    FingerSpacingOffset = MathHelper.Lerp(FingerSpacingOffset, MathHelper.ToRadians(10f), 0.3f);
                    destination = AttackerGuardian.Center;
                    destination += new Vector2(HandSide * 180f, (float)Math.Sin(AttackTime / 16f + HandSide * 1.8f) * (!UsingPointerFinger).ToInt() * 60f - 60f);

                    // Have the pointer finger point ahead of the target.
                    if (UsingPointerFinger)
                        npc.rotation = (Target.Center - PointerFingerPosition + Target.velocity * 20f).ToRotation() + FingerSpacingOffset * 5f;

                    // Release magic bursts periodically.
                    if (AttackTime % 24f == 23f && UsingPointerFinger && AttackTime > 90f)
                    {
                        Main.PlaySound(SoundID.DD2_KoboldIgnite, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 magicShootVelocity = (Target.Center - PointerFingerPosition + Target.velocity * 20f).SafeNormalize(Vector2.UnitX * HandSide) * 20f;
                            magicShootVelocity = magicShootVelocity.RotatedBy(MathHelper.Lerp(-0.7f, 0.7f, AttackTime / 90f % 1f));
                            Utilities.NewProjectileBetter(npc.Center + magicShootVelocity, magicShootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 230, 0f);
                        }
                    }
                    break;
            }

            // Punch the target. This involves resetting the hover destination rapidly and playing sounds periodically.
            if (PunchingTarget)
            {
                // Make a fingers come close to the hand.
                FingerOutwardness = MathHelper.Lerp(FingerOutwardness, 8f, 0.2f);

                float punchInterpolant = (float)Math.Sin(AttackTime / 4f + (HandSide == 1f ? MathHelper.Pi : 0f)) * 0.5f + 0.5f;
                destination = AttackerGuardian.Center;
                destination += AttackerGuardian.SafeDirectionTo(Target.Center) * MathHelper.Lerp(28f, 156f, punchInterpolant);
                destination += AttackerGuardian.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.PiOver2 * HandSide) * punchInterpolant * 70f;

                // Create punch sounds.
                if (AttackTime % 15f == 14f)
                    Main.PlaySound(SoundID.Item74, npc.Center);
            }

            // Make the finger outwardness interpolant towards its traditional value when not punching.
            else
                FingerOutwardness = MathHelper.Lerp(FingerOutwardness, 35f, 0.2f);

            // Close in on the attacker guardian's center when the hands should be invisible.
            if (ShouldBeInvisible)
                destination = AttackerGuardian.Center + AttackerGuardian.SafeDirectionTo(npc.Center);

            float hoverSpeed = MathHelper.Min((AttackerGuardian.position - AttackerGuardian.oldPos[1]).Length() * 1.25f + 8f, npc.Distance(destination));
            npc.velocity = npc.SafeDirectionTo(destination) * hoverSpeed;

            // Perform NaN safety.
            if (npc.velocity.HasNaNs())
                npc.velocity = Vector2.UnitY;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsBehindNonSolidTiles.Add(index);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("CalamityMod/Projectiles/StarProj");
            Vector2 handScale = new Vector2(HandSize) / texture.Size() * 1.6f;
            SpriteEffects direction = HandSide == 1f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Color handColor = Color.Lerp(Color.Orange, Color.Yellow, 0.5f);
            handColor = Color.Lerp(handColor, Color.LightGoldenrodYellow, 0.5f);
            handColor *= npc.Opacity * AttackerGuardian.Opacity;
            handColor.A = 0;

            float distanceFromAttacker = npc.Distance(AttackerGuardian.Center);
            int totalPoints = 20 + (int)(distanceFromAttacker / 40f);

            Vector2 sagLocation = Vector2.Lerp(AttackerGuardian.Center, npc.Center, 0.5f);
            if (AttackerState != AttackerGuardianBehaviorOverride.AttackGuardianAttackState.ThrowingHands)
            {
                sagLocation.Y += AttackerGuardian.velocity.ClampMagnitude(0f, 18f).Y * -10f;
                sagLocation.Y += MathHelper.Lerp(0f, 60f, Utils.InverseLerp(4f, 1f, Math.Abs(AttackerGuardian.velocity.Y), true));
            }

            Vector2[] drawPoints = new BezierCurve(AttackerGuardian.Center, sagLocation, npc.Center).GetPoints(totalPoints).ToArray();

            for (int i = 0; i < 5; i++)
            {
                float fingerAngle = npc.rotation + MathHelper.Lerp(-5f, 5f, i / 5f) * FingerSpacingOffset;
                float universalScaleFactor = i != 0 && UsingPointerFinger ? 0f : 1f;
                float currentFingerOutwardness = FingerOutwardness * universalScaleFactor;
                Vector2 fingerScale = new Vector2(currentFingerOutwardness / 3f) / texture.Size() * new Vector2(4f, 3f) * universalScaleFactor;

                for (int j = 0; j < 3; j++)
                {
                    Vector2 fingerDrawPosition = npc.Center + fingerAngle.ToRotationVector2() * HandSize * 0.5f - Main.screenPosition;
                    fingerDrawPosition += fingerAngle.ToRotationVector2() * currentFingerOutwardness * j / 3f;
                    spriteBatch.Draw(texture, fingerDrawPosition, null, handColor, fingerAngle + MathHelper.PiOver2, texture.Size() * new Vector2(0.5f, 0f), fingerScale, direction, 0f);
                }
            }

            for (int i = 0; i < 30; i++)
            {
                float handRotation = npc.rotation + MathHelper.PiOver2 + MathHelper.TwoPi * i / 30f;
                spriteBatch.Draw(texture, npc.Center - Main.screenPosition, null, handColor * 0.08f, handRotation, texture.Size() * 0.5f, handScale, direction, 0f);
            }

            for (int i = 0; i < drawPoints.Length - 1; i++)
            {
                float completionRatio = i / (float)drawPoints.Length;

                Vector2 currentPoint = drawPoints[i];
                Vector2 nextPoint = drawPoints[i + 1];
                Vector2 midPoint = Vector2.Lerp(currentPoint, nextPoint, 0.5f);

                if (i > 8 && Main.rand.NextBool(50) && npc.Opacity * AttackerGuardian.Opacity == 1f && !ShouldBeInvisible)
                {
                    Dust fire = Dust.NewDustPerfect(currentPoint, 244);
                    fire.color = Color.Yellow;
                    fire.velocity = Vector2.UnitY * -Main.rand.NextFloat(1f, 1.2f);
                    fire.velocity += npc.velocity * new Vector2(1f, 0.3f);
                    fire.scale = 0.8f;
                }

                float rotation = (nextPoint - currentPoint).ToRotation() + MathHelper.PiOver2;
                Vector2 segmentScale = handScale * MathHelper.Lerp(0.6f, 1f, Utils.InverseLerp(0.36f, 0f, completionRatio, true));

                spriteBatch.Draw(texture, currentPoint - Main.screenPosition, null, handColor * 0.5f, rotation, texture.Size() * 0.5f, segmentScale, direction, 0f);
                spriteBatch.Draw(texture, midPoint - Main.screenPosition, null, handColor * 0.5f, rotation, texture.Size() * 0.5f, segmentScale, direction, 0f);
            }

            return false;
        }

        public override bool CheckActive() => false;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = 1;
            return true;
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(ModContent.BuffType<HolyFlames>(), 120, true);
        }
    }
}
