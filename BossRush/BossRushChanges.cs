using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone;
using InfernumMode.BehaviorOverrides.BossAIs.DukeFishron;
using InfernumMode.BehaviorOverrides.BossAIs.Guardians;
using InfernumMode.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.BehaviorOverrides.BossAIs.Polterghast;
using InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using static CalamityMod.Events.BossRushEvent;

namespace InfernumMode.BossRush
{
    public static class BossRushChanges
    {
        public static void Load()
        {
            Bosses = new List<Boss>()
            {
                new Boss(ModContent.NPCType<DesertScourgeHead>(), spawnContext: type =>
                {
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, ModContent.NPCType<DesertScourgeHead>());
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, ModContent.NPCType<DesertNuisanceHead>());
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, ModContent.NPCType<DesertNuisanceHead>());
                }, permittedNPCs: new int[] { ModContent.NPCType<DesertScourgeBody>(), ModContent.NPCType<DesertScourgeTail>(), ModContent.NPCType<DesertNuisanceHead>(),
                    ModContent.NPCType<DesertNuisanceBody>(), ModContent.NPCType<DesertNuisanceTail>() }),

                new Boss(NPCID.SkeletronHead, TimeChangeContext.Night, type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int sans = NPC.NewNPC((int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[sans].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(sans);
                }, permittedNPCs: NPCID.SkeletronHand),

                new Boss(NPCID.WallofFlesh, spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    NPC.SpawnWOF(player.position);
                }, permittedNPCs: new int[] { NPCID.WallofFleshEye, NPCID.LeechHead, NPCID.LeechBody, NPCID.LeechTail, NPCID.TheHungry, NPCID.TheHungryII }),
                
                new Boss(NPCID.EyeofCthulhu, TimeChangeContext.Night, permittedNPCs: NPCID.ServantofCthulhu),

