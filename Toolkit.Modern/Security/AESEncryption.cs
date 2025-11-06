using Microsoft.VisualBasic;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ByteForge.Toolkit;
/*
 *    _   ___ ___ ___                       _   _          
 *   /_\ | __/ __| __|_ _  __ _ _ _  _ _ __| |_(_)___ _ _  
 *  / _ \| _|\__ \ _|| ' \/ _| '_| || | '_ \  _| / _ \ ' \ 
 * /_/ \_\___|___/___|_||_\__|_|  \_, | .__/\__|_\___/_||_|
 *                                |__/|_|                  
 */
/// <summary>
/// The AESEncryption class provides methods for encrypting and decrypting data 
/// using the AES (Advanced Encryption Standard) algorithm.
/// 
/// It includes methods for shifting and rotating values, packing and unpacking 
/// bytes, generating keys, and performing byte substitutions and transformations.
/// </summary>
internal class AESEncryption
{

    private static readonly long packedInCo = BitConverter.ToInt32(new byte[] { 0xB, 0xD, 0x9, 0xE }, 0);

    private long m_Nk;
    private long m_Nb;
    private long m_Nr;
    private readonly byte[] m_fi = new byte[24];
    private readonly byte[] m_ri = new byte[24];
    private readonly long[] m_rco = new long[30];
    private readonly long[] m_fkey = new long[120];
    private readonly long[] m_rkey = new long[120];
    private readonly byte[] m_ptab = new byte[256];
    private readonly byte[] m_ltab = new byte[256];
    private readonly byte[] m_fbsub = new byte[256];
    private readonly byte[] m_rbsub = new byte[256];
    private readonly long[] m_ftable = new long[256];
    private readonly long[] m_rtable = new long[256];

    /// <summary>
    /// Performs a left shift operation on a long value.
    /// </summary>
    /// <param name="lValue">The long value to be shifted.</param>
    /// <param name="iShiftBits">The number of bits to shift.</param>
    /// <returns>The result of the left shift operation.</returns>
    private static long LShift(long lValue, int iShiftBits)
    {
        if (iShiftBits < 0 || iShiftBits > 31)
            throw new ArgumentOutOfRangeException(nameof(iShiftBits), "Value cannot be less than 0 or greater than 31.");

        /*
         * Normally we would use the following code to shift left:
         * 
         *             lValue << iShiftBits
         *             
         * and be done with it. However, the original VB6 implementation
         * of this function uses a custom logic that differs in key aspects.
         * The following is an optimized translation that replaces lookup
         * array access with direct bit operations for better performance.
         * 
         * Original used: l2Power[iShiftBits] and lOnBits[31 - iShiftBits]
         * Optimized uses: (1L << iShiftBits) and ((1L << bits) - 1)
         */
        if (iShiftBits == 0)
            return lValue;

        if (iShiftBits == 31)
            return (lValue & 1) == 0 ? 0x80000000 : 0;

        // Check if the bit that would move into the sign position is set
        var signBitMask = 1L << (31 - iShiftBits);
        if ((lValue & signBitMask) != 0)
        {
            // Mask off the upper bits that would overflow, shift, then set sign bit
            var valueMask = (1L << (32 - iShiftBits)) - 1;
            return ((lValue & valueMask) << iShiftBits) | 0x80000000;
        }

        // Normal shift with upper bits masked off
        var normalMask = (1L << (32 - iShiftBits)) - 1;
        return (lValue & normalMask) << iShiftBits;
    }

