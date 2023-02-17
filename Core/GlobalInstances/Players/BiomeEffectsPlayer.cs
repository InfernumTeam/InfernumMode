using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.Biomes;
using InfernumMode.Content.Subworlds;
using InfernumMode.Content.Tiles;
using InfernumMode.Content.WorldGeneration;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class BiomeEffectsPlayer : ModPlayer
    {
        // This exists because the crystal door in the profaned temple uses the shatter timer as a ref local for readability, which is not possible on properties.
        internal int providenceRoomShatterTimer;

        public int ProvidenceRoomShatterTimer
        {
            get => providenceRoomShatterTimer;
            set => providenceRoomShatterTimer = value;
        }

        public bool ProfanedTempleAnimationHasPlayed
        {
            get;
            set;
        }

        public bool ReturnToPositionBeforeSubworld
        {
            get;
            set;
        }

        public Vector2 PositionBeforeEnteringSubworld
        {
            get;
            set;
        }

        public bool ProfanedLavaFountain
        {
            get;
            set;
        }

        public float MapObscurityInterpolant
        {
            get;
            set;
        }

        public bool ZoneProfaned => Player.InModBiome(ModContent.GetInstance<ProfanedTempleBiome>()) && !WeakReferenceSupport.InAnySubworld();

        public bool InLayer3HadalZone => CustomAbyss.InsideOfLayer3HydrothermalZone(Player.Center.ToTileCoordinates());

        public bool InProfanedArena
        {
            get
            {
                Rectangle arena = WorldSaveSystem.ProvidenceArena;
                arena.X *= 16;
                arena.Y *= 16;
                arena.Width *= 16;
                arena.Height *= 16;

                return Player.Hitbox.Intersects(arena) && !WeakReferenceSupport.InAnySubworld();
            }
        }

        public bool InProfanedArenaAntiCheeseZone
        {
            get
            {
                Rectangle arena = WorldSaveSystem.ProvidenceArena;
                arena.X *= 16;
                arena.Y *= 16;
                arena.Width *= 16;
                arena.Height *= 16;

                // A bit of extra space is given to ensure that the player can't just be a couple blocks away from the temple to abuse long-ranged things, such
                // as the Crystyl Crusher ray.
                arena.Inflate(1080, 1080);

                return Player.Hitbox.Intersects(arena) && !WeakReferenceSupport.InAnySubworld();
            }
        }

        public override void ResetEffects()
        {
            // Disable block placement and destruction in the profaned temple and lost colosseum.
            if (InProfanedArenaAntiCheeseZone || SubworldSystem.IsActive<LostColosseum>())
            {
                Player.AddBuff(BuffID.NoBuilding, 10);
                Player.noBuilding = true;
            }
        }

        public override void PreUpdate()
        {
            // Constantly redefine whether the player is near a profaned fountain. This influences the color of lava to be the same as in the Profaned Temple.
            // NearbyEffects and other things that rely on Terraria's natural scene system had an insufficient range, hence why it's handled in here specifically.
            ProfanedLavaFountain = false;

            int profanedFountainID = ModContent.TileType<ProfanedFountainTile>();
            for (int dx = -75; dx < 75; dx++)
            {
                for (int dy = -75; dy < 75; dy++)
                {
                    int x = (int)(Player.Center.X / 16f + dx);
                    int y = (int)(Player.Center.Y / 16f + dy);
                    if (!WorldGen.InWorld(x, y))
                        continue;

                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == profanedFountainID && Main.tile[x, y].TileFrameX < 36)
                    {
                        ProfanedLavaFountain = true;
                        return;
                    }
                }
            }

            // Make the map turn black if in the final layer of the abyss.
            bool obscureMap = Player.Calamity().ZoneAbyssLayer4 && !NPC.AnyNPCs(ModContent.NPCType<AdultEidolonWyrmHead>());
            MapObscurityInterpolant = MathHelper.Clamp(MapObscurityInterpolant + obscureMap.ToDirectionInt() * 0.008f, 0f, 1f);
            
            // Disable Acid Rain in the Lost Colosseum.
            if (SubworldSystem.IsActive<LostColosseum>())
                Player.Calamity().noStupidNaturalARSpawns = true;
        }

        // Ensure that the profaned temple title card animation state is saved after the player leaves the world.
        public override void SaveData(TagCompound tag)
        {
            tag["ProfanedTempleAnimationHasPlayed"] = ProfanedTempleAnimationHasPlayed;
        }

        public override void LoadData(TagCompound tag)
        {
            ProfanedTempleAnimationHasPlayed = tag.GetBool("ProfanedTempleAnimationHasPlayed");
        }

        public override void PostUpdate()
        {
            // Don't see the invisible blocks in the Colosseum build, since they are used to create invisible barriers and look really unnatural when the illusion is pierced.
            if (SubworldSystem.IsActive<LostColosseum>())
                Player.CanSeeInvisibleBlocks = false;

            // Keep the player out of the providence arena if the door is around.
            if (WorldSaveSystem.ProvidenceDoorXPosition != 0 && !WorldSaveSystem.HasProvidenceDoorShattered && Player.Bottom.Y >= (Main.maxTilesY - 220f) * 16f)
            {
                bool passedDoor = false;
                float doorX = WorldSaveSystem.ProvidenceDoorXPosition;
                while (Player.Right.X >= doorX || passedDoor && Collision.SolidCollision(Player.TopLeft, Player.width, Player.height))
                {
                    Player.velocity.X = 0f;
                    Player.position.X -= 0.1f;
                    passedDoor = true;
                }
            }

            if (ReturnToPositionBeforeSubworld && !Main.gameMenu)
            {
                Player.Spawn(PlayerSpawnContext.RecallFromItem);
                Main.LocalPlayer.Center = PositionBeforeEnteringSubworld;

                NPC.ResetNetOffsets();
                Main.BlackFadeIn = 255;
                Lighting.Clear();
                Main.screenLastPosition = Main.screenPosition;
                Main.screenPosition.X = Player.Center.X - Main.screenWidth * 0.5f;
                Main.screenPosition.Y = Player.Center.Y - Main.screenHeight * 0.5f;
                Main.instantBGTransitionCounter = 10;

                ReturnToPositionBeforeSubworld = false;
            }

            if (CalamityPlayer.areThereAnyDamnBosses && Player.Calamity().momentumCapacitorBoost > 1.8f)
                Player.Calamity().momentumCapacitorBoost = 1.8f;

            // Reset the screen distortion shader for the next frame.
            if (Main.netMode != NetmodeID.Server && InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive())
                InfernumEffectsRegistry.ScreenDistortionScreenShader.Deactivate();

            // Check whether to change the boss rush list.

            // NOTE -- Toasty, are you sure this should be done in player hooks? It seems to me like this would be better suited for a ModSystem, given that Boss Rush is a world-centric event.

            //if (WorldSaveSystem.InfernumMode && !BossRushChanges.InfernumChangesActive)
            //    BossRushChanges.SwapToOrder(true);
            //else if (!WorldSaveSystem.InfernumMode && BossRushChanges.InfernumChangesActive)
            //    BossRushChanges.SwapToOrder(false);            
        }

        public override void UpdateDead()
        {
            // Ensure that the player respawns at the campfire in the Lost Colosseum.
            if (SubworldSystem.IsActive<LostColosseum>())
            {
                LostColosseum.HasBereftVassalAppeared = false;
                Main.spawnTileX = LostColosseum.CampfirePosition.X;
                Main.spawnTileY = LostColosseum.CampfirePosition.Y;
            }
        }
    }
}