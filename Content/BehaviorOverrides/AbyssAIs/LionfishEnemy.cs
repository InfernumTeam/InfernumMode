using CalamityMod;
using CalamityMod.BiomeManagers;
using CalamityMod.Items.Weapons.Rogue;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class LionfishEnemy : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lionfish");
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.UsesNewTargetting[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.damage = 50;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 42;
            NPC.defense = 10;
            NPC.lifeMax = 300;
            NPC.aiStyle = AIType = -1;
            NPC.knockBackResist = 0.1f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(0, 0, 5, 0);
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            SpawnModBiomes = new int[] { ModContent.GetInstance<AbyssLayer1Biome>().Type, ModContent.GetInstance<AbyssLayer2Biome>().Type };
            NPC.Infernum().IsAbyssPredator = true;
            NPC.waterMovementSpeed = 0f;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.InfernumMode.Bestiary.Lionfish")
            });
        }

        public override void AI()
        {
            // Pick a target if a valid one isn't already decided.
            Utilities.TargetClosestAbyssPredator(NPC, false, 500f, 400f);
            NPCAimedTarget target = NPC.GetTargetData();

            // Emit light.
            Lighting.AddLight(NPC.Center, Color.Purple.ToVector3());

            // Swim around slowly if no target was found.
            NPC.spriteDirection = (NPC.velocity.X < 0f).ToDirectionInt();
            if (target.Invalid)
            {
                NPC.noTileCollide = false;
                if (NPC.velocity.Length() < 2.5f)
                    NPC.velocity.Y -= 0.45f;
                NPC.velocity = (NPC.velocity.RotatedBy(0.01f) * 1.01f).ClampMagnitude(1.6f, 5f);

                // Steer away if tiles are ahead.
                if (!Collision.CanHitLine(NPC.Center, 1, 1, NPC.Center + NPC.velocity.SafeNormalize(Vector2.Zero) * 300f, 1, 1))
                {
                    Vector2 leftVelocity = NPC.velocity.RotatedBy(-0.2f);
                    Vector2 rightVelocity = NPC.velocity.RotatedBy(0.2f);
                    float leftDistance = CalamityUtils.DistanceToTileCollisionHit(NPC.Center, leftVelocity) ?? 500f;
                    float rightDistance = CalamityUtils.DistanceToTileCollisionHit(NPC.Center, rightVelocity) ?? 500f;
                    NPC.velocity = (leftDistance > rightDistance ? leftVelocity : rightVelocity) * 0.96f;

                    if (NPC.collideX || NPC.collideY)
                        NPC.velocity *= -3f;
                }
                NPC.rotation = NPC.velocity.X * 0.1f;

                return;
            }

            NPC.noTileCollide = true;

            // Charge down the target.
            if (!NPC.WithinRange(target.Center, 120f))
                NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(target.Center) * 8.4f, 0.04f);
            else
            {
                if (NPC.velocity.Length() < 3f)
                    NPC.velocity.Y -= 0.4f;
                NPC.velocity *= 1.01f;
            }

            NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * 0.08f, 0.3f);
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            if (NPC.frameCounter >= 8D)
            {
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = 0;

                NPC.frameCounter = 0D;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            for (int i = 1; i <= 3; i++)
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>($"Lionfish{i}").Type, NPC.scale);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (hurtInfo.Damage <= 0)
                return;

            target.AddBuff(BuffID.Venom, 180, true);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            var calPlayer = spawnInfo.Player.Calamity();
            bool inFirst2Layers = calPlayer.ZoneAbyssLayer1 || calPlayer.ZoneAbyssLayer2;
            if (inFirst2Layers && spawnInfo.Water && WorldSaveSystem.InPostAEWUpdateWorld)
                return SpawnCondition.CaveJellyfish.Chance * 0.5f;
            return 0f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            var postSkeletron = npcLoot.DefineConditionalDropSet(DropHelper.PostSkele());
            postSkeletron.Add(ModContent.ItemType<Lionfish>(), 10);
        }
    }
}