    /// <summary>
    /// Performs a right shift operation on a long value.
    /// </summary>
    /// <param name="lValue">The long value to be shifted.</param>
    /// <param name="iShiftBits">The number of bits to shift.</param>
    /// <returns>The result of the right shift operation.</returns>
    private static long RShift(long lValue, int iShiftBits)
    {
        if (iShiftBits < 0 || iShiftBits > 31)
            throw new ArgumentOutOfRangeException(nameof(iShiftBits), "Value cannot be less than 0 or greater than 31.");

        /*
         * Normally we would use the following code to shift right:
         * 
         *             lValue >> iShiftBits
         *             
         * and be done with it. However, the original VB6 implementation
         * of this function uses a custom logic that differs in key aspects.
         * The following is an optimized translation that replaces division
         * by powers of 2 with right shift operations for better performance.
         * 
         * Original used: (lValue & 0x7FFFFFFE) / l2Power[iShiftBits]
         * Optimized uses: (lValue & 0x7FFFFFFE) >> iShiftBits
         * 
         * Original used: (0x40000000 / l2Power[iShiftBits - 1])
         * Optimized uses: 0x40000000 >> (iShiftBits - 1)
         */
        if (iShiftBits == 0)
            return lValue;

        if (iShiftBits == 31)
            return (lValue & 0x80000000) == 0 ? 1 : 0;

        // Replace division with right shift for the main calculation
        var rst = (lValue & 0x7FFFFFFE) >> iShiftBits;

        // If the sign bit was set, apply the VB6-specific adjustment
        if ((lValue & 0x80000000) != 0)
            rst |= 0x40000000L >> (iShiftBits - 1);

        return rst;
    }

    /// <summary>
    /// Performs a left shift operation on a byte value.
    /// </summary>
    /// <param name="bytValue">The byte value to be shifted.</param>
    /// <param name="iShiftBits">The number of bits to shift.</param>
    /// <returns>The result of the left shift operation.</returns>
    private static byte LShiftByte(byte bytValue, byte iShiftBits)
    {
        if (iShiftBits < 0 || iShiftBits > 7)
            throw new ArgumentOutOfRangeException(nameof(iShiftBits), "Value cannot be less than 0 or greater than 7.");

        return (byte)(bytValue << iShiftBits);
    }

    /// <summary>
    /// Performs a right shift operation on a byte value.
    /// </summary>
    /// <param name="bytValue">The byte value to be shifted.</param>
    /// <param name="iShiftBits">The number of bits to shift.</param>
    /// <returns>The result of the right shift operation.</returns>
    private static byte RShiftByte(byte bytValue, byte iShiftBits)
    {
        if (iShiftBits < 0 || iShiftBits > 7)
            throw new ArgumentOutOfRangeException(nameof(iShiftBits), "Value cannot be less than 0 or greater than 7.");

        return (byte)(bytValue >> iShiftBits);
    }

    /// <summary>
    /// Performs a left rotation operation on a long value.
    /// </summary>
    /// <param name="lValue">The byte to be shifted.</param>
    /// <param name="iShiftBits">The number of bits to shift.</param>
    /// <returns>The result of the left rotation operation.</returns>
    private static long RotateLeft(long lValue, int iShiftBits)
    {
        return LShift(lValue, iShiftBits) | RShift(lValue, 32 - iShiftBits);
    }

    /// <summary>
    /// Performs a right rotation operation on a long value.
    /// </summary>
    /// <param name="lValue">The byte to be shifted.</param>
    /// <param name="iShiftBits">The number of bits to shift.</param>
    /// <returns>The result of the left rotation operation.</returns>
    private static long RotateRight(long lValue, int iShiftBits)
    {
        return RShift(lValue, iShiftBits) | LShift(lValue, 32 - iShiftBits);
    }

    /// <summary>
    /// Rotates the bits of a byte value to the left by a specified number of bits.
    /// </summary>
    /// <param name="bytValue">The byte value to rotate.</param>
    /// <param name="iShiftBits">The number of bits to rotate to the left.</param>
    /// <returns>The byte value after the bits have been rotated to the left.</returns>
    private static byte RotateLeftByte(byte bytValue, byte iShiftBits)
    {
        return (byte)(LShiftByte(bytValue, iShiftBits) | RShiftByte(bytValue, (byte)(8 - iShiftBits)));
    }

    /// <summary>
    /// Rotates the bits of a byte value to the right by a specified number of bits.
    /// </summary>
    /// <param name="bytValue">The byte value to rotate.</param>
    /// <param name="iShiftBits">The number of bits to rotate to the right.</param>
    /// <returns>The byte value after the bits have been rotated to the right.</returns>
    private static byte RotateRightByte(byte bytValue, byte iShiftBits)
    {
        return (byte)(RShiftByte(bytValue, iShiftBits) | LShiftByte(bytValue, (byte)(8 - iShiftBits)));
    }

