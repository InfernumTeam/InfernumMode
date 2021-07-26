using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Accessories.Wings;
using CalamityMod.Items.Armor.Vanity;
using CalamityMod.Items.Dyes;
using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Furniture.Trophies;
using CalamityMod.Items.SummonItems;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.TownNPCs;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Projectiles.Rogue;
using CalamityMod.Tiles.Ores;
using CalamityMod.World;
using InfernumMode.Buffs;
using InfernumMode.FuckYouModeAIs.Cultist;
using InfernumMode.FuckYouModeAIs.WallOfFlesh;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

using CryogenNPC = CalamityMod.NPCs.Cryogen.Cryogen;
using SignusNPC = CalamityMod.NPCs.Signus.Signus;
using YharonNPC = CalamityMod.NPCs.Yharon.Yharon;

namespace InfernumMode.FuckYouModeAIs.MainAI
{
	public partial class FuckYouModeAIsGlobal : GlobalNPC
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
                index = ModContent.GetModBossHeadSlot("InfernumMode/FuckYouModeAIs/Cryogen/CryogenMapIcon");
        }

        public override void SetDefaults(NPC npc)
        {
            angleTarget = default;
            for (int i = 0; i < ExtraAI.Length; i++)
                ExtraAI[i] = 0f;

            if (PoDWorld.InfernumMode && !BossRushEvent.BossRushActive)
            {
                if (OverridingListManager.InfernumSetDefaultsOverrideList.ContainsKey(npc.type))
                    OverridingListManager.InfernumSetDefaultsOverrideList[npc.type].DynamicInvoke(npc);
            }
        }
        public override bool PreAI(NPC npc)
        {
            if (PoDWorld.InfernumMode && !BossRushEvent.BossRushActive)
            {
                // Correct an enemy's life depending on its cached true life value.
                if (InfernumNPCHPValues.HPValues.ContainsKey(npc.type) &&
                    InfernumNPCHPValues.HPValues[npc.type] != npc.lifeMax)
                {
                    npc.life = npc.lifeMax = InfernumNPCHPValues.HPValues[npc.type];
                    npc.netUpdate = true;
                }

                if (OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type))
                    return (bool)OverridingListManager.InfernumNPCPreAIOverrideList[npc.type].DynamicInvoke(npc);
            }
            return base.PreAI(npc);
        }

        public override bool PreNPCLoot(NPC npc)
        {
            if (PoDWorld.InfernumMode && npc.type == InfernumMode.CalamityMod.NPCType("Providence"))
            {
                DropHelper.DropBags(npc);
                DropHelper.DropItemChance(npc, ModContent.ItemType<ProvidenceTrophy>(), 10);
                DropHelper.DropItemCondition(npc, ModContent.ItemType<KnowledgeProvidence>(), true, !CalamityWorld.downedProvidence);

                DropHelper.DropItemCondition(npc, ModContent.ItemType<RuneofCos>(), true, !CalamityWorld.downedProvidence);

                CalamityGlobalTownNPC.SetNewShopVariable(new int[] { ModContent.NPCType<THIEF>() }, CalamityWorld.downedProvidence);

                // Accessories clientside only in Expert. Both drop if she is defeated at night.
                DropHelper.DropItemCondition(npc, ModContent.ItemType<ElysianWings>(), Main.expertMode, true);
                DropHelper.DropItemCondition(npc, ModContent.ItemType<ElysianAegis>(), Main.expertMode, true);

                // Drops pre-scal, cannot be sold, does nothing aka purely vanity. Requires at least expert for consistency with other post scal dev items.
                DropHelper.DropItemCondition(npc, ModContent.ItemType<ProfanedSoulCrystal>(), true, true);

                // Special drop for defeating her at night
                DropHelper.DropItemCondition(npc, ModContent.ItemType<ProfanedMoonlightDye>(), true, true, 3, 4);

                // All other drops are contained in the bag, so they only drop directly on Normal
                if (!Main.expertMode)
                {
                    // Materials
                    DropHelper.DropItemSpray(npc, ModContent.ItemType<UnholyEssence>(), 20, 30);
                    DropHelper.DropItemSpray(npc, ModContent.ItemType<DivineGeode>(), 15, 20);

                    // Weapons
                    float w = 0.25f;
                    DropHelper.DropEntireWeightedSet(npc,
                        DropHelper.WeightStack<HolyCollider>(w),
                        DropHelper.WeightStack<SolarFlare>(w),
                        DropHelper.WeightStack<TelluricGlare>(w),
                        DropHelper.WeightStack<BlissfulBombardier>(w),
                        DropHelper.WeightStack<PurgeGuzzler>(w),
                        DropHelper.WeightStack<DazzlingStabberStaff>(w),
                        DropHelper.WeightStack<MoltenAmputator>(w)
                    );

                    // Equipment
                    DropHelper.DropItemChance(npc, ModContent.ItemType<SamuraiBadge>(), 40);

                    // Vanity
                    DropHelper.DropItemChance(npc, ModContent.ItemType<ProvidenceMask>(), 7);
                }

                // If Providence has not been killed, notify players of Uelibloom Ore
                if (!CalamityWorld.downedProvidence)
                {
                    string key2 = "Mods.CalamityMod.ProfanedBossText3";
                    Color messageColor2 = Color.Orange;
                    string key3 = "Mods.CalamityMod.TreeOreText";
                    Color messageColor3 = Color.LightGreen;

                    WorldGenerationMethods.SpawnOre(ModContent.TileType<UelibloomOre>(), 15E-05, .4f, .8f);

                    CalamityUtils.DisplayLocalizedText(key2, messageColor2);
                    CalamityUtils.DisplayLocalizedText(key3, messageColor3);
                }

                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Main.NewText(Language.GetTextValue("Mods.CalamityMod.ProfanedBossText4"), Color.DarkOrange);
                }

                // Mark Providence as dead
                CalamityWorld.downedProvidence = true;
                CalamityNetcode.SyncWorld();
                return false;
            }
            return base.PreNPCLoot(npc);
        }

        public override void NPCLoot(NPC npc)
        {
            if (!PoDWorld.InfernumMode)
                return;
            
            if (npc.type == NPCID.WallofFleshEye)
			{
                for (int i = 0; i < Main.rand.Next(11, 15 + 1); i++)
                    Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2CircularEdge(8f, 8f), ModContent.ProjectileType<CursedSoul>(), 55, 0f);
                if (Main.npc.IndexInRange(Main.wof))
                    Main.npc[Main.wof].StrikeNPC(npc.lifeMax, 0f, 0);
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
            if (!PoDWorld.InfernumMode)
                return;

            bool isDesertScourge = npc.type == ModContent.NPCType<DesertScourgeHead>() || npc.type == ModContent.NPCType<DesertScourgeBody>();
            isDesertScourge |= npc.type == ModContent.NPCType<DesertScourgeTail>() || npc.type == ModContent.NPCType<DesertScourgeHeadSmall>();
            isDesertScourge |= npc.type == ModContent.NPCType<DesertScourgeBodySmall>() || npc.type == ModContent.NPCType<DesertScourgeTailSmall>();

            if (isDesertScourge && (projectile.type == ProjectileID.JestersArrow || projectile.type == ProjectileID.UnholyArrow || projectile.type == ProjectileID.WaterBolt))
                damage = (int)(damage * 0.45);
            if (isDesertScourge && (projectile.type == ProjectileID.Flare || projectile.type == ProjectileID.BlueFlare))
                damage = (int)(damage * 0.7);

            if (npc.type == NPCID.KingSlime && projectile.penetrate == -1 || projectile.penetrate > 1)
                damage = (int)(damage * 0.67);

            if ((npc.type == ModContent.NPCType<CalamityMod.NPCs.Perforator.PerforatorBodyMedium>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.Perforator.PerforatorBodyLarge>()) && (projectile.penetrate >= 2 || projectile.penetrate == -1))
            {
                damage = (int)(damage * 0.4);
            }

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
            if (!PoDWorld.InfernumMode)
                return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }

        public override bool CheckDead(NPC npc)
        {
            if (!PoDWorld.InfernumMode)
                return base.CheckDead(npc);

            if (npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>())
                return false;

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
            if (!PoDWorld.InfernumMode)
                return base.CheckActive(npc);

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