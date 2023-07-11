using NoxusBoss.Core;
using System.Linq;
using Terraria;

namespace NoxusBoss.Content.UI
{
    public static class XerocCultistDialogRegistry
    {
        // TODO -- Make italics and other text emphasis work. Also having some camera commands would be cool.
        public static readonly Dialog InitialQuestion = new("Who are you?", "I am Solyn, a member of the Order of The Eye. We have come seeking an opportunity to enlighten. Please, let us converse. There is much we may mutually impart.");

        public static readonly Dialog IntroductorySpiel = new("Uh... sure? What do you have to say?", "You see, unexpected encounters hold the greatest potential for understanding and growth. We traverse the lands, ever curious, " +
            "seeking to unravel that with remains unseen, and to find like minds in this grand pursuit. " +
            "Taking notice of your endeavors, we felt confident that you would agree.", () => SeenDialog(InitialQuestion));

        public static readonly Dialog EncountersDialog = new("Encounters? What kind of encounters?", "Ah, such mysteries are the fabric that enshroud our reality, don't you agree? We ponder the enigmatic forces that shape our world, " +
            "the interconnectedness of all things, and the whispers of hidden knowledge that elude most.", () => SeenDialog(IntroductorySpiel));

        public static readonly Dialog CuriosityDialog1 = new("I suppose there *are* some things that we don't fully understand.", "Perhaps, but there is always more to discover and to learn! The pursuit of knowledge is an unending " +
            "journey, for there are always realms just beyond our current comprehension.", () => SeenDialog(EncountersDialog));

        public static readonly Dialog CuriosityDialog2 = new("Sounds like you're quite the curious bunch.", "Curiosity is the seed from which wisdom blossoms. We are humbled by the vastness of the natural world, the cosmos beyond, " +
            "and the boundless depths of our souls. We encourage introspection and exploration, for only through such endeavors can we truly grow.", () => SeenDialog(EncountersDialog));

        public static readonly Dialog AbstractDialog = new("That all sounds rather abstract. Is there anything more... tangible you focus on?", "Ah, the tangible and the abstract intertwine in fascinating ways, don't they? While our pursuits " +
            "may seem lofty, we do not ignore the physical realm. We study ancient texts and decipher forgotten symbols. Yet... through it all, one name stands tall at the center of all of creation: Xeroc. Xeroc is our focus.",
            () => SeenDialog(CuriosityDialog1, CuriosityDialog2));

        public static readonly Dialog WorshipDialog = new("So, your Order worships this deity?", "Indeed, we hold Xeroc in the highest regard and strive to connect with its divine essence. " +
            "Through our rituals, meditations, and study, we seek to align ourselves with the radiant energy of Xeroc and attain enlightenment.",
            () => SeenDialog(AbstractDialog));

        public static readonly Dialog BeliefDialog1 = new("That's... quite a belief system you have.", "Our belief system is indeed profound, for we perceive the vastness of the universe and the intricate web of existence. " +
            "Our devotion to Xeroc is a guiding light in navigating the mysteries of existence and seeking transcendence beyond the mundane.",
            () => SeenDialog(WorshipDialog));

        public static readonly Dialog BeliefDialog2 = new("Do you ever doubt your beliefs?", "Doubt is a natural aspect of the human experience, and it is through questioning and introspection that our beliefs gain strength. " +
            "We embrace thoughtful skepticism, for it challenges us to seek deeper understanding and reaffirm our commitment to the path of enlightenment. I could not see myself abandoning that.",
            () => SeenDialog(WorshipDialog));

        public static readonly Dialog ConvertDialog = new("Are you looking to convert me?", "Conversion is not our primary intention. We are simply here to offer an opportunity for mutual growth and understanding. " +
            "If you find resonance with our teachings, you are welcome to explore further, but the choice is always yours to make.",
            () => SeenDialog(BeliefDialog1, BeliefDialog2));

        public static readonly Dialog PiquedCuriosityDialog = new("I must admit, your words have piqued my curiosity.", "I am delighted to hear that! Curiosity is the gateway to enlightenment! " +
            "If you wish, we can delve deeper into our philosophy or any specific topic that intrigues you at a later time. The path of illumination is open to all who are willing to embark upon it.",
            () => SeenDialog(ConvertDialog));

        public static readonly Dialog LeavingDialog = new("Later time? Are you leaving?", "I would linger longer, but there are pressing matters I must attend to. Worry not, for we shall meet again in the tapestry of existence. " +
            "Good luck, and may you always find truth in your explorations.",
            () => SeenDialog(PiquedCuriosityDialog));

        public static Dialog[] FirstEncounterDialog => new[]
        {
            InitialQuestion,
            IntroductorySpiel,
            EncountersDialog,

            CuriosityDialog1,
            CuriosityDialog2,

            AbstractDialog,
            WorshipDialog,

            BeliefDialog1,
            BeliefDialog2,

            ConvertDialog,
            PiquedCuriosityDialog,
            LeavingDialog,
        };

        public static void RegisterAsSeenDialog(Dialog dialog)
        {
            var player = Main.LocalPlayer.GetModPlayer<DialogPlayer>();
            if (!player.SeenCultistDialogIDs.Contains(dialog.ID))
                player.SeenCultistDialogIDs.Add(dialog.ID);
        }

        public static void ResetEverything()
        {
            Main.LocalPlayer.GetModPlayer<DialogPlayer>().HasTalkedToCultist = false;
            Main.LocalPlayer.GetModPlayer<DialogPlayer>().SeenCultistDialogIDs.Clear();
        }

        public static bool HasTalkedToCultist() => Main.LocalPlayer.GetModPlayer<DialogPlayer>().HasTalkedToCultist;

        // Simple shorthand method for use when setting up dialog conditions.
        public static bool SeenDialog(params Dialog[] dialog) => SeenDialog(dialog.Select(d => d.ID).ToArray());

        public static bool SeenDialog(params ulong[] ids) => !ids.Except(Main.LocalPlayer.GetModPlayer<DialogPlayer>().SeenCultistDialogIDs).Any();
    }
}