    /// <summary>
    /// Packs an array of bytes into a long value.
    /// </summary>
    /// <param name="B">The byte array to be packed.</param>
    /// <returns>The packed long value.</returns>
    private static long Pack(byte[] B)
    {
        if (B.Length != 4)
            throw new ArgumentOutOfRangeException(nameof(B), "Length must be 4.");

        return BitConverter.ToInt32(B, 0);
    }

    /// <summary>
    /// Packs a subset of a byte array into a long value, starting from a specified index.
    /// </summary>
    /// <param name="B">The byte array to be packed.</param>
    /// <param name="K">The starting index in the byte array.</param>
    /// <returns>The packed long value.</returns>
    private static long PackFrom(byte[] B, long K)
    {
        if (K < 0 || K > B.Length - 4)
            throw new ArgumentOutOfRangeException(nameof(K), "Value cannot be less than 0 or greater than B.Length - 4.");

        var rst = 0L;
        for (var i = 0; i < 4; i++)
            rst |= LShift(B[i + K], i * 8);

        return rst;
    }

    /// <summary>
    /// Unpacks a long value into a byte array.
    /// </summary>
    /// <param name="A">The long value to be unpacked.</param>
    /// <param name="B">The byte array to store the unpacked bytes.</param>
    private static void Unpack(long A, byte[] B)
    {
        if (B.Length != 4)
            throw new ArgumentOutOfRangeException(nameof(B), "Length of the byte array must be 4.");

        var bytes = BitConverter.GetBytes(A);
        Array.Copy(bytes, B, 4);
    }

    /// <summary>
    /// Unpacks a long value into a subset of a byte array, starting from a specified index.
    /// </summary>
    /// <param name="A">The long value to be unpacked.</param>
    /// <param name="B">The byte array to store the unpacked bytes.</param>
    /// <param name="K">The starting index in the byte array.</param>
    private static void UnpackTo(long A, byte[] B, long K)
    {
        if (K < 0 || K > B.Length - 4)
            throw new ArgumentOutOfRangeException(nameof(K), "Value cannot be less than 0 or greater than B.Length - 4.");

        var bytes = BitConverter.GetBytes(A);
        Array.Copy(bytes, 0, B, K, 4);
    }

    /// <summary>
    /// Performs a specific byte operation used in AES encryption.
    /// </summary>
    /// <param name="A">The byte to be processed.</param>
    /// <returns>The result of the operation.</returns>
    private static byte XTime(byte A)
    {
        var B = (A & 0x80) != 0 ? (byte)0x1B : (byte)0;
        A = LShiftByte(A, 1);
        A ^= B;
        return A;
    }

    /// <summary>
    /// Multiplies two bytes in the Galois Field(2^8) used in AES.
    /// </summary>
    /// <param name="X">The first byte to be multiplied.</param>
    /// <param name="Y">The second byte to be multiplied.</param>
    /// <returns>The product of the two bytes in the Galois Field(2^8).</returns>
    /// <remarks>
    /// A Galois Field, named after the mathematician Évariste Galois, is a finite 
    /// field, meaning it contains a finite number of elements. In the context of 
    /// cryptography, particularly in algorithms like AES (Advanced Encryption Standard), 
    /// Galois Fields are used for mathematical operations.
    /// <br/><br/>
    /// The most commonly used Galois Field in cryptography is GF(2^8), which 
    /// contains 256 elements and is therefore suitable for operations on bytes.
    /// In GF(2^8), addition and subtraction are equivalent to the XOR operation, 
    /// and multiplication and division are more complex but can be implemented 
    /// efficiently using lookup tables.
    /// <br/><br/>
    /// The use of Galois Fields in cryptography provides several benefits.
    /// The operations are reversible, which is necessary for decryption, and they 
    /// provide good diffusion and confusion, which are desirable properties in a cipher. 
    /// The structure of a Galois Field also allows for efficient implementation of 
    /// the cipher in hardware or software.
    /// </remarks>
    private byte ByteMultiplication(byte X, byte Y)
    {
        return (X != 0 && Y != 0) ? m_ptab[((long)m_ltab[X] + (long)m_ltab[Y]) % 255] : (byte)0;
    }

