using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFreeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemHeadFree;

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].type != NPCID.Golem)
            {
                GolemBodyBehaviorOverride.DespawnNPC(npc.whoAmI);
                return false;
            }

            npc.lifeMax = Main.npc[(int)npc.ai[0]].lifeMax;

            npc.chaseable = !npc.dontTakeDamage;
            npc.Opacity = npc.dontTakeDamage ? 0f : 1f;
            return false;
        }

        public override void SendExtraData(NPC npc, ModPacket writer)
        {
            writer.Write(npc.Opacity);
            writer.Write(npc.dontTakeDamage);
        }

        public override void ReceiveExtraData(NPC npc, BinaryReader reader)
        {
            npc.Opacity = reader.ReadSingle();
            npc.dontTakeDamage = reader.ReadBoolean();
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (npc.dontTakeDamage)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Golem/FreeHead").Value;
            Texture2D glowMask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Golem/FreeHeadGlow").Value;
            Rectangle rect = new(0, 0, texture.Width, texture.Height);
            if (InfernumMode.EmodeIsActive)
            {
                texture = TextureAssets.Npc[npc.type].Value;
                glowMask = InfernumTextureRegistry.Invisible.Value;
                rect = npc.frame;
            }

            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, rect, lightColor * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(glowMask, npc.Center - Main.screenPosition, rect, Color.White * npc.Opacity, npc.rotation, rect.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            GolemHeadBehaviorOverride.DoEyeDrawing(npc);
            return false;
        }
    }
}