                new Boss(NPCID.DukeFishron, spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int dukeFishron = NPC.NewNPC((int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[dukeFishron].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(dukeFishron);
                }, permittedNPCs: new int[] { NPCID.DetonatingBubble, NPCID.Sharkron, NPCID.Sharkron2, ModContent.NPCType<RedirectingBubble>() }),

                new Boss(NPCID.Golem, TimeChangeContext.Day, type =>
                {
                    int shittyStatueBoss = NPC.NewNPC((int)(Main.player[ClosestPlayerToWorldCenter].position.X + Main.rand.Next(-100, 101)), (int)(Main.player[ClosestPlayerToWorldCenter].position.Y - 400f), type, 1);
                    Main.npc[shittyStatueBoss].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(shittyStatueBoss);
                }, permittedNPCs: new int[] { NPCID.GolemFistLeft, NPCID.GolemFistRight, NPCID.GolemHead, NPCID.GolemHeadFree }),

                new Boss(NPCID.BrainofCthulhu, permittedNPCs: NPCID.Creeper),

                new Boss(NPCID.KingSlime, permittedNPCs: new int[] { NPCID.BlueSlime, NPCID.YellowSlime, NPCID.PurpleSlime, NPCID.RedSlime, NPCID.GreenSlime, NPCID.RedSlime,
                    NPCID.IceSlime, NPCID.UmbrellaSlime, NPCID.Pinky, NPCID.SlimeSpiked, NPCID.RainbowSlime, ModContent.NPCType<KingSlimeJewel>() }),

                new Boss(NPCID.EaterofWorldsHead, permittedNPCs: new int[] { NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail, NPCID.VileSpit }),

                new Boss(ModContent.NPCType<PerforatorHive>(), permittedNPCs: new int[] { ModContent.NPCType<PerforatorHeadLarge>(), ModContent.NPCType<PerforatorBodyLarge>(), ModContent.NPCType<PerforatorTailLarge>(),
                    ModContent.NPCType<PerforatorHeadMedium>(), ModContent.NPCType<PerforatorBodyMedium>(), ModContent.NPCType<PerforatorTailMedium>(), ModContent.NPCType<PerforatorHeadSmall>(),
                    ModContent.NPCType<PerforatorBodySmall>() ,ModContent.NPCType<PerforatorTailSmall>() }),

                new Boss(NPCID.QueenBee),

                new Boss(ModContent.NPCType<AstrumAureus>(), TimeChangeContext.Night, permittedNPCs: ModContent.NPCType<AureusSpawn>()),

                new Boss(ModContent.NPCType<CrabulonIdle>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int thePefectOne = NPC.NewNPC((int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[thePefectOne].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(thePefectOne);
                }, specialSpawnCountdown: 300, permittedNPCs: ModContent.NPCType<CrabShroom>()),

                new Boss(NPCID.Plantera, permittedNPCs: new int[] { NPCID.PlanterasTentacle, NPCID.PlanterasHook, NPCID.Spore }),

                new Boss(ModContent.NPCType<BrimstoneElemental>(), permittedNPCs: ModContent.NPCType<Brimling>()),
                
                new Boss(ModContent.NPCType<RavagerBody>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];

                    Main.PlaySound(SoundID.Roar, player.position, 2);
                    int ravager = NPC.NewNPC((int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[ravager].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(ravager);
                }, usesSpecialSound: true, permittedNPCs: new int[] { ModContent.NPCType<FlamePillar>(), ModContent.NPCType<RockPillar>(), ModContent.NPCType<RavagerLegLeft>(), ModContent.NPCType<RavagerLegRight>(),
                    ModContent.NPCType<RavagerClawLeft>(), ModContent.NPCType<RavagerClawRight>() }),

                new Boss(NPCID.TheDestroyer, TimeChangeContext.Night, specialSpawnCountdown: 300, permittedNPCs: new int[] { NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, NPCID.Probe }),

                new Boss(ModContent.NPCType<Polterghast>(), TimeChangeContext.Day, permittedNPCs: new int[] 
                    { ModContent.NPCType<PhantomFuckYou>(), ModContent.NPCType<PolterghastHook>(), ModContent.NPCType<PolterPhantom>(), ModContent.NPCType<EerieLimb>() }),

                new Boss(ModContent.NPCType<AquaticScourgeHead>(), permittedNPCs: new int[] { ModContent.NPCType<AquaticScourgeBody>(), ModContent.NPCType<AquaticScourgeBodyAlt>(),
                    ModContent.NPCType<AquaticScourgeTail>(), ModContent.NPCType<AquaticParasite>(), ModContent.NPCType<AquaticParasite>(), ModContent.NPCType<AquaticSeekerHead>(),
                    ModContent.NPCType<AquaticSeekerBody>(), ModContent.NPCType<AquaticSeekerTail>() }),

                new Boss(ModContent.NPCType<ProfanedGuardianBoss>(), TimeChangeContext.Day, 
                    permittedNPCs: new int[] { ModContent.NPCType<ProfanedGuardianBoss2>(), ModContent.NPCType<ProfanedGuardianBoss3>(), ModContent.NPCType<EtherealHand>() }),

                new Boss(ModContent.NPCType<CeaselessVoid>(), permittedNPCs: ModContent.NPCType<DarkEnergy>()),

                new Boss(ModContent.NPCType<Cryogen>(), permittedNPCs: new int[] { ModContent.NPCType<CryogenIce>(), ModContent.NPCType<IceMass>(), ModContent.NPCType<Cryocore>(), ModContent.NPCType<Cryocore2>() }),

                new Boss(NPCID.MoonLordCore, spawnContext: type =>
                {
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, type);
                }, permittedNPCs: new int[] { NPCID.MoonLordLeechBlob, NPCID.MoonLordHand, NPCID.MoonLordHead, NPCID.MoonLordFreeEye, ModContent.NPCType<EldritchSeal>() }),

                new Boss(NPCID.SkeletronPrime, TimeChangeContext.Night, permittedNPCs: new int[] { NPCID.PrimeCannon, NPCID.PrimeSaw, NPCID.PrimeVice, NPCID.PrimeLaser, NPCID.Probe }),

                new Boss(ModContent.NPCType<HiveMind>(), spawnContext: type =>
                {
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, type);
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.BossRushTierThreeEndText2", XerocTextColor);
                }, permittedNPCs: new int[] { ModContent.NPCType<DankCreeper>(), ModContent.NPCType<DarkHeart>(), ModContent.NPCType<HiveBlob>(), ModContent.NPCType<HiveBlob2>() }),

