using System.Linq;
using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.Systems;
using InfernumMode.Assets.Effects;
using InfernumMode.Content.Biomes;
using InfernumMode.Content.Subworlds;
using InfernumMode.Content.WorldGeneration;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class BiomeEffectsPlayer : ModPlayer
    {
        // These exist because the crystal door in the profaned temple uses the shatter timer as a ref local for readability, which is not possible on properties.
        internal int providenceRoomShatterTimer;

        internal float lostColosseumTeleportInterpolant;

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

        public bool CosmicBackgroundEffect
        {
            get;
            set;
        }

        public bool AstralMonolithEffect
        {
            get;
            set;
        }

        public float MapObscurityInterpolant
        {
            get;
            set;
        }

        public bool ZoneProfaned => Player.InModBiome(ModContent.GetInstance<ProfanedTempleBiome>()) && !SubworldSystem.IsActive<LostColosseum>();

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

        internal float LostColosseumTeleportInterpolant
        {
            get => lostColosseumTeleportInterpolant;
            set => lostColosseumTeleportInterpolant = value;
        }

        public override void ResetEffects()
        {
            //// Disable block placement and destruction in the profaned temple and lost colosseum.
            //if (InProfanedArenaAntiCheeseZone || SubworldSystem.IsActive<LostColosseum>())
            //{
            //    Player.AddBuff(BuffID.NoBuilding, 10);
            //    Player.noBuilding = true;
            //}
        }

        public override void PreUpdate()
        {
            // Disable the layer 4 abyss water tiles requirement due to how open the biome is overall.
            if (InfernumMode.CanUseCustomAIs)
                BiomeTileCounterSystem.Layer4Tiles = 250;

            // Make the map turn black if in the final layer of the abyss.
            bool obscureMap = Player.Calamity().ZoneAbyssLayer4 && !NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>());
            MapObscurityInterpolant = Clamp(MapObscurityInterpolant + obscureMap.ToDirectionInt() * 0.008f, 0f, 1f);

            // Disable Acid Rain in the Lost Colosseum.
            if (SubworldSystem.IsActive<LostColosseum>())
                Player.Calamity().noStupidNaturalARSpawns = true;

            LostColosseumTeleportInterpolant = Clamp(LostColosseumTeleportInterpolant - 0.008f, 0f, 1f);
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
            if (Main.netMode != NetmodeID.Server)
            {
                if (InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive())
                    InfernumEffectsRegistry.ScreenDistortionScreenShader.Deactivate();
                if (InfernumEffectsRegistry.ScreenBorderShader.IsActive())
                {
                    InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseOpacity(0f);
                    InfernumEffectsRegistry.ScreenBorderShader.GetShader().UseIntensity(0f);
                    InfernumEffectsRegistry.ScreenBorderShader.Deactivate();
                }
            }
            UpdatePortalDistortionEffects();

            // Check whether to change the boss rush list.

            // NOTE -- Toasty, are you sure this should be done in player hooks? It seems to me like this would be better suited for a ModSystem, given that Boss Rush is a world-centric event.

            //if (WorldSaveSystem.InfernumMode && !BossRushChanges.InfernumChangesActive)
            //    BossRushChanges.SwapToOrder(true);
            //else if (!WorldSaveSystem.InfernumMode && BossRushChanges.InfernumChangesActive)
            //    BossRushChanges.SwapToOrder(false);            
        }

        public void UpdatePortalDistortionEffects()
        {
            if (LostColosseumTeleportInterpolant <= 0f || Main.netMode == NetmodeID.Server || Main.myPlayer != Player.whoAmI)
                return;

            if (!InfernumEffectsRegistry.ScreenDistortionScreenShader.IsActive())
                Filters.Scene.Activate("InfernumMode:ScreenDistortion", Player.Center);
            PrepareScreenDistortionShaderParameters();
            if (lostColosseumTeleportInterpolant >= 0.67f)
                MoonlordDeathDrama.RequestLight(1f, Player.Center);
        }

        public void PrepareScreenDistortionShaderParameters()
        {
            InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().UseImage("Images/Extra_193");
            InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["distortionAmount"].SetValue(Pow(LostColosseumTeleportInterpolant, 0.89f) * 50f);
            InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["uvSampleFactors"].SetValue(new Vector2(1f, 5f));
            InfernumEffectsRegistry.ScreenDistortionScreenShader.GetShader().Shader.Parameters["wiggleSpeed"].SetValue(6f);
        }

        public override void UpdateDead()
        {
            // Ensure that the player respawns at the campfire in the Lost Colosseum.
            if (SubworldSystem.IsActive<LostColosseum>())
            {
                // Only mark this if no other players are alive.
                if (!Main.player.Any(player => !player.dead && player.active))
                    LostColosseum.HasBereftVassalAppeared = false;

                Main.spawnTileX = LostColosseum.CampfirePosition.X;
                Main.spawnTileY = LostColosseum.CampfirePosition.Y;
            }

            LostColosseumTeleportInterpolant = 0f;
        }
    }
}
