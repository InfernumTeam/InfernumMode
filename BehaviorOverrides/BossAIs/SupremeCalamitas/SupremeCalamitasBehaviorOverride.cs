using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using SCalBoss = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    // It begins.
    public class SupremeCalamitasBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SCalBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public static readonly Vector2 SepulcherSpawnOffset = new Vector2(0f, -350f);

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Vanish if the target is gone.
            if (!target.active || target.dead)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.1f, 0f, 1f);

                for (int i = 0; i < 2; i++)
                {
                    Dust fire = Dust.NewDustPerfect(npc.Center, (int)CalamityDusts.Brimstone);
                    fire.position += Main.rand.NextVector2Circular(36f, 36f);
                    fire.velocity = Main.rand.NextVector2Circular(8f, 8f);
                    fire.noGravity = true;
                    fire.scale *= Main.rand.NextFloat(1f, 1.2f);
                }

                if (npc.Opacity <= 0f)
                    npc.active = false;
                return false;
            }

            // Set the whoAmI index.
            CalamityGlobalNPC.SCal = npc.whoAmI;

            // Reset things.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool sepulcherIsPresent = NPC.AnyNPCs(ModContent.NPCType<SCalWormHead>());
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackDelay = ref npc.ai[2];
            ref float textState = ref npc.ai[3];
            ref float initialChargeupTime = ref npc.Infernum().ExtraAI[6];

            // Handle initializations.
            if (npc.localAI[1] == 0f)
            {
                // Define the arena.
                Vector2 arenaArea = new Vector2(250f, 250f);
                npc.Infernum().arenaRectangle = Utils.CenteredRectangle(npc.Center, arenaArea * 16f);
                int left = (int)(npc.Center.X / 16 - arenaArea.X * 0.5f);
                int right = (int)(npc.Center.X / 16 + arenaArea.X * 0.5f);
                int top = (int)(npc.Center.Y / 16 - arenaArea.Y * 0.5f);
                int bottom = (int)(npc.Center.Y / 16 + arenaArea.Y * 0.5f);

                for (int i = left; i <= right; i++)
                {
                    for (int j = top; j <= bottom; j++)
                    {
                        // Create arena tiles.
                        if ((i == left || i == right || j == top || j == bottom) && !Main.tile[i, j].active())
                        {
                            Main.tile[i, j].type = (ushort)ModContent.TileType<CalamityMod.Tiles.ArenaTile>();
                            Main.tile[i, j].active(true);
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            else
                                WorldGen.SquareTileFrame(i, j, true);
                        }
                    }
                }

                attackDelay = 180f;
                npc.localAI[1] = 1f;
            }

            // Handle text attack delays. These are used specifically for things like dialog.
            if (attackDelay > 0f)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                DoBehavior_HandleAttackDelaysAndText(npc, target, ref textState, ref attackDelay);

                if (textState == 0f && attackDelay == 2f)
                    initialChargeupTime = 240f;

                attackDelay--;
                return false;
            }

            if (initialChargeupTime > 0f)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                DoBehavior_HandleInitialChargeup(npc, target, ref initialChargeupTime);
                initialChargeupTime--;
                return false;
            }

            // Hover to the side of the target and watch if Sepulcher is present.
            if (sepulcherIsPresent)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;

                float hoverSpeed = 31f;
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 375f;
                hoverDestination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 450f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
                npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

                return false;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_HandleAttackDelaysAndText(NPC npc, Player target, ref float textState, ref float attackDelay)
        {
            // Go to the next text state for later once the delay concludes.
            if (attackDelay == 1f)
                textState++;

            // Slow down and look at the target.
            npc.velocity *= 0.95f;
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            switch ((int)textState)
            {
                // Start of battle.
                case 0:
                    if (attackDelay == 170f)
                        Main.NewText("... So it's you.", Color.Orange);

                    if (attackDelay == 60f)
                        Main.NewText("After all you've done, you will suffer.", Color.Orange);
                    break;
            }
        }

        public static void DoBehavior_HandleInitialChargeup(NPC npc, Player target, ref float initialChargeupTime)
        {
            // Slow down and look at the target.
            npc.velocity *= 0.95f;
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Charge up power.
            Vector2 dustSpawnPosition = npc.Center + Main.rand.NextVector2Unit(40f, 60f);
            Vector2 dustSpawnVelocity = (npc.Center - dustSpawnPosition) * 0.08f;

            Dust magic = Dust.NewDustPerfect(dustSpawnPosition, 267, dustSpawnVelocity);
            magic.color = Color.Lerp(Color.Red, Color.Magenta, Main.rand.NextFloat(0.4f));
            magic.scale *= Main.rand.NextFloat(0.9f, 1.2f);
            magic.noGravity = true;

            if (initialChargeupTime % 40f == 39f)
            {
                for (int i = 0; i < 6; i++)
                {
                    dustSpawnPosition = npc.Center + Main.rand.NextVector2Unit(300f, 380f);
                    dustSpawnVelocity = (npc.Center - dustSpawnPosition) * 0.08f;

                    magic = Dust.NewDustPerfect(dustSpawnPosition, 264, dustSpawnVelocity);
                    magic.color = Color.Lerp(Color.Red, Color.Magenta, Main.rand.NextFloat(0.4f));
                    magic.color.A = 127;
                    magic.noLight = true;
                    magic.scale *= Main.rand.NextFloat(1f, 1.3f);
                    magic.noGravity = true;
                }
            }

            // Summon spirits from below the player that congregate above their head.
            // They transform into sepulcher after a certain amount of time has passed.
            if (initialChargeupTime % 9f == 8f && initialChargeupTime >= 45f)
            {
                Main.PlaySound(SoundID.NPCHit36, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 soulSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 800f, 1000f);
                    Vector2 soulVelocity = -Vector2.UnitY.RotatedByRandom(0.72f) * Main.rand.NextFloat(6f, 10.5f);
                    int spirit = Utilities.NewProjectileBetter(soulSpawnPosition, soulVelocity, ModContent.ProjectileType<SepulcherSpirit>(), 0, 0f);

                    if (Main.projectile.IndexInRange(spirit))
                    {
                        Main.projectile[spirit].ai[0] = initialChargeupTime;
                        Main.projectile[spirit].localAI[0] = Main.rand.NextFloat(0.92f, 1.08f) % 1f;
                        Main.projectile[spirit].owner = target.whoAmI;
                    }
                }
            }

            // Create some dust to accompany the spirits.
            Vector2 sepulcherSpawnPosition = target.Center + SepulcherSpawnOffset;
            for (int i = 0; i < 5; i++)
            {
                Vector2 dustSpawnDirection = Main.rand.NextVector2Unit();
                Vector2 dustSpawnOffset = dustSpawnDirection.RotatedBy(-MathHelper.PiOver2) * Main.rand.NextFloat(50f);

                magic = Dust.NewDustPerfect(sepulcherSpawnPosition + dustSpawnOffset, 267);
                magic.scale = Main.rand.NextFloat(1f, 1.5f);
                magic.color = Color.Lerp(Color.Red, Color.Orange, Main.rand.NextFloat());
                magic.velocity = dustSpawnDirection * Main.rand.NextFloat(10f);
                magic.noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && initialChargeupTime == 1f)
            {
                int sepulcher = NPC.NewNPC((int)sepulcherSpawnPosition.X, (int)sepulcherSpawnPosition.Y, ModContent.NPCType<SCalWormHead>(), 1);
                if (Main.npc.IndexInRange(sepulcher))
                {
                    Main.npc[sepulcher].velocity = -Vector2.UnitY * 11f;
                    CalamityUtils.BossAwakenMessage(sepulcher);
                }
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float lifeRatio = npc.life / (float)npc.lifeMax;
        }
        #endregion AI
    }
}