    /// <summary>
    /// Performs a byte substitution on a long value using the forward substitution box.
    /// </summary>
    /// <param name="A">The long value to be substituted.</param>
    /// <returns>The substituted long value.</returns>
    private long ByteSustitutionBox(long A)
    {
        var B = new byte[4];
        Unpack(A, B);
        for (var i = 0; i < B.Length; i++)
            B[i] = m_fbsub[B[i]];
        return Pack(B);
    }

    /// <summary>
    /// Multiplies two long values in the Galois Field(2^8) used in AES.
    /// </summary>
    /// <param name="X">The first long value to be multiplied.</param>
    /// <param name="Y">The second long value to be multiplied.</param>
    /// <returns>The product of the two long values in the Galois Field(2^8).</returns>
    private long Product(long X, long Y)
    {
        var XB = new byte[4];
        var YB = new byte[4];
        Unpack(X, XB);
        Unpack(Y, YB);
        long result = 0;
        for (var i = 0; i < 4; i++)
            result ^= ByteMultiplication(XB[i], YB[i]);

        return result;
    }

    /// <summary>
    /// Performs the inverse MixColumns operation in AES, which operates on a column of four bytes at a time.
    /// </summary>
    /// <param name="X">The long value representing the column of four bytes.</param>
    /// <returns>The result of the inverse MixColumns operation.</returns>
    private long InverseMixColumns(long X)
    {
        var B = new byte[4];
        var M = packedInCo;
        for (var i = 3; i >= 0; i--)
        {
            B[i] = (byte)Product(M, X);
            M = RotateLeft(M, 24);
        }

        return Pack(B);
    }

    /// <summary>
    /// Performs a byte substitution operation using a precomputed substitution table.
    /// </summary>
    /// <param name="x">The byte to be substituted.</param>
    /// <returns>The substituted byte.</returns>
    private byte SubstituteByte(byte x)
    {
        var y = x = m_ptab[255 - m_ltab[x]];
        for (var i = 0; i < 4; i++)
        {
            x = RotateLeftByte(x, 1);
            y ^= x;
        }
        return (byte)(y ^ 0x63);
    }

    /// <summary>
    /// Generates the substitution tables and other necessary data structures 
    /// used in AES encryption and decryption.
    /// </summary>
    /// <remarks>
    /// Substitution tables, also known as S-boxes, are used in various encryption 
    /// algorithms, including AES (Advanced Encryption Standard). They are a fundamental 
    /// part of substitution-permutation network (SPN) based ciphers.
    /// <br/><br/>
    /// An S-box is essentially a lookup table used to perform substitution in the 
    /// process of encryption or decryption.In the context of AES, the S-box is a 
    /// matrix of 16x16, giving a total of 256 possible values. Each byte of the 
    /// state is replaced by the byte at the corresponding row and column in the 
    /// S-box.
    /// <br/><br/>
    /// The purpose of the S-box is to provide non-linearity in the cipher, which 
    /// helps to protect against linear and differential cryptanalysis.The specific 
    /// values in the S-box are chosen to maximize the non-linearity and the avalanche 
    /// effect (where a small change in input produces a significant change in output).
    /// <br/><br/>
    /// In the case of AES, the S-box is designed in such a way that there are no 
    /// fixed points or opposite fixed points, which means that no byte value, 
    /// when input to the S - box, will output itself or its bitwise complement.
    /// This contributes to the security of the cipher.
    /// </remarks>
    private void GenTables()
    {
        var B = new byte[4];
        m_ltab[0] = 0;
        m_ptab[0] = 1;
        m_ltab[1] = 0;
        m_ptab[1] = 3;
        m_ltab[3] = 1;

        for (var I = 2; I <= 255; I++)
        {
            m_ptab[I] = (byte)(m_ptab[I - 1] ^ XTime(m_ptab[I - 1]));
            m_ltab[m_ptab[I]] = (byte)I;
        }

        m_fbsub[0] = 0x63;
        m_rbsub[0x63] = 0;

        for (var I = 1; I <= 255; I++)
        {
            var Y = SubstituteByte((byte)I);
            m_fbsub[I] = Y;
            m_rbsub[Y] = (byte)I;
        }

        byte n = 1;
        for (var I = 0; I <= 29; I++)
        {
            m_rco[I] = n;
            n = XTime(n);
        }

        for (var I = 0; I <= 255; I++)
        {
            var Y = m_fbsub[I];
            B[3] = (byte)(Y ^ XTime(Y));
            B[2] = Y;
            B[1] = Y;
            B[0] = XTime(Y);
            m_ftable[I] = Pack(B);

            Y = m_rbsub[I];
            B[3] = ByteMultiplication(0xB, Y);
            B[2] = ByteMultiplication(0xD, Y);
            B[1] = ByteMultiplication(0x9, Y);
            B[0] = ByteMultiplication(0xE, Y);
            m_rtable[I] = Pack(B);
        }
    }

