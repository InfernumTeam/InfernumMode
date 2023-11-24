using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HealerShieldCrystal : ModNPC
    {
        public enum CrystalState
        {
            Alive,
            Shattering
        }

        public ref float CurrentState => ref NPC.ai[0];

        public ref float ShatteringTimer => ref NPC.ai[1];

        public Vector2 InitialPosition;

        public static Texture2D WallTexture => ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/HealerShieldWall").Value;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Holy Shield");
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.width = 42;
            NPC.height = 102;
            NPC.lifeMax = 25000;
            NPC.knockBackResist = 0;
            NPC.defense = 50;
            NPC.DR_NERD(0.15f);
            NPC.canGhostHeal = false;
            NPC.noGravity = true;
            NPC.HitSound = SoundID.NPCHit52;
            NPC.DeathSound = SoundID.NPCDeath55;
            NPC.Opacity = 0;
        }

        public override void OnSpawn(IEntitySource source) => InitialPosition = NPC.Center;

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            // Declare this as the active crystal.
            GlobalNPCOverrides.ProfanedCrystal = NPC.whoAmI;

            // Do not take damage by default.
            NPC.dontTakeDamage = true;

            NPC.Opacity = Clamp(NPC.Opacity + 0.01f, 0f, 1f);

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];

            // Get the commanders target.
            Player target = Main.player[commander.target];

            switch ((CrystalState)CurrentState)
            {
                case CrystalState.Alive:
                    DoBehavior_SitStill(target);
                    break;
                case CrystalState.Shattering:
                    DoBehavior_Shatter(target);
                    break;
            }

            // Force anyone close to it to be to the left.
            foreach (Player player in Main.player)
                if (player.active && !player.dead && player.Center.WithinRange(NPC.Center, 6000f))
                    if (player.Center.X > NPC.Center.X)
                        player.Center = new(NPC.Center.X, player.Center.Y);

            ShatteringTimer++;
        }

        public void DoBehavior_SitStill(Player target)
        {
            // If the target is close enough, take damage.
            if (target.WithinRange(NPC.Center * NPC.Opacity, 1000))
                NPC.dontTakeDamage = false;

            float sparkleRate = 12f;

            // Spawn sparkles.
            if (ShatteringTimer % sparkleRate == 0)
            {
                Vector2 position = NPC.Center + Main.rand.NextVector2Circular(NPC.width * 1.3f, NPC.height);
                Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f)) * Main.rand.NextFloat(0.5f, 1f);
                Color color = Color.Lerp(Color.HotPink, Color.LightPink, Main.rand.NextFloat());
                Vector2 scale = new(0.5f + Main.rand.NextFloat());
                Particle sparkle;
                if (Main.rand.NextBool())
                    sparkle = new GenericSparkle(position, velocity, color, Color.White, Main.rand.NextFloat(0.5f, 0.75f), 75, Main.rand.NextFloat(0.05f), 2f);
                else
                    sparkle = new FlareShine(position, velocity, color, Color.White, -PiOver2, scale, scale * 1.5f, 60, Main.rand.NextFloat(0.05f), 2f);
                GeneralParticleHandler.SpawnParticle(sparkle);
            }
        }

        public void DoBehavior_Shatter(Player target)
        {
            float attackLength = 180;
            float offsetAmount = Lerp(0, 15, ShatteringTimer / attackLength * NPC.Opacity);
            NPC.Center = InitialPosition + Main.rand.NextVector2Circular(offsetAmount, offsetAmount);
            NPC.netUpdate = true;

            if (ShatteringTimer == 0)
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceDoorShatterSound with { Volume = 3.0f }, target.Center * NPC.Opacity);

            if (ShatteringTimer >= attackLength)
            {
                // Die
                NPC.active = false;
                NPC.netUpdate = true;
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.ProfanedFlappyBirdEndTip");

                Vector2 wallBottom = NPC.Center + new Vector2(21f, 989f);
                Vector2 wallTop = NPC.Center + new Vector2(21f, -989f);

                float crystalAmount = 50f;
                for (int i = 0; i < crystalAmount; i++)
                {
                    Vector2 crystalSpawnPosition = Vector2.Lerp(wallBottom, wallTop, (float)i / crystalAmount) + Main.rand.NextVector2Circular(24f, 24f);
                    Vector2 crystalVelocity = -Vector2.UnitX.RotatedByRandom(1.06f) * Main.rand.NextFloat(2f, 4f);

                    if (!Collision.SolidCollision(crystalSpawnPosition, 1, 1) && Main.netMode != NetmodeID.Server)
                        Gore.NewGore(new EntitySource_WorldEvent(), crystalSpawnPosition, crystalVelocity, Mod.Find<ModGore>($"ProvidenceDoor{Main.rand.Next(1, 3)}").Type, 1.16f);
                }

                // Despawn all the fire walls.
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<HolyFireWall>());

                if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss))
                    return;

                NPC guard = Main.npc[CalamityGlobalNPC.doughnutBoss];
                // Tell the commander to swap attacks. The other guardians use this.
                GuardianComboAttackManager.SelectNewAttack(guard, ref guard.ai[1], (float)GuardianComboAttackManager.GuardiansAttackType.SoloHealer);
            }
        }


        public override void DrawBehind(int index) => Main.instance.DrawCacheNPCProjectiles.Add(index);

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D mainTexture = TextureAssets.Npc[Type].Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            Vector2 origin = mainTexture.Size() * 0.5f;

            DrawWall(spriteBatch, drawPosition);
            DrawMovingBackglow(spriteBatch, mainTexture, drawPosition, NPC.frame);
            DrawBackglow(spriteBatch, mainTexture, drawPosition, NPC.frame);
            spriteBatch.Draw(mainTexture, drawPosition, null, Color.White * NPC.Opacity, NPC.rotation, origin, NPC.scale, SpriteEffects.None, 0f);
            return false;
        }

        public void DrawWall(SpriteBatch spriteBatch, Vector2 centerPosition)
        {
            // Draw variables.
            Vector2 drawPosition = centerPosition + new Vector2(21, 0);
            Rectangle frame = new(0, 0, WallTexture.Width, WallTexture.Height);

            // Draw the initial wall.
            DrawBackglow(spriteBatch, WallTexture, drawPosition, frame);
            spriteBatch.Draw(WallTexture, drawPosition, frame, Color.White * NPC.Opacity, 0f, WallTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // More variables
            spriteBatch.EnterShaderRegion();
            float lifeRatio = (float)NPC.life / NPC.lifeMax;
            float opacity = Lerp(0f, 0.125f, lifeRatio);
            float interpolant = CurrentState == (float)CrystalState.Shattering ? ShatteringTimer / 120f : 0f;
            float shaderWallOpacity = Lerp(0.02f, 0f, interpolant);

            // Initialize the shader.
            Asset<Texture2D> shaderLayer = InfernumTextureRegistry.HolyCrystalLayer;
            InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(shaderLayer);
            InfernumEffectsRegistry.RealityTear2Shader.Shader.Parameters["fadeOut"].SetValue(true);

            // Draw a large overlay behin the wall, as if they are being protected by it.
            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            Vector2 scale = new Vector2(53.6f, 2010f) / TextureAssets.MagicPixel.Value.Size();
            Rectangle overlayFrame = new(0, 0, (int)(magicPixel.Width * scale.X), (int)(magicPixel.Height * scale.Y));
            DrawData overlay = new(magicPixel, centerPosition + new Vector2(1290, 0), overlayFrame, Color.White * shaderWallOpacity * NPC.Opacity, 0f, overlayFrame.Size() * 0.5f, scale, SpriteEffects.None, 0);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(overlay);
            overlay.Draw(spriteBatch);

            // Draw the wall overlay.
            DrawData wall = new(WallTexture, drawPosition, frame, Color.White * opacity * 0.5f * NPC.Opacity, 0f, WallTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(wall);
            wall.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
        }

        public void DrawBackglow(SpriteBatch spriteBatch, Texture2D npcTexture, Vector2 drawPosition, Rectangle frame)
        {
            float backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount).ToRotationVector2() * 2f;
                Color backglowColor = MagicSpiralCrystalShot.ColorSet[0];
                backglowColor.A = 0;
                spriteBatch.Draw(npcTexture, drawPosition + backglowOffset, frame, backglowColor * NPC.Opacity, NPC.rotation, frame.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
        }

        public void DrawMovingBackglow(SpriteBatch spriteBatch, Texture2D npcTexture, Vector2 drawPosition, Rectangle frame)
        {
            float backglowAmount = 5;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / backglowAmount + Main.GlobalTimeWrappedHourly * 2f).ToRotationVector2() * 7f;
                Color backglowColor = Color.Lerp(MagicSpiralCrystalShot.ColorSet[0], MagicSpiralCrystalShot.ColorSet[1], 0.5f);
                backglowColor.A = 0;
                spriteBatch.Draw(npcTexture, drawPosition + backglowOffset, frame, backglowColor * NPC.Opacity, NPC.rotation, frame.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
        }

        public override bool CheckActive() => false;

        public override bool CheckDead()
        {
            CurrentState = (int)CrystalState.Shattering;
            ShatteringTimer = 0;
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;

            return false;
        }
    }
}
