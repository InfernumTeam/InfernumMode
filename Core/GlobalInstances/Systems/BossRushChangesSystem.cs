using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.CalPlayer;
using CalamityMod.Events;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
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
using InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus;
using InfernumMode.Content.BehaviorOverrides.BossAIs.BoC;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DukeFishron;
using InfernumMode.Content.BehaviorOverrides.BossAIs.EyeOfCthulhu;
using InfernumMode.Content.BehaviorOverrides.BossAIs.KingSlime;
using InfernumMode.Content.BehaviorOverrides.BossAIs.PlaguebringerGoliath;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Polterghast;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.BehaviorOverrides.BossAIs.SlimeGod;
using InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using static CalamityMod.Events.BossRushEvent;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class BossRushChangesSystem : ModSystem
    {
        public static List<Boss> InfernumBosses { get; private set; }

        public static List<Boss> CalamityBosses { get; private set; }

        public static Dictionary<int, Action<NPC>> InfernumBossDeathEffects { get; private set; }

        public static Dictionary<int, Action<NPC>> CalamityBossDeathEffects { get; private set; }

        public static Dictionary<int, int[]> InfernumBossIDsAfterDeath { get; private set; }

        public const int MinBossRushDamage = 500;

        public override void OnModLoad()
        {
            // Cache the calamity boss order.
            //CalamityBosses = Bosses;

            InfernumPlayer.ModifyHitByNPCEvent += (InfernumPlayer player, NPC npc, ref Player.HurtModifiers modifiers) =>
            {
                if (modifiers.FinalDamage.Base <= 0f)
                    return;

                if (InfernumMode.CanUseCustomAIs && BossRushEvent.BossRushActive)
                    modifiers.FinalDamage.Base = Clamp(modifiers.FinalDamage.Base, MinBossRushDamage + Main.rand.Next(35), float.MaxValue);
            };

            InfernumPlayer.ModifyHitByProjectileEvent += (InfernumPlayer player, Projectile projectile, ref Player.HurtModifiers modifiers) =>
            {
                if (modifiers.FinalDamage.Base <= 0f)
                    return;

                if (InfernumMode.CanUseCustomAIs && BossRushEvent.BossRushActive)
                    modifiers.FinalDamage.Base = Clamp(modifiers.FinalDamage.Base, MinBossRushDamage + Main.rand.Next(35), float.MaxValue);
            };

            // Cache our own boss order.
            Bosses = new List<Boss>()
            {
                new Boss(NPCID.KingSlime, permittedNPCs: new int[] { ModContent.NPCType<Ninja>(), ModContent.NPCType<KingSlimeJewel>() }),

                new Boss(NPCID.EyeofCthulhu, TimeChangeContext.Night, permittedNPCs: new int[] { NPCID.ServantofCthulhu, ModContent.NPCType<ExplodingServant>() }),

                new Boss(NPCID.EaterofWorldsHead, permittedNPCs: new int[] { NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail, NPCID.VileSpit }),

                new Boss(NPCID.WallofFlesh, spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    NPC.SpawnWOF(player.position);
                }, permittedNPCs: new int[] { NPCID.WallofFleshEye, NPCID.LeechHead, NPCID.LeechBody, NPCID.LeechTail, NPCID.TheHungry, NPCID.TheHungryII }),

                new Boss(ModContent.NPCType<PerforatorHive>(), permittedNPCs: new int[] { ModContent.NPCType<PerforatorHeadLarge>(), ModContent.NPCType<PerforatorBodyLarge>(), ModContent.NPCType<PerforatorTailLarge>(),
                    ModContent.NPCType<PerforatorHeadMedium>(), ModContent.NPCType<PerforatorBodyMedium>(), ModContent.NPCType<PerforatorTailMedium>(), ModContent.NPCType<PerforatorHeadSmall>(),
                    ModContent.NPCType<PerforatorBodySmall>() ,ModContent.NPCType<PerforatorTailSmall>() }),

                new Boss(NPCID.QueenBee, permittedNPCs: new int[] { NPCID.HornetFatty, NPCID.HornetHoney, NPCID.HornetStingy }),

                new Boss(NPCID.QueenSlimeBoss),

                new Boss(ModContent.NPCType<AstrumAureus>(), TimeChangeContext.Night, permittedNPCs: ModContent.NPCType<AureusSpawn>()),

                new Boss(ModContent.NPCType<Crabulon>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int thePefectOne = NPC.NewNPC(new EntitySource_WorldEvent(), (int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[thePefectOne].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(thePefectOne);
                }, specialSpawnCountdown: 300, permittedNPCs: new int[] { ModContent.NPCType<CrabShroom>() }),

                new Boss(ModContent.NPCType<AquaticScourgeHead>(), permittedNPCs: new int[] { ModContent.NPCType<AquaticScourgeBody>(), ModContent.NPCType<AquaticScourgeBodyAlt>(),
                    ModContent.NPCType<AquaticScourgeTail>() }),

                new Boss(ModContent.NPCType<DesertScourgeHead>(), spawnContext: type =>
                {
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, ModContent.NPCType<DesertScourgeHead>());
                }, permittedNPCs: new int[] { ModContent.NPCType<DesertScourgeBody>(), ModContent.NPCType<DesertScourgeTail>() }),

                new Boss(ModContent.NPCType<ProfanedGuardianCommander>(), TimeChangeContext.Day,
                    permittedNPCs: new int[] { ModContent.NPCType<ProfanedGuardianDefender>(), ModContent.NPCType<ProfanedGuardianHealer>(), ModContent.NPCType<EtherealHand>(), ModContent.NPCType<HealerShieldCrystal>() }),
                
                // Tier 2.
                new Boss(ModContent.NPCType<StormWeaverHead>(), TimeChangeContext.Day, permittedNPCs: new int[] { ModContent.NPCType<StormWeaverBody>(), ModContent.NPCType<StormWeaverTail>(), }),

                new Boss(ModContent.NPCType<BrimstoneElemental>(), permittedNPCs: ModContent.NPCType<Brimling>()),

                new Boss(ModContent.NPCType<Anahita>(), TimeChangeContext.Day, permittedNPCs: new int[] { ModContent.NPCType<Leviathan>(),
                    ModContent.NPCType<AnahitasIceShield>(), NPCID.DetonatingBubble, ModContent.NPCType<RedirectingBubble>() }),

                new Boss(ModContent.NPCType<RavagerBody>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];

                    int ravager = NPC.NewNPC(new EntitySource_WorldEvent(), (int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[ravager].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(ravager);
                }, usesSpecialSound: true, permittedNPCs: new int[] { ModContent.NPCType<FlamePillar>(), ModContent.NPCType<RockPillar>(), ModContent.NPCType<RavagerLegLeft>(), ModContent.NPCType<RavagerLegRight>(),
                    ModContent.NPCType<RavagerClawLeft>(), ModContent.NPCType<RavagerClawRight>() }),

                new Boss(ModContent.NPCType<HiveMind>(), spawnContext: type =>
                {
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, type);
                }, permittedNPCs: new int[] { ModContent.NPCType<DankCreeper>(), ModContent.NPCType<DarkHeart>(), ModContent.NPCType<HiveBlob>(), ModContent.NPCType<HiveBlob2>() }),

                new Boss(NPCID.DukeFishron, spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int dukeFishron = NPC.NewNPC(new EntitySource_WorldEvent(), (int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[dukeFishron].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(dukeFishron);
                }, permittedNPCs: new int[] { NPCID.DetonatingBubble, NPCID.Sharkron, NPCID.Sharkron2, ModContent.NPCType<RedirectingBubble>() }),

                new Boss(ModContent.NPCType<Cryogen>()),

                new Boss(NPCID.BrainofCthulhu, permittedNPCs: new int[] { NPCID.Creeper, ModContent.NPCType<BrainIllusion>() }),

                new Boss(NPCID.Deerclops, permittedNPCs: new int[] { ModContent.NPCType<LightSnuffingHand>() }),

                new Boss(ModContent.NPCType<Signus>(), specialSpawnCountdown: 360),

                new Boss(ModContent.NPCType<Bumblefuck>(), TimeChangeContext.Day, permittedNPCs: new int[] { ModContent.NPCType<Bumblefuck2>(), NPCID.Spazmatism, NPCID.Retinazer }),

                new Boss(ModContent.NPCType<SlimeGodCore>(), permittedNPCs: new int[] { ModContent.NPCType<SlimeGodCore>(), ModContent.NPCType<EbonianPaladin>(), ModContent.NPCType<CrimulanPaladin>(), ModContent.NPCType<SplitCrimulanPaladin>(),
                    ModContent.NPCType<SplitEbonianPaladin>(), ModContent.NPCType<SplitBigSlime>() }),
                
                // Tier 3.
                new Boss(NPCID.SkeletronHead, TimeChangeContext.Night, type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int sans = NPC.NewNPC(new EntitySource_WorldEvent(), (int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[sans].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(sans);
                }, permittedNPCs: NPCID.SkeletronHand),

                new Boss(NPCID.Plantera, permittedNPCs: new int[] { NPCID.PlanterasTentacle, NPCID.PlanterasHook, NPCID.Spore }),

                new Boss(NPCID.TheDestroyer, TimeChangeContext.Night, specialSpawnCountdown: 300, permittedNPCs: new int[] { NPCID.TheDestroyerBody, NPCID.TheDestroyerTail, NPCID.Probe }),

                new Boss(ModContent.NPCType<PlaguebringerGoliath>(), permittedNPCs: new int[] { ModContent.NPCType<BuilderDroneSmall>(), ModContent.NPCType<BuilderDroneBig>(), ModContent.NPCType<SmallDrone>(),
                    ModContent.NPCType<PlagueMine>(), ModContent.NPCType<ExplosivePlagueCharger>() }),

                new Boss(ModContent.NPCType<AstrumDeusHead>(), TimeChangeContext.Night, type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];

                    SoundEngine.PlaySound(AstrumDeusHead.SpawnSound, player.Center);
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, type);
                }, usesSpecialSound: true, permittedNPCs: new int[] { ModContent.NPCType<AstrumDeusBody>(), ModContent.NPCType<AstrumDeusTail>(), ModContent.NPCType<DeusSpawn>() }),

                // Filler.
                new Boss(NPCID.CultistBoss),

                new Boss(NPCID.CultistBoss, spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int doctorLooneyTunes = NPC.NewNPC(new EntitySource_WorldEvent(), (int)player.Center.X, (int)player.Center.Y - 400, type, 1);
                    Main.npc[doctorLooneyTunes].direction = Main.npc[doctorLooneyTunes].spriteDirection = Math.Sign(player.Center.X - player.Center.X - 90f);
                    Main.npc[doctorLooneyTunes].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(doctorLooneyTunes);
                }, permittedNPCs: new int[] { NPCID.CultistBossClone, NPCID.CultistDragonHead, NPCID.CultistDragonBody1, NPCID.CultistDragonBody2, NPCID.CultistDragonBody3, NPCID.CultistDragonBody4,
                    NPCID.CultistDragonTail, NPCID.AncientCultistSquidhead, NPCID.AncientLight, NPCID.AncientDoom }),

                new Boss(NPCID.SkeletronPrime, TimeChangeContext.Night, permittedNPCs: new int[] { NPCID.PrimeCannon, NPCID.PrimeSaw, NPCID.PrimeVice, NPCID.PrimeLaser, NPCID.Probe }),

                new Boss(ModContent.NPCType<OldDuke>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int boomerDuke = NPC.NewNPC(new EntitySource_WorldEvent(), (int)(player.position.X + Main.rand.Next(-100, 101)), (int)(player.position.Y - 400f), type, 1);
                    Main.npc[boomerDuke].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(boomerDuke);
                }, permittedNPCs: new int[] { ModContent.NPCType<OldDukeToothBall>(), ModContent.NPCType<SulphurousSharkron>() }),

                new Boss(NPCID.Golem, TimeChangeContext.Day, type =>
                {
                    int sans = NPC.NewNPC(new EntitySource_WorldEvent(), (int)(Main.player[ClosestPlayerToWorldCenter].position.X + Main.rand.Next(-100, 101)), (int)(Main.player[ClosestPlayerToWorldCenter].position.Y - 400f), type, 1);
                    Main.npc[sans].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(sans);
                }, permittedNPCs: new int[] { NPCID.GolemFistLeft, NPCID.GolemFistRight, NPCID.GolemHead, NPCID.GolemHeadFree }),
                
                // Tier 4.
                new Boss(NPCID.HallowBoss, spawnContext: type =>
                {
                    int drawcodeGoddess = NPC.NewNPC(new EntitySource_WorldEvent(), (int)Main.player[ClosestPlayerToWorldCenter].Center.X, (int)(Main.player[ClosestPlayerToWorldCenter].Center.Y - 400f), type, 1);
                    Main.npc[drawcodeGoddess].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(drawcodeGoddess);
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Events.BossRushTierThreeEndText2", XerocTextColor);
                }, toChangeTimeTo: TimeChangeContext.Night),

                new Boss(NPCID.Spazmatism, TimeChangeContext.Night, type =>
                {
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, NPCID.Spazmatism);
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, NPCID.Retinazer);
                }, permittedNPCs: new int[] { NPCID.Retinazer }),

                new Boss(ModContent.NPCType<Polterghast>(), TimeChangeContext.Day, permittedNPCs: new int[]
                    { ModContent.NPCType<PhantomFuckYou>(), ModContent.NPCType<PolterghastHook>(), ModContent.NPCType<PolterPhantom>(), ModContent.NPCType<PolterghastLeg>() }),

                new Boss(NPCID.MoonLordCore, spawnContext: type =>
                {
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, type);
                }, permittedNPCs: new int[] { NPCID.MoonLordLeechBlob, NPCID.MoonLordHand, NPCID.MoonLordHead, NPCID.MoonLordFreeEye }),

                new Boss(ModContent.NPCType<CeaselessVoid>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int ceaselessVoid = NPC.NewNPC(new EntitySource_WorldEvent(), (int)player.Center.X, (int)(player.position.Y + 300f), type, 1);
                    CalamityUtils.BossAwakenMessage(ceaselessVoid);
                }, permittedNPCs: new int[] { ModContent.NPCType<DarkEnergy>() }),

                new Boss(ModContent.NPCType<CalamitasClone>(), TimeChangeContext.Night, specialSpawnCountdown: 420, dimnessFactor: 0.6f, permittedNPCs: new int[] { ModContent.NPCType<Cataclysm>(), ModContent.NPCType<Catastrophe>(),
                         ModContent.NPCType<SoulSeeker>() }),
                
                // Tier 5.
                new Boss(ModContent.NPCType<DevourerofGodsHead>(), TimeChangeContext.Day, type =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Events.BossRushTierFourEndText2", XerocTextColor);
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
                    {
                        if (Main.player[playerIndex].active)
                        {
                            Player player2 = Main.player[playerIndex];
                            if (player2.FindBuffIndex(ModContent.BuffType<DoGExtremeGravity>()) > -1)
                                player2.ClearBuff(ModContent.BuffType<DoGExtremeGravity>());
                        }
                    }

                    SoundEngine.PlaySound(DevourerofGodsHead.SpawnSound, player.Center);
                    NPC.SpawnOnPlayer(ClosestPlayerToWorldCenter, type);
                }, usesSpecialSound: true, permittedNPCs: new int[] { ModContent.NPCType<DevourerofGodsBody>(), ModContent.NPCType<DevourerofGodsTail>() }),

                new Boss(ModContent.NPCType<Yharon>(), TimeChangeContext.Day),

                new Boss(ModContent.NPCType<Providence>(), TimeChangeContext.Day, type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];

                    SoundEngine.PlaySound(Providence.SpawnSound, player.Center);
                    int prov = NPC.NewNPC(new EntitySource_WorldEvent(), (int)(player.position.X + Main.rand.Next(-500, 501)), (int)(player.position.Y - 250f), type, 1);
                    Main.npc[prov].timeLeft *= 20;
                    CalamityUtils.BossAwakenMessage(prov);
                }, usesSpecialSound: true, permittedNPCs: new int[] { ModContent.NPCType<ProvSpawnOffense>(), ModContent.NPCType<ProvSpawnHealer>(), ModContent.NPCType<ProvSpawnDefense>() }),

                new Boss(ModContent.NPCType<Apollo>(), spawnContext: type =>
                {
                    Player player = Main.player[ClosestPlayerToWorldCenter];
                    int apollo = NPC.NewNPC(new EntitySource_WorldEvent(), (int)player.Center.X, (int)player.Center.Y - 2400, type, 1);
                    Main.npc[apollo].Infernum().ExtraAI[ExoMechManagement.SecondaryMechNPCTypeIndex] = ModContent.NPCType<ThanatosHead>();

                }, permittedNPCs: new int[] { ModContent.NPCType<Artemis>(), ModContent.NPCType<Apollo>(), ModContent.NPCType<AresBody>(),
                    ModContent.NPCType<AresLaserCannon>(), ModContent.NPCType<AresTeslaCannon>(), ModContent.NPCType<AresPlasmaFlamethrower>(), ModContent.NPCType<AresGaussNuke>(), ModContent.NPCType<AresPulseCannon>(), ModContent.NPCType<AresEnergyKatana>(),
                    ModContent.NPCType<ThanatosHead>(), ModContent.NPCType<ThanatosBody1>(), ModContent.NPCType<ThanatosBody2>(), ModContent.NPCType<ThanatosTail>() }),

                new Boss(ModContent.NPCType<SupremeCalamitas>(), spawnContext: type =>
                {
                    SoundEngine.PlaySound(SupremeCalamitas.SpawnSound, Main.player[ClosestPlayerToWorldCenter].Center);
                    CalamityUtils.SpawnBossBetter(Main.player[ClosestPlayerToWorldCenter].Top - new Vector2(42f, 84f), type);
                }, dimnessFactor: 0.5f, permittedNPCs: new int[] { ModContent.NPCType<SepulcherArm>(), ModContent.NPCType<SepulcherHead>(), ModContent.NPCType<SepulcherBody>(),
                    ModContent.NPCType<SepulcherBodyEnergyBall>(), ModContent.NPCType<SepulcherTail>(),
                    ModContent.NPCType<SoulSeekerSupreme>(), ModContent.NPCType<BrimstoneHeart>(), ModContent.NPCType<SupremeCataclysm>(),
                    ModContent.NPCType<SupremeCatastrophe>(), ModContent.NPCType<ShadowDemon>() }),
            };

            // Cache our own boss death effects.
            BossDeathEffects = new Dictionary<int, Action<NPC>>()
            {
                [NPCID.WallofFlesh] = npc =>
                {
                    BringPlayersBackToSpawn();
                },
                [ModContent.NPCType<ProfanedGuardianCommander>()] = npc =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Events.BossRushTierOneEndText", XerocTextColor);
                    CreateTierAnimation(2);
                    BringPlayersBackToSpawn();
                },
                [ModContent.NPCType<SlimeGodCore>()] = npc =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Events.BossRushTierTwoEndText", XerocTextColor);
                    CreateTierAnimation(3);
                },
                [NPCID.Golem] = npc =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Events.BossRushTierThreeEndText", XerocTextColor);
                    CreateTierAnimation(4);
                },
                [ModContent.NPCType<CeaselessVoid>()] = npc =>
                {
                    BringPlayersBackToSpawn();
                },
                [ModContent.NPCType<CalamitasClone>()] = npc =>
                {
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Events.BossRushTierFourEndText", XerocTextColor);
                    CreateTierAnimation(5);
                },
                [ModContent.NPCType<Providence>()] = npc =>
                {
                    BringPlayersBackToSpawn();
                },
                [ModContent.NPCType<SupremeCalamitas>()] = npc =>
                {
                    CalamityUtils.KillAllHostileProjectiles();
                    HostileProjectileKillCounter = 3;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(new EntitySource_WorldEvent(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BossRushEndEffectThing>(), 0, 0f, Main.myPlayer);
                }
            };
            BossIDsAfterDeath = new Dictionary<int, int[]>
            {
                [ModContent.NPCType<Apollo>()] = new int[]
                {
                    ModContent.NPCType<Apollo>(),
                    ModContent.NPCType<Artemis>(),
                    ModContent.NPCType<AresBody>(),
                    ModContent.NPCType<AresLaserCannon>(),
                    ModContent.NPCType<AresPlasmaFlamethrower>(),
                    ModContent.NPCType<AresTeslaCannon>(),
                    ModContent.NPCType<AresGaussNuke>(),
                    ModContent.NPCType<AresPulseCannon>(),
                    ModContent.NPCType<ThanatosHead>(),
                    ModContent.NPCType<ThanatosBody1>(),
                    ModContent.NPCType<ThanatosBody2>(),
                    ModContent.NPCType<ThanatosTail>(),
                }
            };

            // TODO: Make these properly swapable without breaking.
            //Bosses = InfernumBosses;
            //BossDeathEffects = InfernumBossDeathEffects;
            //BossIDsAfterDeath = InfernumBossIDsAfterDeath;
        }

        internal static void BringPlayersBackToSpawn()
        {
            // Post-Wall of Flesh teleport back to spawn.
            for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
            {
                bool appropriatePlayer = Main.myPlayer == playerIndex;
                if (Main.player[playerIndex].active && appropriatePlayer)
                {
                    Main.player[playerIndex].Spawn(PlayerSpawnContext.RecallFromItem);
                    SoundEngine.PlaySound(TeleportSound with { Volume = 1.6f }, Main.player[playerIndex].Center);
                }
            }
        }

        public static void HandleTeleports()
        {
            Player player = Main.LocalPlayer;
            Vector2? teleportPosition = null;

            // Teleport the player to the garden for the guardians fight in boss rush.
            if (BossRushStage < Bosses.Count - 1 && !CalamityUtils.AnyBossNPCS())
            {
                if (CurrentlyFoughtBoss == NPCID.WallofFlesh && !player.ZoneUnderworldHeight)
                    teleportPosition = CalamityPlayer.GetUnderworldPosition(player);
                if (CurrentlyFoughtBoss == ModContent.NPCType<ProfanedGuardianCommander>() && !player.Infernum_Biome().ZoneProfaned)
                    teleportPosition = WorldSaveSystem.ProvidenceArena.TopLeft() * 16f + new Vector2(WorldSaveSystem.ProvidenceArena.Width * 3.2f - 16f, 800f);
                if (CurrentlyFoughtBoss == ModContent.NPCType<CeaselessVoid>() && !player.ZoneDungeon)
                    teleportPosition = WorldSaveSystem.ForbiddenArchiveCenter.ToWorldCoordinates() + Vector2.UnitY * 1032f;
                if (CurrentlyFoughtBoss == ModContent.NPCType<Providence>() && !player.Infernum_Biome().ZoneProfaned)
                    teleportPosition = WorldSaveSystem.ProvidenceArena.TopRight() * 16f + new Vector2(WorldSaveSystem.ProvidenceArena.Width * -3.2f - 16f, 800f);
            }

            if (BossRushStage < Bosses.Count && CurrentlyFoughtBoss == NPCID.SkeletronHead && player.ZoneUnderworldHeight)
                player.Spawn(PlayerSpawnContext.RecallFromItem);

            // Check to make sure the teleport position is valid.
            bool fightingProfanedBoss = CurrentlyFoughtBoss == ModContent.NPCType<ProfanedGuardianCommander>() || CurrentlyFoughtBoss == ModContent.NPCType<Providence>();
            if (fightingProfanedBoss && WorldSaveSystem.ProvidenceArena.TopLeft() == Vector2.Zero)
            {
                BossRushStage++;
                return;
            }
            if (CurrentlyFoughtBoss == ModContent.NPCType<CeaselessVoid>() && WorldSaveSystem.ForbiddenArchiveCenter == Point.Zero)
            {
                BossRushStage++;
                return;
            }

            // Teleport the player.
            if (teleportPosition.HasValue)
            {
                if (CurrentlyFoughtBoss != NPCID.SkeletronHead && WorldUtils.Find(teleportPosition.Value.ToTileCoordinates(), Searches.Chain(new Searches.Down(100), new Conditions.IsSolid()), out Point p))
                    teleportPosition = p.ToWorldCoordinates(8f, -32f);

                CalamityPlayer.ModTeleport(player, teleportPosition.Value, playSound: false, 7);
                SoundEngine.PlaySound(TeleportSound with { Volume = 1.6f }, player.Center);
            }
        }

        public static void SwapToOrder(bool infernumOrder)
        {
            if (infernumOrder)
            {
                Bosses = InfernumBosses;
                BossDeathEffects = InfernumBossDeathEffects;
                BossIDsAfterDeath = InfernumBossIDsAfterDeath;
            }
            else
            {
                Bosses = CalamityBosses;
                BossDeathEffects = CalamityBossDeathEffects;
                BossIDsAfterDeath.Clear();
            }
        }
    }
}
