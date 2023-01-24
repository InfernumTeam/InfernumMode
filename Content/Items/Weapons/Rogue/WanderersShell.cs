using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.Rogue;
using InfernumMode.Content.Projectiles.Rogue;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Rogue
{
    public class WanderersShell : RogueWeapon
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wanderer's Shell");
            Tooltip.SetDefault("Throws a spread of sea shells that home in on enemies before bouncing off of them and lingering on the ground\n" +
                "Stealth strikes cause all lingering shells to explode into water geysers that target nearby enemies\n" +
                "The open edge is too sharp to put near your ear");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 36;
            Item.damage = 103;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = 21;
            Item.useTime = 21;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 1f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<WanderersShellProj>();
            Item.shootSpeed = 16f;
            Item.DamageType = RogueDamageClass.Instance;

            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ModContent.RarityType<InfernumVassalRarity>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Stealth strikes cause all existing shells to break open and release water beams at nearby targets.
            if (player.Calamity().StealthStrikeAvailable())
            {
                int waterID = ModContent.ProjectileType<WanderersShellWater>();
                foreach (Projectile shell in Utilities.AllProjectilesByID(type).Where(p => p.owner == player.whoAmI))
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 sandVelocity = new(Main.rand.NextFloat(-7f, 7f), -Main.rand.NextFloat(1.5f, 4f));
                        Dust crust = Dust.NewDustPerfect(shell.Center, 32, sandVelocity, Scale: Main.rand.NextFloat(0.65f, 1f));
                        crust.noGravity = true;
                    }

                    NPC potentialTarget = shell.Center.ClosestNPCAt(800f);
                    if (potentialTarget is not null)
                        Projectile.NewProjectile(source, shell.Center, shell.SafeDirectionTo(potentialTarget.Center), waterID, damage / 4, 0f, player.whoAmI);

                    // Destroy the shell.
                    shell.Kill();
                }

                SoundEngine.PlaySound(SoundID.Item84, player.Center);
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 shellVelocity = velocity + Main.rand.NextVector2Circular(1.6f, 1.6f);
                Projectile.NewProjectile(source, position, shellVelocity, type, damage, knockback, player.whoAmI);
            }

            return false;
        }
    }
}
