using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.AttackerGuardianBehaviorOverride;

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

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Shield");
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.width = 42;
            NPC.height = 102;
            NPC.lifeMax = 20000;
            NPC.knockBackResist = 0;
            NPC.defense = 50;
            NPC.DR_NERD(0.2f);
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

            NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.05f, 0f, 1f);

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
        }

        public void DoBehavior_SitStill(Player target)
        {
            // If the target is close enough, take damage.
            if (target.WithinRange(NPC.Center * NPC.Opacity, 800))
                NPC.dontTakeDamage = false;
        }

        public void DoBehavior_Shatter(Player target)
        {
            float attackLength = 180;
            float offsetAmount = MathHelper.Lerp(0, 15, ShatteringTimer / attackLength * NPC.Opacity);
            NPC.Center = InitialPosition + Main.rand.NextVector2Circular(offsetAmount, offsetAmount);
            NPC.netUpdate = true;

            if (ShatteringTimer == 0)
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceDoorShatterSound with { Volume = 3.0f }, target.Center * NPC.Opacity);

            if (ShatteringTimer >= attackLength)
            {
                // Die
                NPC.active = false;
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
                    return;

                NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
                // Tell the commander to swap attacks. The other guardians check for this automatically.
                commander.ai[0] = (float)GuardianComboAttackManager.GuardiansAttackType.SoloHealer;
                // Reset the first 5 extra ai slots. These are used for per attack information.
                for (int i = 0; i < 5; i++)
                    commander.Infernum().ExtraAI[i] = 0f;

                // Reset the attack timer.
                commander.ai[1] = 0f;

                // Despawn all the fire walls.
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<HolyFireWall>());
            }
            ShatteringTimer++;
        }


        public override void DrawBehind(int index) => Main.instance.DrawCacheNPCProjectiles.Add(index);

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D mainTexture = TextureAssets.Npc[Type].Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            Vector2 origin = mainTexture.Size() * 0.5f;

            DrawWall(spriteBatch, drawPosition);
            DrawBackglow(spriteBatch, mainTexture, drawPosition, NPC.frame);
            spriteBatch.Draw(mainTexture, drawPosition, null, Color.White * NPC.Opacity, NPC.rotation * NPC.Opacity, origin, NPC.scale * NPC.Opacity, SpriteEffects.None, 0f);
            return false;
        }

        public void DrawWall(SpriteBatch spriteBatch, Vector2 centerPosition)
        {
            // Draw variables.
            Texture2D wallTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/ProfanedGuardians/HealerShieldWall").Value;
            Vector2 drawPosition = centerPosition + new Vector2(21, 0);
            Rectangle frame = new(0, 0, wallTexture.Width, wallTexture.Height);

            // Draw the initial wall.
            DrawBackglow(spriteBatch, wallTexture, drawPosition, frame);
            spriteBatch.Draw(wallTexture, drawPosition, frame, Color.White * NPC.Opacity, 0f, wallTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // More variables
            spriteBatch.EnterShaderRegion();
            float lifeRatio = (float)NPC.life / NPC.lifeMax;
            float opacity = MathHelper.Lerp(0f, 0.125f, lifeRatio);
            float interpolant = ShatteringTimer / 120f;
            float shaderWallOpacity = MathHelper.Lerp(0.02f, 0f, interpolant);

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
            DrawData wall = new(wallTexture, drawPosition, frame, Color.White * opacity * 0.5f * NPC.Opacity, 0f, wallTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(wall);
            wall.Draw(spriteBatch);

            spriteBatch.ExitShaderRegion();
        }

        public void DrawBackglow(SpriteBatch spriteBatch, Texture2D npcTexture, Vector2 drawPosition, Rectangle frame)
        {
            float backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = MagicCrystalShot.ColorSet[0];
                backglowColor.A = 0;
                spriteBatch.Draw(npcTexture, drawPosition + backglowOffset, frame, backglowColor * NPC.Opacity, NPC.rotation, frame.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
        }

        public override bool CheckActive() => false;

        public override bool CheckDead()
        {
            CurrentState = (int)CrystalState.Shattering;
            NPC.life = NPC.lifeMax;
            NPC.netUpdate = true;

            return false;
        }
    }
}