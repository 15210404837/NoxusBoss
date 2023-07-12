using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Assets.Fonts;
using NoxusBoss.Core;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
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

        public UIFancyText DialogResponseUI
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
                if (!XerocCultistDialogRegistry.HasTalkedToCultist)
                    return "...";

                string[] wrappedLines = WordwrapString(FullDialog.Response, DialogInquiryUI.font, (int)((BackgroundUI.Width.Pixels - 60f) / DialogScale / BackgroundScale * 0.98f), 50, out _);
                return string.Join('\n', wrappedLines).TrimEnd('\n');
            }
        }

        public static float BackgroundScale => 1f;

        public static float DialogScale => 0.335f;

        public static readonly SoundStyle CultistSpeakSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Cultists/XerocCultistSpeak", 4) with { Volume = 0.8f, MaxInstances = 4 };

        public override void OnInitialize()
        {
            Asset<Texture2D> backgroundTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/UI/CultistDialogBackground", AssetRequestMode.ImmediateLoad);

            // Create the background element.
            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 backgroundPosition = screenArea * 0.5f - Vector2.UnitY * 300f - backgroundTexture.Value.Size();
            BackgroundUI = new(backgroundTexture, Color.White);
            BackgroundUI.Width.Set(BackgroundScale * 740f, 0f);
            BackgroundUI.Height.Set(BackgroundScale * 142f, 0f);
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
            DialogResponseUI = new(string.Empty, FontRegistry.Instance.CultistFont, FontRegistry.Instance.CultistFontItalics, Color.LightCoral, DialogScale);
            DialogResponseUI.Top.Set(22f, 0f);
            DialogResponseUI.Left.Set(30f, 0f);
            CurrentlySpokenDialog = string.Empty;
            BackgroundUI.Append(DialogResponseUI);

            // Default the dialog option to the first option that can be displayed.
            FullDialog = XerocCultistDialogRegistry.FirstEncounterDialog.FirstOrDefault(d => d.CanBeDisplayed) ?? XerocCultistDialogRegistry.InitialQuestion;
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
                if (CurrentlySpokenDialog.Length >= FullDialogWrapped.Length && XerocCultistDialogRegistry.HasTalkedToCultist)
                    XerocCultistDialogRegistry.RegisterAsSeenDialog(FullDialog);
            }
        }

        public void UpdateSpokenDialog()
        {
            // Add the next character to the spoken dialog.
            string wrappedDialog = FullDialogWrapped;
            if (CurrentlySpokenDialog != wrappedDialog && CurrentlySpokenDialog.Length < wrappedDialog.Length)
            {
                char nextCharacter = wrappedDialog[CurrentlySpokenDialog.Length];

                // Play speaking sounds if the next character is not silence.
                bool isEndOfSentence = nextCharacter == '.' || nextCharacter == '?' || nextCharacter == '!';
                if (!string.IsNullOrEmpty(nextCharacter.ToString()) && !isEndOfSentence && CurrentlySpokenDialog.Length % 8 == 2)
                    SoundEngine.PlaySound(CultistSpeakSound with { Pitch = -0.12f, Volume = 0.3f });

                CurrentlySpokenDialog += nextCharacter;

                if (nextCharacter == '[')
                {
                    while (nextCharacter != ']')
                    {
                        nextCharacter = wrappedDialog[CurrentlySpokenDialog.Length];
                        CurrentlySpokenDialog += nextCharacter;
                    }
                }
            }
        }
    }
}
