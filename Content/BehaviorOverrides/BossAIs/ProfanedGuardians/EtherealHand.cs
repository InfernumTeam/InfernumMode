using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.GuardianComboAttackManager;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class EtherealHand : ModNPC
    {
        #region Enumerations
        public enum HandSides
        {
            Left = -1,
            Right = 1
        }
        #endregion

        #region Fields/Properties
        public HandSides HandSide => NPC.ai[0] == 1 ? HandSides.Left : HandSides.Right;

        public Player Target => Main.player[NPC.target];

        public ref float FingerOutwardness => ref NPC.localAI[0];

        public ref float FingerSpacingOffset => ref NPC.localAI[1];

        public bool UsingPointerFinger
        {
            get => NPC.localAI[2] != 0f;
            set => NPC.localAI[2] = value.ToInt();
        }

        public static bool ShouldEditDestination => AttackerGuardian.Infernum().ExtraAI[HandsShouldUseNotDefaultPositionIndex] == 1f;

        public static NPC AttackerGuardian
        {
            get
            { 
                if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss))
                    return Main.npc[CalamityGlobalNPC.doughnutBoss];
                return null;
            }
        }

        public static bool ShouldBeInvisible => AttackerGuardian.localAI[2] != 0f;

        public Vector2 PointerFingerPosition => NPC.Center + (NPC.rotation + FingerSpacingOffset * -5f).ToRotationVector2() * FingerOutwardness;

        public const float HandSize = 56f;

        #endregion

        #region Overrides
        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Ethereal Hand");
            NPCID.Sets.TrailingMode[NPC.type] = 2;
            NPCID.Sets.TrailCacheLength[NPC.type] = 15;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.damage = 160;
            NPC.width = NPC.height = 50;
            NPC.dontTakeDamage = true;
            NPC.lifeMax = 10000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.hide = true;
            NPC.alpha = 255;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.doughnutBoss < 0 || !Main.npc[CalamityGlobalNPC.doughnutBoss].active || AttackerGuardian is null)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            // Fade in and out as necessary.
            NPC.alpha = Utils.Clamp(NPC.alpha + (ShouldBeInvisible ? 40 : -12), 0, 255);

            // Inherit the current target from the attacker guardian.
            NPC.target = AttackerGuardian.target;

            // Point away from the attacker guardian.
            NPC.rotation = AttackerGuardian.AngleTo(NPC.Center);

            // Reset hand attributes and the hover destination.
            UsingPointerFinger = false;
            FingerSpacingOffset = MathHelper.Lerp(FingerSpacingOffset, MathHelper.ToRadians(9f), 0.25f);

            Vector2 destination = AttackerGuardian.Center;
            destination += new Vector2((float)HandSide * 120f, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f + (float)HandSide * 2.1f) * 30f - 80f);

            FingerOutwardness = MathHelper.Lerp(FingerOutwardness, 35f, 0.2f);

            // Close in on the attacker guardian's center when the hands should be invisible.
            if (ShouldBeInvisible)
                destination = AttackerGuardian.Center + AttackerGuardian.SafeDirectionTo(NPC.Center);

            // Edit the move position if told to.
            if (ShouldEditDestination)
                destination = (HandSide == HandSides.Left ? LeftHandPosition : RightHandPosition) + AttackerGuardian.Center;

            AttackerGuardian.Infernum().ExtraAI[HandsShouldUseNotDefaultPositionIndex] = 0f;

            float hoverSpeed = MathHelper.Min((AttackerGuardian.position - AttackerGuardian.oldPos[1]).Length() * 1.25f + 8f, NPC.Distance(destination));

            // GLUE to the position.
            if (ShouldEditDestination)
                hoverSpeed = 2000f;
            NPC.velocity = NPC.SafeDirectionTo(destination) * hoverSpeed;

            // Perform NaN safety.
            if (NPC.velocity.HasNaNs())
                NPC.velocity = Vector2.UnitY;

            // Dont deal damage.
            NPC.damage = 0;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsBehindNonSolidTiles.Add(index);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // WHY
            if (AttackerGuardian == null)
                return false;
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Vector2 handScale = new Vector2(HandSize) / texture.Size() * 1.6f;
            SpriteEffects direction = HandSide == HandSides.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Color handColor = Color.Lerp(Color.Orange, Color.Yellow, 0.5f);
            handColor = Color.Lerp(handColor, Color.LightGoldenrodYellow, 0.5f);
            handColor *= NPC.Opacity * AttackerGuardian.Opacity;
            handColor.A = 0;

            float distanceFromAttacker = NPC.Distance(AttackerGuardian.Center);
            int totalPoints = 20 + (int)(distanceFromAttacker / 40f);

            Vector2 sagLocation = Vector2.Lerp(AttackerGuardian.Center, NPC.Center, 0.5f);
           
            sagLocation.Y += AttackerGuardian.velocity.ClampMagnitude(1f, 18f).Y * -5f;
            sagLocation.Y += MathHelper.Lerp(0f, 30f, Utils.GetLerpValue(4f, 1f, Math.Abs(AttackerGuardian.velocity.Y + 0.1f), true));
            

            Vector2[] drawPoints = new BezierCurve(AttackerGuardian.Center, sagLocation, NPC.Center).GetPoints(totalPoints).ToArray();

            for (int i = 0; i < 5; i++)
            {
                float fingerAngle = NPC.rotation + MathHelper.Lerp(-5f, 5f, i / 5f) * FingerSpacingOffset;
                float universalScaleFactor = i != 0 && UsingPointerFinger ? 0f : 1f;
                float currentFingerOutwardness = FingerOutwardness * universalScaleFactor;
                Vector2 fingerScale = new Vector2(currentFingerOutwardness / 3f) / texture.Size() * new Vector2(4f, 3f) * universalScaleFactor;

                for (int j = 0; j < 3; j++)
                {
                    Vector2 fingerDrawPosition = NPC.Center + fingerAngle.ToRotationVector2() * HandSize * 0.5f - Main.screenPosition;
                    fingerDrawPosition += fingerAngle.ToRotationVector2() * currentFingerOutwardness * j / 3f;
                    Main.spriteBatch.Draw(texture, fingerDrawPosition, null, handColor, fingerAngle + MathHelper.PiOver2, texture.Size() * new Vector2(0.5f, 0f), fingerScale, direction, 0f);
                }
            }

            for (int i = 0; i < 30; i++)
            {
                float handRotation = NPC.rotation + MathHelper.PiOver2 + MathHelper.TwoPi * i / 30f;
                Main.spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, null, handColor * 0.08f, handRotation, texture.Size() * 0.5f, handScale, direction, 0f);
            }

            for (int i = 0; i < drawPoints.Length - 1; i++)
            {
                float completionRatio = i / (float)drawPoints.Length;

                Vector2 currentPoint = drawPoints[i];
                Vector2 nextPoint = drawPoints[i + 1];
                Vector2 midPoint = Vector2.Lerp(currentPoint, nextPoint, 0.5f);

                if (i > 8 && Main.rand.NextBool(50) && NPC.Opacity * AttackerGuardian.Opacity == 1f && !ShouldBeInvisible)
                {
                    Dust fire = Dust.NewDustPerfect(currentPoint, 244);
                    fire.color = Color.Yellow;
                    fire.velocity = Vector2.UnitY * -Main.rand.NextFloat(1f, 1.2f);
                    fire.velocity += NPC.velocity * new Vector2(1f, 0.3f);
                    fire.scale = 0.8f;
                }

                float rotation = (nextPoint - currentPoint).ToRotation() + MathHelper.PiOver2;
                Vector2 segmentScale = handScale * MathHelper.Lerp(0.6f, 1f, Utils.GetLerpValue(0.36f, 0f, completionRatio, true));

                Main.spriteBatch.Draw(texture, currentPoint - Main.screenPosition, null, handColor * 0.5f, rotation, texture.Size() * 0.5f, segmentScale, direction, 0f);
                Main.spriteBatch.Draw(texture, midPoint - Main.screenPosition, null, handColor * 0.5f, rotation, texture.Size() * 0.5f, segmentScale, direction, 0f);
            }

            return false;
        }

        public override bool CheckActive() => false;
        #endregion
    }
}
