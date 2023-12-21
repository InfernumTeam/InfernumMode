using CalamityMod;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using CalamityMod.UI;
using InfernumMode.Common.DataStructures;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Content.Achievements;
using InfernumMode.Content.Achievements.InfernumAchievements;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Cryogen;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.Cutscenes;
using InfernumMode.Content.Items.SummonItems;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CryogenNPC = CalamityMod.NPCs.Cryogen.Cryogen;

namespace InfernumMode.Core.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        #region Statics
        internal static bool ShouldSetDefaults
        {
            get;
            set;
        }
        #endregion

        #region Instance and Variables
        public override bool InstancePerEntity => true;

        // I'll be fucking damned if this isn't enough.
        public const int TotalExtraAISlots = 100;

        public int? TotalPlayersAtStart;

        public bool DisableNaturalDespawning;

        public bool IsAbyssPredator;

        public bool IsAbyssPrey;

        public bool HasResetHP;

        // I'll be fucking damned if this isn't enough.
        public float[] ExtraAI = new float[TotalExtraAISlots];

        public Rectangle Arena;

        public PrimitiveTrailCopy OptionalPrimitiveDrawer;

        public Primitive3DStrip Optional3DStripDrawer;

        internal static int Cryogen = -1;

        internal static int AstrumAureus = -1;

        internal static int ProfanedCrystal = -1;

        internal static int Yharon = -1;
        #endregion

        #region Reset Effects
        public override void ResetEffects(NPC npc)
        {
            static void ResetSavedIndex(ref int index, int type, int type2 = -1)
            {
                if (index >= 0)
                {
                    // If the index npc is not active, reset the index.
                    if (!Main.npc[index].active)
                        index = -1;

                    // Else, if this is -1,
                    else if (type2 == -1)
                    {
                        // If the index is not the correct type, reset the index.
                        if (Main.npc[index].type != type)
                            index = -1;
                    }
                    else
                    {
                        if (Main.npc[type].type != type && Main.npc[index].type != type2)
                            index = -1;
                    }
                }
            }

            ResetSavedIndex(ref Cryogen, ModContent.NPCType<CryogenNPC>());
            ResetSavedIndex(ref AstrumAureus, ModContent.NPCType<AstrumAureus>());
            ResetSavedIndex(ref ProfanedCrystal, ModContent.NPCType<HealerShieldCrystal>());
            ResetSavedIndex(ref Yharon, ModContent.NPCType<Yharon>());
        }
        #endregion Reset Effects

        #region Overrides

        public override void SetDefaults(NPC npc)
        {
            // Only set these when told to.
            if (!ShouldSetDefaults)
                return;

            for (int i = 0; i < ExtraAI.Length; i++)
                ExtraAI[i] = 0f;

            var infernum = npc.Infernum();
            infernum.IsAbyssPredator = false;
            infernum.IsAbyssPrey = false;
            infernum.HasResetHP = false;
            infernum.OptionalPrimitiveDrawer = null;
            infernum.Optional3DStripDrawer = null;

            if (InfernumMode.CanUseCustomAIs && NPCBehaviorOverride.BehaviorOverrides.TryGetValue(npc.type, out var value))
                value.SetDefaults(npc);
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.BossBestiaryPriority.Add(ModContent.NPCType<GreatSandShark>());
        }

        public override bool PreAI(NPC npc)
        {
            // Initialize the amount of players the NPC had when it spawned.
            if (!npc.Infernum().TotalPlayersAtStart.HasValue)
            {
                int activePlayerCount = 0;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active)
                        activePlayerCount++;
                }
                npc.Infernum().TotalPlayersAtStart = activePlayerCount;
                npc.netUpdate = true;
            }

            if (InfernumMode.CanUseCustomAIs)
            {
                // Correct an enemy's life depending on its cached true life value.
                if (!npc.Infernum().HasResetHP && NPCHPValues.HPValues.TryGetValue(npc.type, out int maxHP) && maxHP >= 0)
                {
                    NPCHPValues.AdjustMaxHP(npc, ref maxHP);

                    if (maxHP != npc.lifeMax)
                    {
                        npc.life = npc.lifeMax = maxHP;
                        if (BossHealthBarManager.Bars.Any(b => b.NPCIndex == npc.whoAmI))
                            BossHealthBarManager.Bars.First(b => b.NPCIndex == npc.whoAmI).InitialMaxLife = npc.lifeMax;

                        npc.Infernum().HasResetHP = true;
                        npc.netUpdate = true;
                    }
                }

                if (NPCBehaviorOverride.BehaviorOverrides.TryGetValue(npc.type, out var value))
                {
                    // Disable the effects of timed DR.
                    if (npc.Calamity().KillTime >= 1 && npc.Calamity().AITimer < npc.Calamity().KillTime)
                        npc.Calamity().AITimer = npc.Calamity().KillTime;

                    // If any boss NPC is active, apply Zen to nearby players to reduce the spawn rate.
                    if (Main.netMode != NetmodeID.Server && CalamityConfig.Instance.BossZen && (npc.Calamity().KillTime > 0 || npc.type == ModContent.NPCType<Draedon>() || npc.type == ModContent.NPCType<ThiccWaifu>()))
                    {
                        if (!Main.LocalPlayer.dead && Main.LocalPlayer.active && npc.WithinRange(Main.LocalPlayer.Center, 6400f))
                            Main.LocalPlayer.AddBuff(ModContent.BuffType<BossEffects>(), 2);
                    }

                    // Decrement each immune timer if it's greater than 0.
                    for (int i = 0; i < CalamityGlobalNPC.maxPlayerImmunities; i++)
                    {
                        if (npc.Calamity().dashImmunityTime[i] >= 1)
                            npc.Calamity().dashImmunityTime[i]--;
                    }

                    // Disable netOffset effects.
                    npc.netOffset = Vector2.Zero;

                    bool result = value.PreAI(npc);

                    // Disable the effects of certain unpredictable freeze debuffs.
                    // Time Bolt and a few other weapon-specific debuffs are not counted here since those are more deliberate weapon mechanics.
                    // That said, I don't know a single person who uses Time Bolt so it's probably irrelevant either way lol.
                    npc.buffImmune[ModContent.BuffType<Eutrophication>()] = true;
                    npc.buffImmune[ModContent.BuffType<GalvanicCorrosion>()] = true;
                    npc.buffImmune[ModContent.BuffType<GlacialState>()] = true;
                    npc.buffImmune[ModContent.BuffType<TemporalSadness>()] = true;
                    npc.buffImmune[BuffID.Webbed] = true;

                    return result;
                }
            }
            return base.PreAI(npc);
        }

        public override void OnKill(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            OnKillEvent?.Invoke(npc);

            // Trigger achievement checks.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active)
                    continue;

                Player player = Main.player[i];
                if (npc.boss || KillAllMinibossesAchievement.MinibossIDs.Contains(npc.type))
                    AchievementPlayer.ExtraUpdateHandler(player, AchievementUpdateCheck.NPCKill, npc.whoAmI);
            }

            // Check for whether to play the post mechs cutscene.
            if (!WorldSaveSystem.HasSeenPostMechsCutscene)
            {
                // If Prime was just killed, and the other two are also dead.
                if (npc.type == NPCID.SkeletronPrime && NPC.downedMechBoss1 && NPC.downedMechBoss2)
                    CutsceneManager.QueueCutscene(ModContent.GetInstance<DraedonPostMechsCutscene>());
                // If Destroyer was just killed, and the other two are dead.
                else if (npc.type == NPCID.TheDestroyer && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                    CutsceneManager.QueueCutscene(ModContent.GetInstance<DraedonPostMechsCutscene>());
                // If Twins were just killed, and the other two are dead.
                else if ((npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism) && NPC.downedMechBoss1 && NPC.downedMechBoss3)
                    CutsceneManager.QueueCutscene(ModContent.GetInstance<DraedonPostMechsCutscene>());
            }
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.CanHitPlayer(npc, target, ref cooldownSlot);

            // Exceptions that do not have behavior overrides but exist in the fight still.
            bool isSepulcher = npc.type == ModContent.NPCType<SepulcherHead>() || npc.type == ModContent.NPCType<SepulcherBody>() || npc.type == ModContent.NPCType<SepulcherBodyEnergyBall>() || npc.type == ModContent.NPCType<SepulcherTail>();
            bool isWofNPC = npc.type is NPCID.LeechHead or NPCID.LeechBody or NPCID.LeechTail or NPCID.TheHungry or NPCID.TheHungryII;
            bool isBee = (npc.type is NPCID.Bee or NPCID.BeeSmall) && NPC.AnyNPCs(NPCID.QueenBee);

            if ((NPCBehaviorOverride.BehaviorOverrides.TryGetValue(npc.type, out var value) && value.UseBossImmunityCooldownID) || isSepulcher || isWofNPC || isBee)
                cooldownSlot = ImmunityCooldownID.Bosses;

            if (npc.type == ModContent.NPCType<DevourerofGodsBody>() && OverridingListManager.Registered<DevourerofGodsHead>())
            {
                cooldownSlot = 0;
                return npc.alpha == 0;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            // Make Cryogen release ice particles when hit.
            if (npc.type == ModContent.NPCType<CryogenNPC>() && OverridingListManager.Registered(npc.type))
                CryogenBehaviorOverride.OnHitIceParticles(npc, projectile, hit.Crit);
        }

        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            HitEffectsEvent?.Invoke(npc, ref hit);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            // Loop through the StrikeNPC event subscribers and dynamically update the damage and such for every loop iteration.
            // If any of the subscribers instruct this method to return false and disable damage, that applies universally.
            // The reason the loop is necessary is because simply invoking the event and returning the result will only give back the result for the
            // last subscriber called, effectively ignoring whatever all the other subscribers say should happen.
            bool result = true;
            foreach (StrikeNPCDelegate d in StrikeNPCEvent.GetInvocationList().Cast<StrikeNPCDelegate>())
                result &= d.Invoke(npc, ref modifiers);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (InfernumMode.CanUseCustomAIs)
                BalancingChangesManager.ApplyFromProjectile(npc, ref modifiers, projectile);

            foreach (ModifyHitByProjectileDelegate subscription in ModifyHitByProjectileEvent.GetInvocationList().Cast<ModifyHitByProjectileDelegate>())
                subscription.Invoke(npc, projectile, ref modifiers);
        }

        public override bool CheckDead(NPC npc)
        {
            if (InfernumMode.CanUseCustomAIs && NPCBehaviorOverride.BehaviorOverrides.TryGetValue(npc.type, out var value))
                return value.CheckDead(npc);

            return base.CheckDead(npc);
        }

        public override bool CheckActive(NPC npc)
        {
            if (npc.Infernum().DisableNaturalDespawning)
                return false;

            if (!InfernumMode.CanUseCustomAIs)
                return base.CheckActive(npc);

            return base.CheckActive(npc);
        }

        public override void OnChatButtonClicked(NPC npc, bool firstButton)
        {
            Referenced<bool> wasGivenDungeonsCurse = Main.LocalPlayer.Infernum().GetRefValue<bool>("WasGivenDungeonsCurse");
            if (npc.type == NPCID.OldMan && firstButton && InfernumMode.CanUseCustomAIs && !wasGivenDungeonsCurse.Value)
            {
                Item.NewItem(npc.GetSource_FromThis(), Main.LocalPlayer.Hitbox, ModContent.ItemType<DungeonsCurse>());
                wasGivenDungeonsCurse.Value = true;
            }
        }
        #endregion
    }
}
