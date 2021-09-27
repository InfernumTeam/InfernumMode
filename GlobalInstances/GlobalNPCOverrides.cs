using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Projectiles.DraedonsArsenal;
using CalamityMod.Projectiles.Rogue;
using InfernumMode.BehaviorOverrides.BossAIs.EoW;
using InfernumMode.BehaviorOverrides.BossAIs.SlimeGod;
using InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


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

            bool isInkCloud = projectile.type == ModContent.ProjectileType<InkCloud>() || projectile.type == ModContent.ProjectileType<InkCloud2>() || projectile.type == ModContent.ProjectileType<InkCloud3>();
            if (isInkCloud && (npc.type == ModContent.NPCType<SlimeSpawnCrimson3>() || npc.type == ModContent.NPCType<SlimeSpawnCorrupt2>()))
                damage = (int)(damage * 0.6);

            if (npc.type == NPCID.WallofFleshEye && (projectile.penetrate == -1 || projectile.penetrate > 1))
                damage = (int)(damage * 0.785);

            if (npc.type == NPCID.WallofFleshEye && projectile.type == ModContent.ProjectileType<TrackingDiskLaser>())
                damage = (int)(damage * 0.625);

            if (projectile.type == ProjectileID.HolyArrow || projectile.type == ProjectileID.HallowStar)
                damage = (int)(damage * 0.65);

            if ((npc.type == ModContent.NPCType<CalamityMod.NPCs.Perforator.PerforatorBodyMedium>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.Perforator.PerforatorBodyLarge>()) && (projectile.penetrate >= 2 || projectile.penetrate == -1))
            {
                damage = (int)(damage * 0.4);
            }
        }

        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }

        public override bool CheckActive(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.CheckActive(npc);

            if (npc.type == NPCID.KingSlime)
                return false;

            return base.CheckActive(npc);
        }

        #endregion
    }
}