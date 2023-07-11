using System;
using System.Text;

namespace NoxusBoss.Content.UI
{
    public class Dialog
    {
        public Func<bool> SelectionRequirement;

        public string Inquiry;

        public string Response;

        public ulong ID
        {
            get
            {
                // This isn't guaranteed to not have collisions, but probabilistically that won't actually matter.
                byte[] responseBytes = Encoding.UTF8.GetBytes(Response);
                ulong id = 0uL;

                // Go through all bytes and stuff them into the eight bytes that make up the ID.
                // Once more than eight bytes are looped through they begin to "wrap" around and start scrambling the ID at each of the byte positions.
                for (int i = 0; i < responseBytes.Length; i++)
                {
                    ulong currentByte = responseBytes[i];
                    unchecked
                    {
                        id += currentByte << (i * 8);
                    }
                }

                return id;
            }
        }

        public bool CanBeDisplayed => !XerocCultistDialogRegistry.SeenDialog(ID) && (SelectionRequirement?.Invoke() ?? true);

        public Dialog(string inquiry, string response, Func<bool> selectionRequirement)
        {
            Inquiry = inquiry;
            Response = response;
            SelectionRequirement = selectionRequirement;
        }

        public Dialog(string inquiry, string response) : this(inquiry, response, () => true) { }
    }
}
