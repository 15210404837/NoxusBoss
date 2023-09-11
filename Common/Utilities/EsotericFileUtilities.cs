using System.IO;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        public static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (IOException)
            {
                // The file is unavailable because it is:
                // 1. Still being written to
                // 2. Being processed by another thread, or
                // 3. Does not exist (has already been processed).
                return true;
            }

            // The file is not locked.
            return false;
        }
    }
}
