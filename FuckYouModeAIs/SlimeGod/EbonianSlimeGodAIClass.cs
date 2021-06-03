using CalamityMod.NPCs;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class EbonianSlimeGodAIClass
    {
        #region Enumerations
        public enum CrimulanSlimeGodAttackType
        {
            CorruptSpawning = 0,
            ShortLeaps = 1,
            BigSlam = 2,
            GelCloudSlam = 3
        }
        #endregion

        #region AI

        [OverrideAppliesTo("SlimeGodRun", typeof(CrimulanSlimeGodAIClass), "CrimulanSlimeGodAI", EntityOverrideContext.NPCAI)]
        public static bool CrimulanSlimeGodAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGod) /*|| !Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodPurple)*/)
            {
                npc.active = false;
                return false;
            }

            // This will affect the other gods as well in terms of behavior.
            ref float universalState = ref Main.npc[CalamityGlobalNPC.slimeGod].ai[0];
            ref float universalTimer = ref Main.npc[CalamityGlobalNPC.slimeGod].ai[1];
            ref float stuckTimer = ref npc.Infernum().ExtraAI[5];
            ref float stuckTeleportCountdown = ref npc.Infernum().ExtraAI[6];

            if (stuckTeleportCountdown > 0f)
			{
                stuckTeleportCountdown--;

                npc.velocity.X = 0f;
                npc.velocity.Y += 0.3f;
                npc.scale = 1f - stuckTeleportCountdown / 40f;
                npc.damage = 0;
                return false;
			}

            npc.damage = npc.defDamage;

            if (!Collision.CanHit(target.Center, 1, 1, npc.Center, 1, 1))
            {
                stuckTimer++;
                if (stuckTimer > 180f)
				{
                    stuckTimer = 0f;
                    npc.Center = target.Center - Vector2.UnitY * 10f;
                    stuckTeleportCountdown = 40f;
                    npc.netUpdate = true;
                }
            }
            else if (stuckTimer > 0f)
                stuckTimer--;

            // Set the universal whoAmI variable.
            CalamityGlobalNPC.slimeGodRed = npc.whoAmI;
            /*
            npc.realLife = CalamityGlobalNPC.slimeGodPurple;
            npc.life = Main.npc[npc.realLife].life;
            npc.lifeMax = Main.npc[npc.realLife].lifeMax;
            */

            switch ((CrimulanSlimeGodAttackType)(int)universalState)
            {
                case CrimulanSlimeGodAttackType.CorruptSpawning:
                    DoAttack_CorruptSpawning(npc, target, ref universalTimer);
                    break;
            }

            return false;
        }

        public static void DoAttack_CorruptSpawning(NPC npc, Player target, ref float attackTimer)
		{
            Vector2 destination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 360f;
            destination.Y = MathHelper.Clamp(destination.Y - 3200f, 160f, Main.maxTilesX * 16f - 32f);

            if (WorldUtils.Find(destination.ToTileCoordinates(), Searches.Chain(new Searches.Down(450), new Conditions.IsSolid()), out Point result))
                destination.Y = result.Y * 16f - 8f;

            bool onSolidGround = WorldGen.SolidTile(Framing.GetTileSafely(npc.Bottom + Vector2.UnitY * 16f));

            if (MathHelper.Distance(target.Center.X, destination.Y) < 20f)
            {
                if (onSolidGround)
                {
                    Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, destination, 0.3f, 11.5f, out _);
                    npc.netUpdate = true;
                }
            }
            else
                npc.velocity.X *= 0.8f;
            
            if (Main.netMode != NetmodeID.MultiplayerClient && MathHelper.Distance(target.Center.X, destination.Y) > 135f)
			{

			}

            if (npc.velocity.Y < 15f)
                npc.velocity.Y += 0.3f;
        }
        #endregion AI
    }
}
