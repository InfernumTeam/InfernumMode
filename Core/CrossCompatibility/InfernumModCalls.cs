using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items;
using CalamityMod.NPCs.AdultEidolonWyrm;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.TrackedMusic;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.CrossCompatibility
{
    public class InfernumModCalls
    {
        public static object Call(params object[] args)
        {
            if (args is null || args.Length <= 0)
                return new ArgumentException("ERROR: No function name specified. First argument must be a function name.");
            if (args[0] is not string)
                return new ArgumentException("ERROR: First argument must be a string function name.");

            string methodName = args[0].ToString();
            switch (methodName)
            {
                case "GetInfernumActive":
                    return WorldSaveSystem.InfernumMode;
                case "SetInfernumActive":
                    WorldSaveSystem.InfernumMode = (bool)args[1];
                    break;
                case "BopHeadToMusic":
                    Player player = (Player)args[1];
                    float headRotationTime = (float)args[2];

                    // Return the head rotation to its intended angle if there is no music high point being played.
                    if (!TrackedMusicManager.TryGetSongInformation(out var songInfo) || !songInfo.HeadphonesHighPoints.Any(s => s.WithinRange(TrackedMusicManager.SongElapsedTime)) || player.velocity.Length() > 0.1f)
                    {
                        player.headRotation = player.headRotation.AngleTowards(0f, 0.042f);
                        headRotationTime = 0f;
                        return headRotationTime;
                    }

                    // Jam to the music in accordance with its BMP.
                    float beatTime = MathHelper.TwoPi * songInfo.BeatsPerMinute / 3600f;
                    if (songInfo.HeadBobState == BPMHeadBobState.Half)
                        beatTime *= 0.5f;
                    if (songInfo.HeadBobState == BPMHeadBobState.Quarter)
                        beatTime *= 0.25f;

                    headRotationTime += beatTime;
                    player.headRotation = MathF.Sin(headRotationTime) * 0.276f;
                    player.eyeHelper.BlinkBecausePlayerGotHurt();
                    return headRotationTime;
                case "CanPlayMusicForNPC":
                    int npcID = (int)args[1];
                    return CanPlayMusicForNPC(npcID);
                case "RegisterAsSoulHeadphones":
                    Item item = (Item)args[1];
                    item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
                    item.rare = ModContent.RarityType<InfernumSoulDrivenHeadphonesRarity>();
                    item.Infernum_Tooltips().DeveloperItem = true;
                    break;
                case "CanPlaySoulHeadphonesMusic":
                    string musicName = (string)args[1];
                    return musicName switch
                    {
                        "BereftVassal" => WorldSaveSystem.DownedBereftVassal,
                        "ExoMechs" => DownedBossSystem.downedExoMechs,
                        _ => (object)false,
                    };
            }
            return null;
        }

        public static bool CanPlayMusicForNPC(int npcID)
        {
            if (BossRushEvent.BossRushActive)
                return false;

            // Minibosses.
            if (npcID is NPCID.BigMimicCorruption or NPCID.BigMimicCrimson or NPCID.BigMimicHallow)
            {
                int npcIndex = NPC.FindFirstNPC(npcID);
                if (npcIndex < 0)
                    return false;
                return Main.npc[npcIndex].ai[2] >= 1f;
            }
            if (npcID is NPCID.SandElemental)
                return NPC.AnyNPCs(npcID);
            if (npcID == ModContent.NPCType<GiantClam>())
            {
                int npcIndex = NPC.FindFirstNPC(npcID);
                if (npcIndex < 0)
                    return false;
                return Main.npc[npcIndex].Infernum().ExtraAI[5] >= 1f;
            }

            // Bosses.
            if (npcID == NPCID.KingSlime)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.EyeofCthulhu)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.BrainofCthulhu)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.EaterofWorldsHead)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.QueenBee)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.SkeletronHead)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.WallofFlesh)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.BloodNautilus)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.QueenSlimeBoss)
                return NPC.AnyNPCs(npcID);
            if (npcID is NPCID.Retinazer or NPCID.Spazmatism or NPCID.SkeletronPrime or NPCID.TheDestroyer)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.Plantera)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.Golem)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.HallowBoss)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.DukeFishron)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.CultistBoss)
                return NPC.AnyNPCs(npcID);
            if (npcID == NPCID.MoonLordCore)
            {
                int npcIndex = NPC.FindFirstNPC(npcID);
                if (npcIndex < 0)
                    return false;
                return Main.npc[npcIndex].Infernum().ExtraAI[10] >= MoonLordCoreBehaviorOverride.IntroSoundLength;
            }
            if (npcID == ModContent.NPCType<AdultEidolonWyrmHead>())
                return NPC.AnyNPCs(npcID);
            if (npcID == ModContent.NPCType<Draedon>())
                return NPC.AnyNPCs(npcID) || InfernumMode.DraedonThemeTimer > 0;
            if (npcID == ModContent.NPCType<SupremeCalamitas>())
                return NPC.AnyNPCs(npcID);

            return false;
        }
    }
}
