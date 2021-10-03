using CalamityMod;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Dyes;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.Ravager;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Projectiles.DraedonsArsenal;
using CalamityMod.Projectiles.Rogue;
using CalamityMod.World;
using InfernumMode.Buffs;
using InfernumMode.BehaviorOverrides.BossAIs.Cultist;
using InfernumMode.BehaviorOverrides.BossAIs.EoW;
using InfernumMode.BehaviorOverrides.BossAIs.SlimeGod;
using InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

using CryogenNPC = CalamityMod.NPCs.Cryogen.Cryogen;
using PolterghastNPC = CalamityMod.NPCs.Polterghast.Polterghast;
using OldDukeNPC = CalamityMod.NPCs.OldDuke.OldDuke;
using YharonNPC = CalamityMod.NPCs.Yharon.Yharon;
using CalamityMod.NPCs.AquaticScourge;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        #region Instance and Variables
        public override bool InstancePerEntity => true;

        public static bool MLSealTeleport = false;
        public const int TotalExtraAISlots = 100;

        // I'll be fucking damned if this isn't enough
        internal float[] ExtraAI = new float[TotalExtraAISlots];
        internal Vector2 angleTarget = default;
        internal Rectangle arenaRectangle = default;
        internal bool canTelegraph = false;

        #endregion

        #region Helper Properties

        public static bool IsDoGAlive => NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()) ||
                                         NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHeadS>());
        #endregion

        #region Overrides

        #region Get Alpha
        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            if (npc.type == NPCID.MoonLordHand ||
                npc.type == NPCID.MoonLordHead ||
                npc.type == NPCID.MoonLordCore)
            {
                if (PoDWorld.InfernumMode)
                    return new Color(7, 81, 81);
            }
            if (npc.type == ModContent.NPCType<CalamitasRun3>() && PoDWorld.InfernumMode)
            {
                bool brotherAlive = false;
                if (CalamityGlobalNPC.cataclysm != -1)
                {
                    if (Main.npc[CalamityGlobalNPC.cataclysm].active)
                    {
                        brotherAlive = true;
                    }
                }
                if (CalamityGlobalNPC.catastrophe != -1)
                {
                    if (Main.npc[CalamityGlobalNPC.catastrophe].active)
                    {
                        brotherAlive = true;
                    }
                }
                if (PoDWorld.InfernumMode && brotherAlive)
                    return new Color(100, 0, 0, 127);
            }
            return base.GetAlpha(npc, drawColor);
        }
        #endregion

        public override void BossHeadSlot(NPC npc, ref int index)
        {
            if (!PoDWorld.InfernumMode)
                return;

            if (npc.type == ModContent.NPCType<CryogenNPC>())
                index = ModContent.GetModBossHeadSlot("InfernumMode/BehaviorOverrides/BossAIs/Cryogen/CryogenMapIcon");
        }

        public override void SetDefaults(NPC npc)
        {
            angleTarget = default;
            for (int i = 0; i < ExtraAI.Length; i++)
                ExtraAI[i] = 0f;

            if (InfernumMode.CanUseCustomAIs)
            {
                if (OverridingListManager.InfernumSetDefaultsOverrideList.ContainsKey(npc.type))
                    OverridingListManager.InfernumSetDefaultsOverrideList[npc.type].DynamicInvoke(npc);
            }
        }
        public override bool PreAI(NPC npc)
        {
            if (InfernumMode.CanUseCustomAIs)
            {
                // Correct an enemy's life depending on its cached true life value.
                if (InfernumNPCHPValues.HPValues.ContainsKey(npc.type) &&
                    InfernumNPCHPValues.HPValues[npc.type] != npc.lifeMax)
                {
                    npc.life = npc.lifeMax = InfernumNPCHPValues.HPValues[npc.type];
                    npc.netUpdate = true;
                }

                // Make perf worms immune to debuffs.
                if (CalamityGlobalNPC.PerforatorIDs.Contains(npc.type))
                {
                    for (int k = 0; k < npc.buffImmune.Length; k++)
                        npc.buffImmune[k] = true;
                }

                if (OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type))
                    return (bool)OverridingListManager.InfernumNPCPreAIOverrideList[npc.type].DynamicInvoke(npc);
            }
            return base.PreAI(npc);
        }

        public override bool PreNPCLoot(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.PreNPCLoot(npc);

            if (npc.type == NPCID.EaterofWorldsHead)
            {
                if (npc.realLife != -1 && Main.npc[npc.realLife].Infernum().ExtraAI[7] == 0f)
                {
                    Main.npc[npc.realLife].NPCLoot();
                    Main.npc[npc.realLife].Infernum().ExtraAI[7] = 1f;
                    return false;
                }

                if (npc.ai[2] >= 2f)
                    npc.boss = true;
                else if (npc.realLife == -1 && npc.Infernum().ExtraAI[8] == 0f)
                {
                    npc.Infernum().ExtraAI[8] = 1f;
                    EoWHeadBehaviorOverride.HandleSplit(npc, ref npc.ai[2]);
                }

                return npc.ai[2] >= 2f;
            }

            if (npc.type == InfernumMode.CalamityMod.NPCType("Providence"))
            {
                // Drops pre-scal, cannot be sold, does nothing aka purely vanity. Requires at least expert for consistency with other post scal dev items.
                DropHelper.DropItemCondition(npc, ModContent.ItemType<ProfanedSoulCrystal>(), true, true);

                // Special drop for defeating her at night
                DropHelper.DropItemCondition(npc, ModContent.ItemType<ProfanedMoonlightDye>(), true, true, 3, 4);

                Main.NewText(Language.GetTextValue("Mods.CalamityMod.ProfanedBossText4"), Color.DarkOrange);

                return true;
            }

            if (npc.type == ModContent.NPCType<OldDukeNPC>())
                CalamityMod.CalamityMod.StopRain();

            return base.PreNPCLoot(npc);
        }

        public override void NPCLoot(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;
            
            if (npc.type == NPCID.WallofFleshEye)
			{
                for (int i = 0; i < Main.rand.Next(11, 15 + 1); i++)
                    Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2CircularEdge(8f, 8f), ModContent.ProjectileType<CursedSoul>(), 55, 0f);
                if (Main.npc.IndexInRange(Main.wof))
                    Main.npc[Main.wof].StrikeNPC(1550, 0f, 0);
            }

            if (npc.type == NPCID.WallofFlesh)
            {
                for (int i = 0; i < Main.rand.Next(18, 29 + 1); i++)
                {
                    int soul = Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2CircularEdge(8f, 8f), ModContent.ProjectileType<CursedSoul>(), 55, 0f);
                    Main.projectile[soul].localAI[1] = Main.rand.NextBool(2).ToDirectionInt();
                }
            }

            if (npc.type == InfernumMode.CalamityMod.NPCType("DevourerofGodsHead"))
            {
                // Skip the sentinel phase entirely
                CalamityWorld.DoGSecondStageCountdown = 600;

                if (Main.netMode == NetmodeID.Server)
                {
                    var netMessage = InfernumMode.CalamityMod.GetPacket();
                    netMessage.Write((byte)14);
                    netMessage.Write(CalamityWorld.DoGSecondStageCountdown);
                    netMessage.Send();
                }
            }
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (npc.type == ModContent.NPCType<DevourerofGodsBodyS>())
            {
                cooldownSlot = 0;
                return npc.alpha == 0;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override bool StrikeNPC(NPC npc, ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            if (!PoDWorld.InfernumMode)
                return base.StrikeNPC(npc, ref damage, defense, ref knockback, hitDirection, ref crit);

            if (npc.type == InfernumMode.CalamityMod.NPCType("Yharon"))
            {
                if (npc.life - (int)Math.Ceiling(damage) <= 0)
                {
                    npc.NPCLoot();
                }
            }

            if (npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordHead)
            {
                if (damage > 1500)
                    damage = 1500;
                return false;
            }

            if (npc.type == NPCID.TheDestroyerBody)
                CalamityGlobalNPC.DestroyerIDs.Remove(NPCID.TheDestroyerBody);

            double realDamage = crit ? damage * 2 : damage;
            int life = npc.realLife > 0 ? Main.npc[npc.realLife].life : npc.life;
            if ((npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>()) &&
                 life - realDamage <= 1000)
            {
                damage = 0;
                npc.dontTakeDamage = true;
                npc.Infernum().ExtraAI[10] = 1f;
                return false;
            }

            return base.StrikeNPC(npc, ref damage, defense, ref knockback, hitDirection, ref crit);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            bool isDesertScourge = npc.type == ModContent.NPCType<DesertScourgeHead>() || npc.type == ModContent.NPCType<DesertScourgeBody>();
            isDesertScourge |= npc.type == ModContent.NPCType<DesertScourgeTail>() || npc.type == ModContent.NPCType<DesertScourgeHeadSmall>();
            isDesertScourge |= npc.type == ModContent.NPCType<DesertScourgeBodySmall>() || npc.type == ModContent.NPCType<DesertScourgeTailSmall>();
            bool isSplitEoW = npc.type == NPCID.EaterofWorldsBody && npc.realLife >= 0 && Main.npc[npc.realLife].ai[2] >= 1f;

            if (isDesertScourge && (projectile.type == ProjectileID.JestersArrow || projectile.type == ProjectileID.UnholyArrow || projectile.type == ProjectileID.WaterBolt))
                damage = (int)(damage * 0.45);

            if (projectile.type == ProjectileID.Flare || projectile.type == ProjectileID.BlueFlare)
                damage = (int)(damage * 0.8);

            if (isDesertScourge && (projectile.type == ProjectileID.Flare || projectile.type == ProjectileID.BlueFlare))
                damage = (int)(damage * 0.75);

            if (npc.type == NPCID.KingSlime && (projectile.penetrate == -1 || projectile.penetrate > 1))
                damage = (int)(damage * 0.67);

            if (isSplitEoW && (projectile.penetrate == -1 || projectile.penetrate > 1))
                damage = (int)(damage * 0.45);

            if ((npc.type == ModContent.NPCType<CalamityMod.NPCs.Perforator.PerforatorBodyMedium>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.Perforator.PerforatorBodyLarge>()) && (projectile.penetrate >= 2 || projectile.penetrate == -1))
            {
                damage = (int)(damage * 0.4);
            }

            bool isInkCloud = projectile.type == ModContent.ProjectileType<InkCloud>() || projectile.type == ModContent.ProjectileType<InkCloud2>() || projectile.type == ModContent.ProjectileType<InkCloud3>();
            if (isInkCloud && (npc.type == ModContent.NPCType<SlimeSpawnCrimson3>() || npc.type == ModContent.NPCType<SlimeSpawnCorrupt2>()))
                damage = (int)(damage * 0.6);

            if (npc.type == NPCID.WallofFleshEye && (projectile.penetrate == -1 || projectile.penetrate > 1))
                damage = (int)(damage * 0.785);

            if (npc.type == NPCID.WallofFleshEye && projectile.type == ModContent.ProjectileType<TrackingDiskLaser>())
                damage = (int)(damage * 0.625);

            if (projectile.type == ProjectileID.HolyArrow || projectile.type == ProjectileID.HallowStar)
                damage = (int)(damage * 0.65);

            if (npc.type == ModContent.NPCType<AquaticScourgeBody>() && (projectile.penetrate == -1 || projectile.penetrate > 1))
                damage = (int)(damage * 0.45);

            if (projectile.type == ModContent.ProjectileType<SporeBomb>() || projectile.type == ModContent.ProjectileType<LeafArrow>() || projectile.type == ModContent.ProjectileType<IcicleArrowProj>())
                damage = (int)(damage * 0.55);

            bool isPhantasmDragon = npc.type == NPCID.CultistDragonBody1 || npc.type == NPCID.CultistDragonBody2 || npc.type == NPCID.CultistDragonBody3 || npc.type == NPCID.CultistDragonBody4 || npc.type == NPCID.CultistDragonTail;
            if (isPhantasmDragon && (projectile.penetrate == -1 || projectile.penetrate > 1))
                damage = (int)(damage * 0.24);

            if (npc.type == ModContent.NPCType<DevourerofGodsHeadS>() || npc.type == ModContent.NPCType<DevourerofGodsTailS>() &&
                (projectile.type == ModContent.ProjectileType<PhantasmalRuinGhost>() || projectile.type == ModContent.ProjectileType<PhantasmalRuinProj>()) || projectile.type == ProjectileID.LostSoulFriendly)
            {
                damage = (int)(damage * 0.6);
            }

            if (npc.type == ModContent.NPCType<DevourerofGodsBody>() && (projectile.type == ProjectileID.MoonlordBullet || projectile.type == ProjectileID.MoonlordArrow || projectile.type == ProjectileID.MoonlordArrowTrail))
                damage = (int)(damage * 0.45);

            if (npc.type == InfernumMode.CalamityMod.NPCType("Providence") && projectile.minion && projectile.type == ModContent.ProjectileType<HolyFireBulletProj>())
                damage = (int)(damage * 0.6);
        }

        public override void BossHeadRotation(NPC npc, ref float rotation)
        {
            if (!PoDWorld.InfernumMode)
                return;

            if (npc.type == ModContent.NPCType<DevourerofGodsHead>() ||
                npc.type == ModContent.NPCType<DevourerofGodsTail>() ||
                npc.type == ModContent.NPCType<DevourerofGodsTailS>() ||
                npc.type == ModContent.NPCType<DevourerofGodsBodyS>() ||
                npc.type == ModContent.NPCType<DevourerofGodsHeadS>())
            {
                if (npc.Opacity < 0.1f)
                    rotation = float.NaN;
            }

            if (npc.type == ModContent.NPCType<Siren>())
            {
                if (npc.Opacity < 0.1f)
                    rotation = float.NaN;
            }

            // Prevent Yharon from showing himself amongst his illusions in Subphase 10.
            if (npc.type == ModContent.NPCType<YharonNPC>())
            {
                if (npc.life / (float)npc.lifeMax <= 0.05f && npc.Infernum().ExtraAI[2] == 1f)
                    rotation = float.NaN;
            }
        }

        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }

        public override bool CheckDead(NPC npc)
        {
            if (!PoDWorld.InfernumMode)
                return base.CheckDead(npc);

            if (npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>())
                return false;

            if (npc.type == ModContent.NPCType<DevourerofGodsHeadS>() || npc.type == ModContent.NPCType<DevourerofGodsBodyS>() || npc.type == ModContent.NPCType<DevourerofGodsTailS>())
            {
                npc.life = 1;
                npc.dontTakeDamage = true;
                if (npc.type == ModContent.NPCType<DevourerofGodsHeadS>())
                {
                    if (npc.Infernum().ExtraAI[20] == 0f)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DevourerSpawn"), npc.Center);
                        npc.Infernum().ExtraAI[20] = 1f;
                    }
                }
                else
                    npc.life = npc.lifeMax;
                npc.netUpdate = true;
                return false;
            }

            if (npc.type == ModContent.NPCType<PolterghastNPC>())
            {
                if (npc.Infernum().ExtraAI[6] > 0f)
                    return true;
                npc.Infernum().ExtraAI[6] = 1f;
                npc.life = 1;
                npc.netUpdate = true;
                npc.dontTakeDamage = true;

                return npc.Infernum().ExtraAI[6] == 0f;
            }

            if (npc.type == ModContent.NPCType<Bumblefuck2>())
            {
                if (npc.ai[0] != 3f && npc.ai[3] > 0f)
                {
                    npc.life = npc.lifeMax;
                    npc.dontTakeDamage = true;
                    npc.ai[0] = 3f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
                return false;
            }

            if ((npc.type == NPCID.Spazmatism || npc.type == NPCID.Retinazer))
            {
                bool otherTwinHasCreatedShield = false;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active)
                        continue;
                    if (Main.npc[i].type != NPCID.Retinazer && Main.npc[i].type != NPCID.Spazmatism)
                        continue;
                    if (Main.npc[i].type == npc.type)
                        continue;

                    if (Main.npc[i].Infernum().ExtraAI[3] == 1f)
                    {
                        otherTwinHasCreatedShield = true;
                        break;
                    }
                }

                if (npc.Infernum().ExtraAI[3] == 0f && !otherTwinHasCreatedShield)
                {
                    npc.life = 1;
                    npc.active = true;
                    npc.netUpdate = true;
                    npc.dontTakeDamage = true;
                    return false;
                }
            }

            if (npc.type == ModContent.NPCType<RavagerClawLeft>() || npc.type == ModContent.NPCType<RavagerClawRight>())
            {
                npc.Infernum().ExtraAI[0] = 1f;
                npc.netUpdate = true;
                npc.active = true;
                npc.dontTakeDamage = true;
                npc.life = npc.lifeMax;

                // Synchronize the other claw, if it too has been released.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC checkNPC = Main.npc[i];
                    bool correctNPC = checkNPC.type == ModContent.NPCType<RavagerClawLeft>() || checkNPC.type == ModContent.NPCType<RavagerClawRight>();
                    if (!correctNPC || !checkNPC.active || checkNPC.Infernum().ExtraAI[0] != 1f)
                        continue;

                    checkNPC.ai[0] = 2f;
                    checkNPC.ai[1] = 0f;
                    checkNPC.netUpdate = true;
                }

                return false;
            }

            if (npc.type == NPCID.CultistBoss)
            {
                CultistBehaviorOverride.ClearAwayEntities();
                npc.Infernum().ExtraAI[6] = 1f;
                npc.netUpdate = true;
                npc.active = true;
                npc.dontTakeDamage = true;
                npc.life = 1;

                Main.PlaySound(SoundID.NPCDeath59, npc.Center);

                return false;
            }

            return base.CheckDead(npc);
        }

        public override bool CheckActive(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.CheckActive(npc);

            if (npc.type == NPCID.KingSlime)
                return false;
            if (npc.type == NPCID.AncientCultistSquidhead)
                return false;
            if (npc.type == NPCID.MoonLordFreeEye)
                return false;

            return base.CheckActive(npc);
        }

        public override void OnHitPlayer(NPC npc, Player target, int damage, bool crit)
        {
            if (!PoDWorld.InfernumMode)
                return;

            if (npc.type == NPCID.Retinazer && !NPC.AnyNPCs(NPCID.Spazmatism))
                target.AddBuff(ModContent.BuffType<RedSurge>(), 180);
            if (npc.type == NPCID.Spazmatism && !NPC.AnyNPCs(NPCID.Retinazer))
                target.AddBuff(ModContent.BuffType<ShadowflameInferno>(), 180);
        }

        #endregion
    }
}