                new Boss(ModContent.NPCType<CalamitasRun3>(), TimeChangeContext.Night, specialSpawnCountdown: 420, dimnessFactor: 0.6f, permittedNPCs: new int[] { ModContent.NPCType<CalamitasRun>(), ModContent.NPCType<CalamitasRun2>(),
                    ModContent.NPCType<LifeSeeker>(), ModContent.NPCType<SoulSeeker>(), ModContent.NPCType<SoulSeeker2>() }),

                new Boss(ModContent.NPCType<StormWeaverHead>(), TimeChangeContext.Day, permittedNPCs: new int[] { ModContent.NPCType<StormWeaverBody>(), ModContent.NPCType<StormWeaverTail>(),  }),

                new Boss(ModContent.NPCType<Siren>(), TimeChangeContext.Day, permittedNPCs: new int[] { ModContent.NPCType<Leviathan>(), ModContent.NPCType<AquaticAberration>(), ModContent.NPCType<Parasea>(),
                    ModContent.NPCType<SirenIce>(), NPCID.DetonatingBubble, ModContent.NPCType<RedirectingBubble>() }),

                new Boss(NPCID.Spazmatism, TimeChangeContext.Night, type =>
                {
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, NPCID.Spazmatism);
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, NPCID.Retinazer);
                }, permittedNPCs: NPCID.Retinazer),

                new Boss(ModContent.NPCType<PlaguebringerGoliath>(), permittedNPCs: new int[] { ModContent.NPCType<PlagueBeeG>(), ModContent.NPCType<PlagueBeeLargeG>(), ModContent.NPCType<PlagueHomingMissile>(),
                    ModContent.NPCType<PlagueMine>(), ModContent.NPCType<PlaguebringerShade>() }),

                new Boss(ModContent.NPCType<AstrumDeusHeadSpectral>(), TimeChangeContext.Night, type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];

                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AstrumDeusSpawn"), player.Center);
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, type);
                }, usesSpecialSound: true, permittedNPCs: new int[] { ModContent.NPCType<AstrumDeusBodySpectral>(), ModContent.NPCType<AstrumDeusTailSpectral>() }),

                new Boss(ModContent.NPCType<Signus>(), specialSpawnCountdown: 360, permittedNPCs: new int[] { ModContent.NPCType<CosmicLantern>(), ModContent.NPCType<SignusBomb>() }),

                new Boss(ModContent.NPCType<Bumblefuck>(), TimeChangeContext.Day, permittedNPCs: new int[] { ModContent.NPCType<Bumblefuck2>(), NPCID.Spazmatism, NPCID.Retinazer }),

                new Boss(ModContent.NPCType<OldDuke>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int boomerDuke = NPC.NewNPC((int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[boomerDuke].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(boomerDuke);
                }, permittedNPCs: new int[] { ModContent.NPCType<OldDukeToothBall>(), ModContent.NPCType<OldDukeSharkron>() }),

                new Boss(NPCID.CultistBoss, spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int doctorLooneyTunes = NPC.NewNPC((int)player.Center.X, (int)player.Center.Y - 400, type, 1);
                    Main.npc[doctorLooneyTunes].direction = Main.npc[doctorLooneyTunes].spriteDirection = Math.Sign(player.Center.X - player.Center.X - 90f);
                    Main.npc[doctorLooneyTunes].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(doctorLooneyTunes);
                }, permittedNPCs: new int[] { NPCID.CultistBossClone, NPCID.CultistDragonHead, NPCID.CultistDragonBody1, NPCID.CultistDragonBody2, NPCID.CultistDragonBody3, NPCID.CultistDragonBody4,
                    NPCID.CultistDragonTail, NPCID.AncientCultistSquidhead, NPCID.AncientLight, NPCID.AncientDoom }),

                new Boss(ModContent.NPCType<SlimeGodCore>(), permittedNPCs: new int[] { ModContent.NPCType<SlimeGod>(), ModContent.NPCType<SlimeGodRun>(), ModContent.NPCType<SlimeGodSplit>(), ModContent.NPCType<SlimeGodRunSplit>(),
                    ModContent.NPCType<SlimeSpawnCorrupt>(), ModContent.NPCType<SlimeSpawnCorrupt2>(), ModContent.NPCType<SlimeSpawnCrimson>(), ModContent.NPCType<SlimeSpawnCrimson2>() }),

                new Boss(ModContent.NPCType<Providence>(), TimeChangeContext.Day, type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];

                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.BossRushTierFourEndText2", XerocTextColor);
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceSpawn"), player.Center);
                    int prov = NPC.NewNPC((int)(player.position.X + Main.rand.Next(-500, 501)), (int)(player.position.Y - 250f), type, 1);
                    Main.npc[prov].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(prov);
                }, usesSpecialSound: true, permittedNPCs: new int[] { ModContent.NPCType<ProvSpawnOffense>(), ModContent.NPCType<ProvSpawnHealer>(), ModContent.NPCType<ProvSpawnDefense>() }),

                new Boss(ModContent.NPCType<DevourerofGodsHead>(), TimeChangeContext.Day, type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
                    {
                        if (Main.player[playerIndex].active)
                        {
                            Player player2 = Main.player[playerIndex];
                            if (player2.FindBuffIndex(ModContent.BuffType<ExtremeGravity>()) > -1)
                                player2.ClearBuff(ModContent.BuffType<ExtremeGravity>());
                        }
                    }

                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DevourerSpawn"), player.Center);
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, type);
                }, usesSpecialSound: true, permittedNPCs: new int[] { ModContent.NPCType<DevourerofGodsBody>(), ModContent.NPCType<DevourerofGodsTail>() }),

                new Boss(ModContent.NPCType<Yharon>(), TimeChangeContext.Day, permittedNPCs: new int[] { ModContent.NPCType<DetonatingFlare>(), ModContent.NPCType<DetonatingFlare2>() }),

                new Boss(ModContent.NPCType<Apollo>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    NPC.NewNPC((int)player.Center.X, (int)player.Center.Y - 2400, type, 1);
                }, permittedNPCs: new int[] { ModContent.NPCType<Artemis>(), ModContent.NPCType<Apollo>(), ModContent.NPCType<AresBody>(),
                    ModContent.NPCType<AresLaserCannon>(), ModContent.NPCType<AresTeslaCannon>(), ModContent.NPCType<AresPlasmaFlamethrower>(), ModContent.NPCType<AresGaussNuke>(),
                    ModContent.NPCType<ThanatosHead>(), ModContent.NPCType<ThanatosBody1>(), ModContent.NPCType<ThanatosBody2>(), ModContent.NPCType<ThanatosTail>() }),

                new Boss(ModContent.NPCType<SupremeCalamitas>(), spawnContext: type =>
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SupremeCalamitasSpawn"), Main.player[ClosestPlayerToWorldCenter].Center);
                    CalamityUtils.SpawnBossBetter(Main.player[ClosestPlayerToWorldCenter].Top - new Vector2(42f, 84f), type);
                }, dimnessFactor: 0.6f, permittedNPCs: new int[] { ModContent.NPCType<SCalWormArm>(), ModContent.NPCType<SCalWormHead>(), ModContent.NPCType<SCalWormBody>(), 
                    ModContent.NPCType<SCalWormBodyWeak>(), ModContent.NPCType<SCalWormTail>(),
                    ModContent.NPCType<SoulSeekerSupreme>(), ModContent.NPCType<BrimstoneHeart>(), ModContent.NPCType<SupremeCataclysm>(), 
                    ModContent.NPCType<SupremeCatastrophe>(), ModContent.NPCType<ShadowDemon>() }),
            };

            BossDeathEffects = new Dictionary<int, Action<NPC>>()
            {
                [NPCID.KingSlime] = npc =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.BossRushTierOneEndText", XerocTextColor);
                },
                [ModContent.NPCType<RavagerBody>()] = npc =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.BossRushTierTwoEndText", XerocTextColor);
                },
                [NPCID.SkeletronPrime] = npc =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.BossRushTierThreeEndText", XerocTextColor);
                },
                [ModContent.NPCType<SlimeGodCore>()] = npc =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.BossRushTierFourEndText", XerocTextColor);
                },
                [ModContent.NPCType<SupremeCalamitas>()] = npc =>
                {
                    CalamityUtils.KillAllHostileProjectiles();
                    CalamityWorld.bossRushHostileProjKillCounter = 3;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<BossRushEndEffectThing>(), 0, 0f, Main.myPlayer);
                }
            };
        }
    }
}