    /// <summary>
    /// Generates the round keys for AES encryption and decryption 
    /// based on the input key.
    /// </summary>
    /// <param name="NB">The block size in 32-bit words. Usually 4 for AES.</param>
    /// <param name="NK">The key size in 32-bit words. 4 for AES-128, 6 for AES-192, 8 for AES-256.</param>
    /// <param name="Key">The input key used to generate the round keys.</param>
    /// <remarks>
    /// In the context of symmetric encryption algorithms like AES 
    /// (Advanced Encryption Standard), round keys are derived from 
    /// the original encryption key using a key schedule algorithm.
    /// <br/><br/>
    /// The original key is expanded into an array of keys, one for 
    /// each round of the encryption process.These round keys are 
    /// used in each round of the encryption and decryption process. 
    /// The use of different keys in each round adds to the security 
    /// of the algorithm, making it resistant to various types of 
    /// cryptographic attacks.
    /// <br/><br/>
    /// In AES, the number of rounds and thus the number of round keys 
    /// depends on the size of the original key.For example, AES-128 
    /// uses 10 rounds, AES-192 uses 12 rounds, and AES-256 uses 14 
    /// rounds.Each round key is the same size as the block size, which 
    /// is 128 bits in AES.
    /// </remarks>
    private void GKey(long NB, long NK, byte[] Key)
    {
        long I, J, K, M, N;
        long C1, C2, C3;
        var CipherKey = new long[8];
        m_Nb = NB;
        m_Nk = NK;
        m_Nr = m_Nb >= m_Nk ? 6 + m_Nb : 6 + m_Nk;
        C1 = 1;
        C2 = 2 + ((m_Nb < 8) ? 0 : 1);
        C3 = 3 + ((m_Nb < 8) ? 0 : 1);

        for (J = 0; J < NB; J++)
        {
            M = J * 3;
            m_fi[M] = (byte)((J + C1) % NB);
            m_fi[M + 1] = (byte)((J + C2) % NB);
            m_fi[M + 2] = (byte)((J + C3) % NB);
            m_ri[M] = (byte)((NB + J - C1) % NB);
            m_ri[M + 1] = (byte)((NB + J - C2) % NB);
            m_ri[M + 2] = (byte)((NB + J - C3) % NB);
        }

        N = m_Nb * (m_Nr + 1);
        for (I = 0; I < m_Nk; I++)
        {
            J = I * 4;
            CipherKey[I] = PackFrom(Key, J);
        }

        for (I = 0; I < m_Nk; I++)
            m_fkey[I] = CipherKey[I];

        J = m_Nk;
        K = 0;
        while (J < N)
        {
            m_fkey[J] = m_fkey[J - m_Nk] ^ ByteSustitutionBox(RotateLeft(m_fkey[J - 1], 24)) ^ m_rco[K];
            _ = RotateLeft(m_fkey[J - 1], 24);
            if (m_Nk <= 6)
            {
                I = 1;
                while (I < m_Nk && (I + J) < N)
                {
                    m_fkey[I + J] = m_fkey[I + J - m_Nk] ^ m_fkey[I + J - 1];
                    I++;
                }
            }
            else
            {
                I = 1;
                while (I < 4 && (I + J) < N)
                {
                    m_fkey[I + J] = m_fkey[I + J - m_Nk] ^ m_fkey[I + J - 1];
                    I++;
                }
                if (J + 4 < N)
                {
                    m_fkey[J + 4] = m_fkey[J + 4 - m_Nk] ^ ByteSustitutionBox(m_fkey[J + 3]);
                }
                I = 5;
                while (I < m_Nk && (I + J) < N)
                {
                    m_fkey[I + J] = m_fkey[I + J - m_Nk] ^ m_fkey[I + J - 1];
                    I++;
                }
            }
            J += m_Nk;
            K++;
        }
        for (J = 0; J < m_Nb; J++)
        {
            m_rkey[J + N - NB] = m_fkey[J];
        }
        I = m_Nb;
        while (I < N - m_Nb)
        {
            K = N - m_Nb - I;
            for (J = 0; J < m_Nb; J++)
            {
                m_rkey[K + J] = InverseMixColumns(m_fkey[I + J]);
            }
            I += m_Nb;
        }
        J = N - m_Nb;
        while (J < N)
        {
            m_rkey[J - N + m_Nb] = m_fkey[J];
            J++;
        }
    }

