namespace ByteForge.Toolkit;

/*
 * __   _____ __  __      _   _    
 * \ \ / / _ )  \/  |__ _| |_| |_  
 *  \ V /| _ \ |\/| / _` |  _| ' \ 
 *   \_/ |___/_|  |_\__,_|\__|_||_|
 *                                 
 */
/// <summary>
/// Provides mathematical functions compatible with Visual Basic 6.0, including random number generation.
/// </summary>
/// <remarks>
/// This class was ported from the original VBMath.vb from the official Microsoft.VisualBasic.Core library.
/// https://github.com/dotnet/runtime/blob/6d0ac32a8e3a53e03b4143899844b587ebb19b49/src/libraries/Microsoft.VisualBasic.Core/src/Microsoft/VisualBasic/VBMath.vb
/// </remarks>
internal static class VBMath
{
    private static int rndSeed = 0;

    /// <summary>
    /// Generates a random floating-point number between 0.0 and 1.0, equivalent to VB6's Rnd() with no arguments.
    /// </summary>
    /// <returns>A random float value in the range [0.0, 1.0).</returns>
    public static float Rnd()
    {
        return Rnd(1.0f);
    }

    /// <summary>
    /// Generates a random floating-point number based on the provided number, equivalent to VB6's Rnd(Number).
    /// If Number is 0, returns the next random number from the current seed.
    /// If Number is negative, uses it to reseed the generator.
    /// If Number is positive, generates a new random number and updates the seed.
    /// </summary>
    /// <param name="Number">The input value to control random number generation. 0 for next random, negative for reseeding, positive for new random.</param>
    /// <returns>A random float value in the range [0.0, 1.0).</returns>
    public static float Rnd(float Number)
    {
        long seed = VBMath.rndSeed;

        //  if parameter is zero, generate float from present seed
        if (Number != 0.0f)
        {
            //  if parameter is negative, use to create new seed
            if (Number < 0.0f)
            {
                //Original C++ code
                //rndSeed = *(ULONG *) & fltVal;
                //rndSeed = (rndSeed + (rndSeed >> 24)) & 0xffffffL;

                seed = BitConverter.ToInt32(BitConverter.GetBytes(Number), 0);

                var i64 = seed;
                i64 = (i64 & 0xFFFFFFFFL);
                seed = (int)((i64 + (i64 >> 24)) & 0xFFFFFFL);
            }

            //  if parameter is non-zero, generate a new seed
            seed = ((long)seed * 0x43FD43FDL + 0xC39EC3L) & 0xFFFFFFL;
        }

        //  copy back seed value to per-project structure
        VBMath.rndSeed = (int)seed;

        //  normalize seed to floating value from 0.0 up to 1.0
        return (float)seed / 16777216.0f;
    }

    /// <summary>
    /// Initializes the random number generator with a seed based on the current time, equivalent to VB6's Randomize with no arguments.
    /// </summary>
    public static void Randomize()
    {
        var sngTimer = GetTimer();
        long seed = VBMath.rndSeed;
        int lValue;

        //  treat Single as a long Integer
        lValue = BitConverter.ToInt32(BitConverter.GetBytes(sngTimer), 0);

        //  xor the upper and lower words of the long and put in
        //  the middle two bytes
        lValue = ((lValue & 0xFFFF) ^ (lValue >> 16)) << 8;

        //  replace the middle two bytes of the seed with lValue
#pragma warning disable CS0675 
        seed = ((seed & 0xFF0000FFL) | lValue);
#pragma warning restore CS0675

        //  copy back seed value to per-project structure
        VBMath.rndSeed = (int)seed;
    }

    /// <summary>
    /// Initializes the random number generator with a seed based on the provided double value, equivalent to VB6's Randomize(Number).
    /// </summary>
    /// <param name="Number">The double value used to generate the seed.</param>
    public static void Randomize(double Number)
    {
        long seed = VBMath.rndSeed;
        int lValue;

        //  for little-endian R8, the high-order Integer is second half
        if (BitConverter.IsLittleEndian)
        {
            lValue = BitConverter.ToInt32(BitConverter.GetBytes(Number), 4);
        }
        else
        {
            lValue = BitConverter.ToInt32(BitConverter.GetBytes(Number), 0);
        }

        //  xor the upper and lower words of the Integer and put in
        //  the middle two bytes
        // Original C++ line
        // lValue = ((lValue & 0xffff) ^ (lValue >> 16)) << 8;
        lValue = ((lValue & 0xFFFF) ^ (lValue >> 16)) << 8;

        //  replace the middle two bytes of the seed with lValue
        //Original C++ line
        // rndSeed = (rndSeed & 0xff0000ff) | lValue;
#pragma warning disable CS0675
        seed = (seed & 0xFF0000FFL) | lValue;
#pragma warning restore CS0675

        //  copy back seed value to per-project structure
        VBMath.rndSeed = (int)seed;
    }

    private static float GetTimer()
    {
        DateTime dt = DateTime.Now;
        return (float)((60 * dt.Hour + dt.Minute) * 60 + dt.Second + (dt.Millisecond / 1000.0));
    }
}