namespace Ascii3dEngine.Tests
{
    public static class TestUtilities
    {
        static TestUtilities()
        {
            TestSettings = new Settings();
            Map = new CharMap(TestSettings);
        }

        public readonly static CharMap Map;
        public readonly static Settings TestSettings;
    }
}