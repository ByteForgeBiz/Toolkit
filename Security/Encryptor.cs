namespace ByteForge.Toolkit
{
    /*
     *  ___                       _           
     * | __|_ _  __ _ _ _  _ _ __| |_ ___ _ _ 
     * | _|| ' \/ _| '_| || | '_ \  _/ _ \ '_|
     * |___|_||_\__|_|  \_, | .__/\__\___/_|  
     *                  |__/|_|               
     */
    /// <summary>  
    /// Provides methods for encrypting and decrypting strings using AES encryption.  
    /// </summary>  
    public class Encryptor
    {
        private readonly AESEncryption aes = new AESEncryption();
        private readonly string secretKey;

        /// <summary>  
        /// Initializes a new instance of the <see cref="Encryptor"/> class.  
        /// </summary>  
        /// <param name="seed">The seed used to generate the secret key.</param>  
        /// <param name="size">The size of the secret key.</param>  
        public Encryptor(int seed, int size) => secretKey = AESEncryption.GenerateKey(seed, size);

        /// <summary>  
        /// Encrypts the specified plain text.  
        /// </summary>  
        /// <param name="plainText">The plain text to encrypt.</param>  
        /// <returns>The encrypted text.</returns>  
        public string Encrypt(string plainText) => aes.Encrypt(plainText, secretKey);

        /// <summary>  
        /// Decrypts the specified cipher text.  
        /// </summary>  
        /// <param name="cipherText">The cipher text to decrypt.</param>  
        /// <returns>The decrypted text.</returns>  
        public string Decrypt(string cipherText) => aes.Decrypt(cipherText, secretKey);

        /// <summary>  
        /// Encrypts the specified plain text using a new instance of the <see cref="Encryptor"/> class.  
        /// </summary>  
        /// <param name="seed">The seed used to generate the secret key.</param>  
        /// <param name="size">The size of the secret key.</param>  
        /// <param name="plainText">The plain text to encrypt.</param>  
        /// <returns>The encrypted text.</returns>  
        public static string Encrypt(int seed, int size, string plainText) => new Encryptor(seed, size).Encrypt(plainText);

        /// <summary>  
        /// Decrypts the specified cipher text using a new instance of the <see cref="Encryptor"/> class.  
        /// </summary>  
        /// <param name="seed">The seed used to generate the secret key.</param>  
        /// <param name="size">The size of the secret key.</param>  
        /// <param name="cipherText">The cipher text to decrypt.</param>  
        /// <returns>The decrypted text.</returns>  
        public static string Decrypt(int seed, int size, string cipherText) => new Encryptor(seed, size).Decrypt(cipherText);
    }
}