    /// <summary>
    /// Encrypts a buffer of bytes using the AES encryption algorithm.
    /// The buffer size should be a multiple of the block size (16 bytes for AES).
    /// The encryption is done in-place, meaning the original bytes in the buffer are replaced by the encrypted bytes.
    /// </summary>
    /// <param name="Buff">The buffer of bytes to be encrypted.</param>
    private void EncryptBuffer(byte[] Buff)
    {
        var A = new long[8];
        var B = new long[8];
        long[] X, Y, T;

        /*
         * Optimization: Replaced byteMask with 0xFF constant
         * Original used: byteMask (which equals 255)
         * Optimized uses: 0xFF (same value, no array lookup)
         * This eliminates multiple array accesses per encryption round.
         */
        const long byteMask = 0xFF;

        for (var I = 0; I < m_Nb; I++)
        {
            A[I] = PackFrom(Buff, I * 4);
            A[I] ^= m_fkey[I];
        }
        var K = (int)m_Nb;
        X = A;
        Y = B;
        for (var I = 1; I < m_Nr; I++)
        {
            for (var J = 0; J < m_Nb; J++)
            {
                var M = J * 3;
                Y[J] = m_fkey[K++] ^ m_ftable[X[J] & byteMask] ^
                    RotateLeft(m_ftable[RShift(X[m_fi[M]], 8) & byteMask], 8) ^
                    RotateLeft(m_ftable[RShift(X[m_fi[M + 1]], 16) & byteMask], 16) ^
                    RotateLeft(m_ftable[RShift(X[m_fi[M + 2]], 24) & byteMask], 24);
            }
            T = X;
            X = Y;
            Y = T;
        }
        for (var J = 0; J < m_Nb; J++)
        {
            var M = J * 3;
            Y[J] = m_fkey[K++] ^ m_fbsub[X[J] & byteMask] ^
                RotateLeft(m_fbsub[RShift(X[m_fi[M]], 8) & byteMask], 8) ^
                RotateLeft(m_fbsub[RShift(X[m_fi[M + 1]], 16) & byteMask], 16) ^
                RotateLeft(m_fbsub[RShift(X[m_fi[M + 2]], 24) & byteMask], 24);
        }
        for (var I = 0; I < m_Nb; I++)
        {
            UnpackTo(Y[I], Buff, I * 4);
            X[I] = 0;
            Y[I] = 0;
        }
    }

