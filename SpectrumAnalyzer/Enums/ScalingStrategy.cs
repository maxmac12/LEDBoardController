namespace SpectrumAnalyzer.Enums
{
    public enum ScalingStrategy
    {
        Decibel,
        Linear,
        Sqrt
    }

    public enum LEDModes
    {
        OFF = 0,
        WHITE,
        COLOR,
        COLOR_PULSE,
        RAINBOW_CYCLE,
        WHITE_OVER_RAINBOW,
        NUM_LED_MODES
    };
}