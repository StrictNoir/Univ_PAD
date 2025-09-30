

namespace Subscriber
{
    public  static class CheckpointCreator
    {
        private static string GetCheckpointDir()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".broker_checkpoints");
        }

        private static string GetCheckpointPath(string host, int port, string topic)
        {
            return Path.Combine(GetCheckpointDir(), $"{host}_{port}_{topic}.ckpt");
        }

        public static string? LoadCheckpoint(string host, int port, string topic)
        {
            string path = GetCheckpointPath(host, port, topic);
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Trim();
            }
            return null;
        }

        public static void SaveCheckpoint(string host, int port, string topic, string storeId)
        {
            string dir = GetCheckpointDir();
            Directory.CreateDirectory(dir);

            string path = GetCheckpointPath(host, port, topic);
            File.WriteAllText(path, storeId);
        }
    }
}