    /// <summary>
    /// Decrypts a buffer of bytes using the AES decryption algorithm.
    /// The buffer size should be a multiple of the block size (16 bytes for AES).
    /// The decryption is done in-place, meaning the original bytes in the buffer are replaced by the decrypted bytes.
    /// </summary>
    /// <param name="Buff">The buffer of bytes to be decrypted.</param>
    private void DecryptBuffer(byte[] Buff)
    {
        var A = new long[8];
        var B = new long[8];
        long[] X, Y, T;

        /*
         * Optimization: Replaced byteMask with 0xFF constant
         * Original used: byteMask (which equals 255)
         * Optimized uses: 0xFF (same value, no array lookup)
         * This eliminates multiple array accesses per encryption round.
         */
        const long byteMask = 0xFF;

        for (var I = 0; I < m_Nb; I++)
        {
            A[I] = PackFrom(Buff, I * 4);
            A[I] ^= m_rkey[I];
        }
        var K = (int)m_Nb;
        X = A;
        Y = B;
        for (var I = 1; I < m_Nr; I++)
        {
            for (var J = 0; J < m_Nb; J++)
            {
                var M = J * 3;
                Y[J] = m_rkey[K++] ^ m_rtable[X[J] & byteMask] ^
                    RotateLeft(m_rtable[RShift(X[m_ri[M]], 8) & byteMask], 8) ^
                    RotateLeft(m_rtable[RShift(X[m_ri[M + 1]], 16) & byteMask], 16) ^
                    RotateLeft(m_rtable[RShift(X[m_ri[M + 2]], 24) & byteMask], 24);
            }
            T = X;
            X = Y;
            Y = T;
        }
        for (var J = 0; J < m_Nb; J++)
        {
            var M = J * 3;
            Y[J] = m_rkey[K++] ^ m_rbsub[X[J] & byteMask] ^
                RotateLeft(m_rbsub[RShift(X[m_ri[M]], 8) & byteMask], 8) ^
                RotateLeft(m_rbsub[RShift(X[m_ri[M + 1]], 16) & byteMask], 16) ^
                RotateLeft(m_rbsub[RShift(X[m_ri[M + 2]], 24) & byteMask], 24);
        }
        for (var I = 0; I < m_Nb; I++)
        {
            UnpackTo(Y[I], Buff, I * 4);
            X[I] = Y[I] = 0;
        }
    }

    /// <summary>
    /// Encrypts a byte array using the AES encryption algorithm and a specified password.
    /// </summary>
    /// <param name="bytMessage">The byte array to be encrypted.</param>
    /// <param name="bytPassword">The password used for encryption, represented as a byte array.</param>
    /// <returns>The encrypted byte array.</returns>
    private byte[] EncryptBytes(byte[] bytMessage, byte[] bytPassword)
    {
        if (bytMessage == null || bytMessage.Length == 0 || bytPassword == null || bytPassword.Length == 0)
            return Array.Empty<byte>();

        var bytKey = new byte[32];
        Array.Copy(bytPassword, bytKey, Math.Min(bytPassword.Length, 32));

        GenTables();
        GKey(8, 8, bytKey);

        var lLength = bytMessage.Length;
        var lEncodedLength = lLength + 4;
        lEncodedLength = lEncodedLength % 32 != 0 ? lEncodedLength + 32 - (lEncodedLength % 32) : lEncodedLength;

        var bytIn = new byte[lEncodedLength];
        var bytOut = new byte[lEncodedLength];

        Buffer.BlockCopy(BitConverter.GetBytes(lLength), 0, bytIn, 0, 4);
        Buffer.BlockCopy(bytMessage, 0, bytIn, 4, lLength);

        var bytTemp = new byte[32];
        for (var lCount = 0; lCount < lEncodedLength; lCount += 32)
        {
            Buffer.BlockCopy(bytIn, lCount, bytTemp, 0, 32);
            EncryptBuffer(bytTemp);
            Buffer.BlockCopy(bytTemp, 0, bytOut, lCount, 32);
        }

        return bytOut;
    }

    /// <summary>
    /// Decrypts an array of bytes using the specified password.
    /// </summary>
    /// <param name="bytIn">The byte array to decrypt.</param>
    /// <param name="bytPassword">The password used for decryption, as a byte array.</param>
    /// <returns>The decrypted byte array.</returns>
    private byte[] DecryptBytes(byte[] bytIn, byte[] bytPassword)
    {
        if (bytIn == null || bytIn.Length == 0 || bytPassword == null || bytPassword.Length == 0 || bytIn.Length % 32 != 0)
            return Array.Empty<byte>();

        var bytKey = new byte[32];
        Array.Copy(bytPassword, bytKey, Math.Min(bytPassword.Length, 32));

        GenTables();
        GKey(8, 8, bytKey);

        var bytOut = new byte[bytIn.Length];
        var bytTemp = new byte[32];
        for (var lCount = 0; lCount < bytIn.Length; lCount += 32)
        {
            Buffer.BlockCopy(bytIn, lCount, bytTemp, 0, 32);
            DecryptBuffer(bytTemp);
            Buffer.BlockCopy(bytTemp, 0, bytOut, lCount, 32);
        }

        var lLength = BitConverter.ToInt32(bytOut, 0);
        if (lLength > bytIn.Length - 4)
        {
            return Array.Empty<byte>();
        }

        var bytMessage = new byte[lLength];
        Buffer.BlockCopy(bytOut, 4, bytMessage, 0, lLength);
        return bytMessage;
    }

