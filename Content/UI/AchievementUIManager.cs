using InfernumMode.Content.Achievements;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace InfernumMode.Content.UI
{
    public abstract class AchievementUIState : UIState
    {
        internal abstract UIScrollbar Scrollbar { get; set; }
    }

    // I'd much rather use custom ui than this vanilla stuff but it would look different so :D
    public class AchievementUIManager : AchievementUIState
    {
        private UIList AchivementList;

        private readonly List<InfernumUIAchievementListItem> AchievementElements = new();

        private UIElement OuterContainer;

        internal override UIScrollbar Scrollbar { get; set; }

        public void InitializePage()
        {
            // Clear all of the saved fields.
            RemoveAllChildren();
            AchievementElements.Clear();
            AchivementList = null;
            OuterContainer = null;

            // Create a new UI Element.
            // The following is all from the vanilla achievement UIState, to
            // ensure this looks identical to that.
            UIElement uIElement = new();
            uIElement.Width.Set(0f, 0.8f);
            uIElement.MaxWidth.Set(900f, 0f);
            uIElement.MinWidth.Set(700f, 0f);
            uIElement.Top.Set(220f, 0f);
            uIElement.Height.Set(-220f, 1f);
            uIElement.HAlign = 0.5f;
            OuterContainer = uIElement;
            Append(uIElement);

            UIPanel uIPanel = new();
            uIPanel.Width.Set(0f, 1f);
            uIPanel.Height.Set(-110f, 1f);
            uIPanel.BackgroundColor = new Color(79, 33, 33) * 0.8f;
            uIPanel.PaddingTop = 0f;
            uIElement.Append(uIPanel);

            AchivementList = new UIList();
            AchivementList.Width.Set(-25f, 1f);
            AchivementList.Height.Set(-50f, 1f);
            AchivementList.Top.Set(50f, 0f);
            AchivementList.ListPadding = 5f;
            uIPanel.Append(AchivementList);

            UITextPanel<LocalizedText> uITextPanel = new(Language.GetText("Death Wishes"), 1f, large: true)
            {
                HAlign = 0.5f
            };
            uITextPanel.Top.Set(-33f, 0f);
            uITextPanel.SetPadding(13f);
            uITextPanel.BackgroundColor = new Color(174, 71, 71);
            uIElement.Append(uITextPanel);

            UITextPanel<LocalizedText> uITextPanel2 = new(Language.GetText("UI.Back"), 0.7f, large: true);
            uITextPanel2.Width.Set(-10f, 0.5f);
            uITextPanel2.Height.Set(50f, 0f);
            uITextPanel2.VAlign = 1f;
            uITextPanel2.HAlign = 0.5f;
            uITextPanel2.Top.Set(-45f, 0f);
            uITextPanel2.OnMouseOver += FadedMouseOver;
            uITextPanel2.OnMouseOut += FadedMouseOut;
            uITextPanel2.OnLeftClick += GoBackClick;
            uITextPanel2.BackgroundColor = new Color(174, 71, 71);
            uIElement.Append(uITextPanel2);

            // Get a copy of the ordered achievement list.
            List<Achievement> list = AchievementPlayer.GetAchievementsList();
            for (int i = 0; i < list.Count; i++)
            {
                // Do not count dev wishes.
                if (list[i].IsDevWish)
                    continue;
                // Create a list item for each achievement, and add it to the local achievement list.
                InfernumUIAchievementListItem item = new(list[i], true);
                AchivementList.Add(item);
                AchievementElements.Add(item);
            }

            UITextPanel<string> next = new(">", 0.7f, true);
            next.Width.Set(-10f, 0.1f);
            next.Height.Set(50f, 0f);
            next.VAlign = 1f;
            next.HAlign = 0.85f;
            next.Top.Set(-45f, 0f);
            next.OnMouseOver += FadedMouseOver;
            next.OnMouseOut += FadedMouseOut;
            next.OnLeftClick += AchivementClick;
            next.BackgroundColor = new Color(174, 71, 71);
            uIElement.Append(next);

            // Add a scroll bar. This is a custom one that derives from the base UIScrollbar, but with the ability
            // to have a custom background texture (WHY IS IT HARDCODED).
            AchievementUIScrollbar uIScrollbar = new();
            uIScrollbar.SetView(100f, 1000f);
            uIScrollbar.Height.Set(-50f, 1f);
            uIScrollbar.Top.Set(50f, 0f);
            uIScrollbar.HAlign = 1f;
            uIPanel.Append(uIScrollbar);

            AchivementList.SetScrollbar(uIScrollbar);
            Scrollbar = uIScrollbar;
        }

        private void AchivementClick(UIMouseEvent evt, UIElement listeningElement)
        {
            IngameFancyUI.OpenUIState(UIRenderingSystem.wishesUIManager);
            UIRenderingSystem.CurrentAchievementUI = UIRenderingSystem.wishesUIManager;
        }

        // When the Back button is clicked.
        private void GoBackClick(UIMouseEvent evt, UIElement listeningElement)
        {
            Main.menuMode = 0;
            IngameFancyUI.Close();
        }

        // When the mouse begins to hover.
        private void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            ((UIPanel)evt.Target).BackgroundColor = new Color(171, 73, 73);
            ((UIPanel)evt.Target).BorderColor = Colors.FancyUIFatButtonMouseOver;
        }

        // When the mouse stops hovering.
        private void FadedMouseOut(UIMouseEvent evt, UIElement listeningElement)
        {
            ((UIPanel)evt.Target).BackgroundColor = new Color(151, 63, 63) * 0.8f;
            ((UIPanel)evt.Target).BorderColor = Color.Black;
        }

        // When this UIState is activated.
        public override void OnActivate()
        {
            // Initialize the page.
            InitializePage();

            // Vanilla stuff.
            if (Main.gameMenu)
            {
                OuterContainer.Top.Set(220f, 0f);
                OuterContainer.Height.Set(-220f, 1f);
            }
            else
            {
                OuterContainer.Top.Set(120f, 0f);
                OuterContainer.Height.Set(-120f, 1f);
            }

            AchivementList.UpdateOrder();
            if (PlayerInput.UsingGamepadUI)
                UILinkPointNavigator.ChangePoint(3002);
        }
    }

    public class WishesUIManager : AchievementUIState
    {
        private UIList AchivementList;

        private readonly List<InfernumUIAchievementListItem> AchievementElements = new();

        private UIElement OuterContainer;

        internal override UIScrollbar Scrollbar { get; set; }

        public void InitializePage()
        {
            // Clear all of the saved fields.
            RemoveAllChildren();
            AchievementElements.Clear();
            AchivementList = null;
            OuterContainer = null;

            // Create a new UI Element.
            // The following is all from the vanilla achievement UIState, to
            // ensure this looks identical to that.
            UIElement uIElement = new();
            uIElement.Width.Set(0f, 0.8f);
            uIElement.MaxWidth.Set(900f, 0f);
            uIElement.MinWidth.Set(700f, 0f);
            uIElement.Top.Set(220f, 0f);
            uIElement.Height.Set(-220f, 1f);
            uIElement.HAlign = 0.5f;
            OuterContainer = uIElement;
            Append(uIElement);

            UIPanel uIPanel = new();
            uIPanel.Width.Set(0f, 1f);
            uIPanel.Height.Set(-110f, 1f);
            uIPanel.BackgroundColor = new Color(79, 33, 33) * 0.8f;
            uIPanel.PaddingTop = 0f;
            uIElement.Append(uIPanel);

            AchivementList = new UIList();
            AchivementList.Width.Set(-25f, 1f);
            AchivementList.Height.Set(-50f, 1f);
            AchivementList.Top.Set(50f, 0f);
            AchivementList.ListPadding = 5f;
            uIPanel.Append(AchivementList);

            UITextPanel<LocalizedText> uITextPanel = new(Language.GetText("Dev Wishes"), 1f, large: true)
            {
                HAlign = 0.5f
            };
            uITextPanel.Top.Set(-33f, 0f);
            uITextPanel.SetPadding(13f);
            uITextPanel.BackgroundColor = new Color(174, 71, 71);
            uIElement.Append(uITextPanel);

            UITextPanel<LocalizedText> uITextPanel2 = new(Language.GetText("UI.Back"), 0.7f, large: true);
            uITextPanel2.Width.Set(-10f, 0.5f);
            uITextPanel2.Height.Set(50f, 0f);
            uITextPanel2.VAlign = 1f;
            uITextPanel2.HAlign = 0.5f;
            uITextPanel2.Top.Set(-45f, 0f);
            uITextPanel2.OnMouseOver += FadedMouseOver;
            uITextPanel2.OnMouseOut += FadedMouseOut;
            uITextPanel2.OnLeftClick += GoBackClick;
            uITextPanel2.BackgroundColor = new Color(174, 71, 71);
            uIElement.Append(uITextPanel2);

            // Get a copy of the ordered achievement list.
            List<Achievement> list = AchievementPlayer.GetAchievementsList();
            for (int i = 0; i < list.Count; i++)
            {
                // Do not count non dev wishes.
                if (!list[i].IsDevWish)
                    continue;
                // Create a list item for each achievement, and add it to the local achievement list.
                InfernumUIAchievementListItem item = new(list[i], true);
                AchivementList.Add(item);
                AchievementElements.Add(item);
            }

            UITextPanel<string> next = new("<", 0.7f, true);
            next.Width.Set(-10f, 0.1f);
            next.Height.Set(50f, 0f);
            next.VAlign = 1f;
            next.HAlign = 0.15f;
            next.Top.Set(-45f, 0f);
            next.OnMouseOver += FadedMouseOver;
            next.OnMouseOut += FadedMouseOut;
            next.OnLeftClick += WishesClick;
            next.BackgroundColor = new Color(174, 71, 71);
            uIElement.Append(next);

            // Add a scroll bar. This is a custom one that derives from the base UIScrollbar, but with the ability
            // to have a custom background texture (WHY IS IT HARDCODED).
            AchievementUIScrollbar uIScrollbar = new();
            uIScrollbar.SetView(100f, 1000f);
            uIScrollbar.Height.Set(-50f, 1f);
            uIScrollbar.Top.Set(50f, 0f);
            uIScrollbar.HAlign = 1f;
            uIPanel.Append(uIScrollbar);

            AchivementList.SetScrollbar(uIScrollbar);
            Scrollbar = uIScrollbar;
        }

        private void WishesClick(UIMouseEvent evt, UIElement listeningElement)
        {
            IngameFancyUI.OpenUIState(UIRenderingSystem.achievementUIManager);
            UIRenderingSystem.CurrentAchievementUI = UIRenderingSystem.achievementUIManager;
        }

        // When the Back button is clicked.
        internal void GoBackClick(UIMouseEvent evt, UIElement listeningElement)
        {
            Main.menuMode = 0;
            IngameFancyUI.Close();
        }

        // When the mouse begins to hover.
        private void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            ((UIPanel)evt.Target).BackgroundColor = new Color(171, 73, 73);
            ((UIPanel)evt.Target).BorderColor = Colors.FancyUIFatButtonMouseOver;
        }

        // When the mouse stops hovering.
        private void FadedMouseOut(UIMouseEvent evt, UIElement listeningElement)
        {
            ((UIPanel)evt.Target).BackgroundColor = new Color(151, 63, 63) * 0.8f;
            ((UIPanel)evt.Target).BorderColor = Color.Black;
        }

        public override void OnActivate()
        {
            // Initialize the page.
            InitializePage();

            // Vanilla stuff.
            if (Main.gameMenu)
            {
                OuterContainer.Top.Set(220f, 0f);
                OuterContainer.Height.Set(-220f, 1f);
            }
            else
            {
                OuterContainer.Top.Set(120f, 0f);
                OuterContainer.Height.Set(-120f, 1f);
            }

            AchivementList.UpdateOrder();
            if (PlayerInput.UsingGamepadUI)
                UILinkPointNavigator.ChangePoint(3002);
        }
    }
}
