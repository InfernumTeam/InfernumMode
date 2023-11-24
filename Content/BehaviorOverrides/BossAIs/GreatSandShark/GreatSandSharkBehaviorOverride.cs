using CalamityMod;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using GreatSandSharkNPC = CalamityMod.NPCs.GreatSandShark.GreatSandShark;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark
{
    public class GreatSandSharkBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<GreatSandSharkNPC>();

        public static LocalizedText NewName => Utilities.GetLocalization("NameOverrides.GreatSandShark.EntryName");

        public override void Load()
        {
            GlobalNPCOverrides.HitEffectsEvent += UseCustomHitSound;
        }

        private void UseCustomHitSound(NPC npc, ref NPC.HitInfo hit)
        {
            // Play GSS' custom hit sound.
            if (npc.type == ModContent.NPCType<GreatSandSharkNPC>() && npc.soundDelay <= 0)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkHitSound with { Volume = 2f }, npc.Center);
                npc.soundDelay = 11;
            }
        }

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 300;
            npc.height = 120;
            npc.scale = 1f;
            npc.Opacity = 1f;
            npc.defense = 40;
            npc.DR_NERD(0.25f);
        }

        public override bool PreAI(NPC npc)
        {
            // Disappear if the bereft vassal is not present.
            int vassal = NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>());
            if (vassal == -1)
            {
                npc.active = false;
                return false;
            }

            // Do not despawn.
            npc.timeLeft = 7200;

            // Stay inside of the world.
            npc.Center = Vector2.Clamp(npc.Center, Vector2.One * 150f, Vector2.One * new Vector2(Main.maxTilesX * 16f - 150f, Main.maxTilesY * 16f - 150f));

            // Fix vanilla FindFrame jank.
            npc.localAI[3] = 1f;

            // Stop being so FAT you SILLY shark!!!
            npc.height = 104;
            npc.width = 280;

            // Get rid of the traditional hit sound.
            npc.HitSound = null;

            // Reset damage and other things.
            npc.defDamage = 230;
            npc.damage = npc.defDamage;

            // Inherit attributes from the bereft vassal.
            BereftVassalComboAttackManager.InheritAttributesFromLeader(npc);

            // Inehrit music from the bereft vassal.
            npc.boss = true;
            npc.ModNPC.Music = Main.npc[vassal].ModNPC.Music;
            npc.ModNPC.SceneEffectPriority = (SceneEffectPriority)12;

            Player target = Main.player[npc.target];
            BereftVassalComboAttackManager.DoComboAttacksIfNecessary(npc, target, ref BereftVassalComboAttackManager.Vassal.ai[1]);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            return false;
        }
    }
}
