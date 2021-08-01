using CalamityMod.Events;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.DesertScourge;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.DesertScourge
{
	public class AquaticScourgeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float initializedFlag = ref npc.Infernum().ExtraAI[0];
            ref float angeredYet = ref npc.Infernum().ExtraAI[1];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 55, ModContent.NPCType<AquaticScourgeBody>(), ModContent.NPCType<AquaticScourgeTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            // Determine hostility.
            if (npc.justHit || lifeRatio < 0.995f || BossRushEvent.BossRushActive)
            {
                if (npc.damage == 0)
                    npc.timeLeft *= 20;

                angeredYet = 1f;
                npc.damage = npc.defDamage;
                npc.boss = true;
                npc.netUpdate = true;
            }
            else
                npc.damage = 0;

            // If there still was no valid target, swim away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                DoAttack_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            // Swim slowly around the "target" when not angry.
            if (angeredYet == 0f)
                DoAttack_SlowSearchMovement(npc, target);
            else
            {
                DoMovement_AggressiveSnakeMovement(npc, target);
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            return false;
        }

        #region Specific Behaviors

        public static void DoAttack_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 18f)
                npc.velocity.Y += 0.32f;

            if (npc.timeLeft > 200)
                npc.timeLeft = 200;
        }

        public static void DoAttack_SlowSearchMovement(NPC npc, Player target)
        {
            if (npc.WithinRange(target.Center, 160f) && npc.velocity != Vector2.Zero)
                return;

            Vector2 flyDestination = target.Center;

            // Don't fly too high in the air.
            if (WorldUtils.Find(flyDestination.ToTileCoordinates(), Searches.Chain(new Searches.Down(10000), new CustomTileConditions.IsWaterOrSolid()), out Point result))
            {
                Vector2 worldCoordinatesResult = result.ToWorldCoordinates();
                if (worldCoordinatesResult.Y > flyDestination.Y + 50f)
                    flyDestination.Y = worldCoordinatesResult.Y + 25f;
            }

            float movementSpeed = MathHelper.Lerp(5f, 8.5f, Utils.InverseLerp(300f, 750f, npc.Distance(flyDestination), true));
            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(flyDestination), movementSpeed / 300f, true) * movementSpeed;
        }

        public static void DoMovement_AggressiveSnakeMovement(NPC npc, Player target)
        {

        }

        #endregion Specific Behaviors

        #region AI Utility Methods
        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        #endregion AI Utility Methods

        #endregion AI
    }
}