    /// <summary>
    /// Encrypts a string using the AES encryption algorithm and a specified password.
    /// </summary>
    /// <param name="plainText">The string to be encrypted.</param>
    /// <param name="Key">The password used for encryption.</param>
    /// <returns>The encrypted string.</returns>
    public string Encrypt(string? plainText, string Key)
    {
        if (string.IsNullOrEmpty(Key))
            throw new ArgumentNullException(nameof(Key));
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var Pass = Encoding.Unicode.GetBytes(Key);
        var ByteIn = Encoding.Unicode.GetBytes(plainText);
        var ByteOut = EncryptBytes(ByteIn, Pass);
        return BitConverter.ToString(ByteOut).Replace("-", "");
    }

    /// <summary>
    /// Decrypts a string using the AES decryption algorithm and a specified password.
    /// </summary>
    /// <param name="cipherText">The encrypted string to be decrypted.</param>
    /// <param name="Key">The password used for decryption.</param>
    /// <returns>The decrypted string.</returns>
    public string Decrypt(string? cipherText, string Key)
    {
        if (string.IsNullOrEmpty(Key))
            throw new ArgumentNullException(nameof(Key));
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        try
        {
            var Pass = Encoding.Unicode.GetBytes(Key);

            var ByteIn = Enumerable.Range(0, cipherText.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(cipherText.Substring(x, 2), 16))
                             .ToArray();
            var ByteOut = DecryptBytes(ByteIn, Pass);
            return Encoding.Unicode.GetString(ByteOut);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Decryption failed. The data may be corrupted or the key may be incorrect.", ex);
        }
    }

    /// <summary>
    /// Generates a cryptographic key of a specified length using a given seed.
    /// The generated key is a string of characters.
    /// </summary>
    /// <param name="Seed">The seed used for key generation. Different seeds will produce different keys.</param>
    /// <param name="Length">The desired length of the key. The generated key will contain this many characters.</param>
    /// <returns>The generated cryptographic key as a string.</returns>
    public static string GenerateKey(int Seed, int Length)
    {
        var rnd = new VB6Random(Seed);
        var sb = new StringBuilder();
        while (sb.Length < Length)
        {
            char C;
            do
            {
                C = (char)rnd.Next(48, 123);
            } while (!IsAsciiLetterOrDigit(C));
            sb.Append(C);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Determines if the given character is an ASCII letter.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>true if the character is an ASCII letter; otherwise, false.</returns>
    public static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';

    /// <summary>
    /// Determines if the given character is an ASCII digit.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>true if the character is an ASCII digit; otherwise, false.</returns>
    public static bool IsAsciiDigit(char c) => (c >= '0' && c <= '9');

    /// <summary>
    /// Determines if the given character is an ASCII letter or digit.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>true if the character is an ASCII letter or digit; otherwise, false.</returns>
    public static bool IsAsciiLetterOrDigit(char c) => IsAsciiLetter(c) | IsAsciiDigit(c);

    /// <summary>
    /// The VB6Random class provides methods for generating random numbers in a way that is compatible with VB6.
    /// </summary>
    private class VB6Random
    {
        /// <summary>
        /// Initializes a new instance of the VB6Random class with the specified seed.
        /// </summary>
        public VB6Random(int seed)
        {
            _ = VBMath.Rnd(-1);
            VBMath.Randomize(seed);
        }

        /// <summary>
        /// Returns a random float between 0 (inclusive) and 1 (exclusive).
        /// </summary>
        public float Next() => VBMath.Rnd();

        /// <summary>
        /// Generates a random integer that is within a specified range.
        /// </summary>
        /// <param name="min">The inclusive lower bound of the random number returned.</param>
        /// <param name="max">The exclusive upper bound of the random number returned. max must be greater than or equal to min.</param>
        /// <returns>A 32-bit signed integer greater than or equal to min and less than max; that is, the range of return values includes min but not max.</returns>
        public int Next(int min, int max)
        {
            return (int)(Next() * (max - min) + min);
        }
    }
}
