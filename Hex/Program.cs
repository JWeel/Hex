namespace Hex
{
    public static class Program
    {
        [System.STAThread]
        private static void Main()
        {
            using var core = new Core();
            core.Run();
        }
    }
}
