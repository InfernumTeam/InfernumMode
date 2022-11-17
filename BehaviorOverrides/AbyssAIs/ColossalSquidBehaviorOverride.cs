using CalamityMod;
using CalamityMod.NPCs.Abyss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
{
    public class ColossalSquidBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ColossalSquid>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        // Piecewise function variables for determining the offset of tentacles when swiping at the target.
        public static CurveSegment Anticipation => new(EasingType.PolyOut, 0f, 0f, -0.53f, 2);

        public static CurveSegment Slash => new(EasingType.PolyIn, 0.17f, Anticipation.EndingHeight, 2.06f, 3);

        public static CurveSegment Recovery => new(EasingType.SineOut, 0.4f, Slash.EndingHeight, -1.53f);

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            ref float isHostile = ref npc.Infernum().ExtraAI[0];
            ref float hasSummonedTentacles = ref npc.Infernum().ExtraAI[1];
            ref float leftTentacleIndex = ref npc.Infernum().ExtraAI[2];
            ref float rightTentacleIndex = ref npc.Infernum().ExtraAI[3];
            ref float attackTimer = ref npc.Infernum().ExtraAI[4];

            // Don't naturally despawn if sleeping.
            if (isHostile != 1f)
                npc.timeLeft = 7200;

            // Summon tentacles on the first frame.
            if (hasSummonedTentacles == 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    leftTentacleIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 125, (int)npc.Center.Y + 1040, ModContent.NPCType<ColossalSquidTentacle>(), 1, 1f, npc.whoAmI);
                    rightTentacleIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 100, (int)npc.Center.Y + 1040, ModContent.NPCType<ColossalSquidTentacle>(), 1, 0f, npc.whoAmI);
                }

                hasSummonedTentacles = 1f;
                npc.netUpdate = true;
            }

            // Become hostile if hit.
            if (npc.justHit && npc.Infernum().ExtraAI[0] != 1f)
            {
                isHostile = 1f;
                npc.netUpdate = true;
            }

            // Stop at this point if not hostile, and just sit in place.
            if (isHostile != 1f)
                return false;

            NPC leftTentacle = Main.npc[(int)leftTentacleIndex];
            NPC rightTentacle = Main.npc[(int)rightTentacleIndex];

            float swingCompletion = attackTimer / 90f % 1f;
            Vector2 tentacleDirection = PiecewiseAnimation(swingCompletion, Anticipation, Slash, Recovery).ToRotationVector2();
            Vector2 legDestination = npc.Center + tentacleDirection * (Convert01To010(swingCompletion) * 10f + 250f);

            rightTentacle.Center = legDestination;

            // Increment the attack timer.
            attackTimer++;

            return false;
        }
        #endregion AI and Behaviors

        #region Frames and Drawcode
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/SleepingColossalSquid").Value;
            Rectangle frame = texture.Frame();
            if (npc.Infernum().ExtraAI[0] == 1f)
            {
                texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/ColossalSquid").Value;
                frame = npc.frame;
            }

            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * 30f;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, frame.Size() * 0.5f, npc.scale, direction, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}
