using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace NoxusBoss.Content.UI
{
    public class XerocCultistDialogUI : UIState
    {
        public string CurrentlySpokenDialog
        {
            get;
            set;
        }

        public Dialog FullDialog
        {
            get;
            set;
        }

        public UICustomBackground BackgroundUI
        {
            get;
            private set;
        }

        public UIText DialogResponseUI
        {
            get;
            private set;
        }

        public UIDialogOptions DialogInquiryUI
        {
            get;
            private set;
        }

        public string FullDialogWrapped
        {
            get
            {
                if (!XerocCultistDialogRegistry.HasTalkedToCultist())
                    return "...";

                return string.Join('\n', WordwrapString(FullDialog.Response, DialogInquiryUI.font, (int)((BackgroundUI.Width.Pixels - 60f) / DialogScale / BackgroundScale * 0.98f), 50, out _)).TrimEnd('\n'); ;
            }
        }

        public static float BackgroundScale => 1.4f;

        public static float DialogScale => 0.75f;

        public override void OnInitialize()
        {
            Asset<Texture2D> backgroundTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/UI/CultistDialogBackground", AssetRequestMode.ImmediateLoad);

            // Create the background element.
            BackgroundUI = new(backgroundTexture, Color.White);

            BackgroundUI.Width.Set(BackgroundScale * 644f, 0f);
            BackgroundUI.Height.Set(BackgroundScale * 132f, 0f);

            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 backgroundPosition = screenArea * 0.5f - Vector2.UnitY * 300f - backgroundTexture.Value.Size();
            BackgroundUI.Top.Set(backgroundPosition.Y, 0f);
            BackgroundUI.Left.Set(backgroundPosition.X, 0f);
            BackgroundUI.OnClick += SpeedUpDialog;
            Append(BackgroundUI);

            // Create the dialog inquiry element.
            float inquiryScale = 0.8f;
            DialogInquiryUI = new(XerocCultistDialogRegistry.FirstEncounterDialog, FontAssets.MouseText.Value, new(45, 12, 31), new(79, 24, 68), inquiryScale, () =>
            {
                return CurrentlySpokenDialog.Length >= FullDialogWrapped.Length;
            });
            DialogInquiryUI.Width.Set(284f, 0f);
            DialogInquiryUI.Height.Set(36f, 0f);

            DialogInquiryUI.Top.Set(116f, 0f);
            DialogInquiryUI.Left.Set(210f, 0f);
            DialogInquiryUI.OnClick += SelectDialog;
            BackgroundUI.Append(DialogInquiryUI);

            // Create the dialog response element.
            DialogResponseUI = new(string.Empty, DialogScale);
            DialogResponseUI.Top.Set(30f, 0f);
            DialogResponseUI.Left.Set(30f, 0f);
            CurrentlySpokenDialog = string.Empty;
            BackgroundUI.Append(DialogResponseUI);

            FullDialog ??= XerocCultistDialogRegistry.InitialQuestion;
        }

        private void SelectDialog(UIMouseEvent evt, UIElement listeningElement)
        {
            var ui = (UIDialogOptions)listeningElement;
            var validDialog = ui.ValidDialog;
            var textBoxes = ui.TextBoxes;
            CurrentlySpokenDialog = string.Empty;

            // Store the selected full dialog.
            for (int i = 0; i < validDialog.Count; i++)
            {
                if (textBoxes[i].Intersects(MouseScreenRectangle))
                {
                    FullDialog = validDialog[i];
                    Main.LocalPlayer.GetModPlayer<DialogPlayer>().HasTalkedToCultist = true;
                    break;
                }
            }
        }

        private void SpeedUpDialog(UIMouseEvent evt, UIElement listeningElement)
        {
            if (CurrentlySpokenDialog.Length >= 10 && CurrentlySpokenDialog != FullDialogWrapped)
                CurrentlySpokenDialog = FullDialogWrapped;
        }

        public override void Update(GameTime gameTime)
        {
            // Manually click elements from the dialog.
            if (DialogInquiryUI.TextBoxes.Any(b => b.Intersects(MouseScreenRectangle)) && !DialogInquiryUI.IsMouseHovering)
            {
                if (Main.mouseLeft)
                    DialogInquiryUI.Click(new(DialogInquiryUI, Main.MouseScreen));
            }

            // Keep the dialog UI updated.
            UpdateSpokenDialog();
            if (DialogResponseUI.Text != CurrentlySpokenDialog)
            {
                DialogResponseUI.SetText(CurrentlySpokenDialog);

                // Register the dialog as read once it has been fully completed.
                if (CurrentlySpokenDialog.Length >= FullDialogWrapped.Length && XerocCultistDialogRegistry.HasTalkedToCultist())
                    XerocCultistDialogRegistry.RegisterAsSeenDialog(FullDialog);
            }
        }

        public void UpdateSpokenDialog()
        {
            // Add the next character to the spoken dialog.
            string wrappedDialog = FullDialogWrapped;
            if (CurrentlySpokenDialog != wrappedDialog && CurrentlySpokenDialog.Length < wrappedDialog.Length)
                CurrentlySpokenDialog += wrappedDialog[CurrentlySpokenDialog.Length];
        }
    }
}
