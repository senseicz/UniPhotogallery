using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UniPhotoGallery.Globals
{
	/// <summary>
	/// Trída poskytující metody pro kryptování a dekryptování stringu (hesel)
	/// a vzájemné porovnávání. Využívá symetrickou (algoritmus Rijndael)
	/// šifru.
	/// 
	/// Priklad pouziti:
	///		Page.Response.Write(new Crypto().Encrypt("Toto je test") + "<br />");
	///		Page.Response.Write(new Crypto().Decrypt(new Crypto().Encrypt("Toto je test")));
	///		Page.Response.Write(Crypto.Porovnej(hash1, hash2).ToString());
	/// </summary>
	/// <remarks>
	/// Nezapomente si prilinkovat namespace: EMPIRE.SharedObjects.Crypto
	/// Nektere komentare jsou v anglictine = english
	///	</remarks>
	public class Crypto
	{

		#region Privatni promenne
		
		/// <summary>
		/// Passphrase from which a pseudo-random password will be derived. The
		/// derived password will be used to generate the encryption key.
		/// Passphrase can be any string. In this example we assume that this
		/// passphrase is an ASCII string.
		/// </summary>
		private string _passPhrase      = "B0h3mkA007";        // can be any string
		
		/// <summary>
		/// Salt value used along with passphrase to generate password. Salt can
		/// be any string. In this example we assume that salt is an ASCII string.
		/// </summary>
		private string _saltValue       = "s@1tValue";        // can be any string

		/// <summary>
		/// Hash algorithm used to generate password. Allowed values are: "MD5" and
		/// "SHA1". SHA1 hashes are a bit slower, but more secure than MD5 hashes.
		/// </summary>
		private string _hashAlgorithm   = "SHA1";             // can be "MD5"

		/// <summary>
		/// Number of iterations used to generate password. One or two iterations
		/// should be enough.
		/// </summary>
		private int _passwordIterations = 2;                  // can be any number

		/// <summary>
		/// Initialization vector (or IV). This value is required to encrypt the
		/// first block of plaintext data. For RijndaelManaged class IV must be 
		/// exactly 16 ASCII characters long.
		/// </summary>
		private string _initVector      = "@1B2c3D4e5F6g7H8"; // must be 16 bytes

		/// <summary>
		/// Size of encryption key in bits. Allowed values are: 128, 192, and 256. 
		/// Longer keys are more secure than shorter keys.
		/// </summary>
		private int _keySize            = 256;                // can be 192 or 128
		#endregion

		#region Konstruktory
		/// <summary>
		/// Konstrutor
		/// </summary>
		public Crypto()
		{
		}
		#endregion

		#region Public metody
		/// <SUMMARY>
		/// Zakryptuje poslany text s pouzitim Rijndaelova symetrickeho algoritmu
		/// a vrati zakryptovany base64-encoded string.
		/// </SUMMARY>
		/// <PARAM name="plainText">
		/// Hodnota ktera ma byt zakryptovana.
		/// </PARAM>
		/// <RETURNS>
		/// Zakryptovany base64-encoded string
		/// </RETURNS>
		public string Encrypt(string plainText)
		{
			// Convert strings into byte arrays.
			// Let us assume that strings only contain ASCII codes.
			// If strings include Unicode characters, use Unicode, UTF7, or UTF8 
			// encoding.
			byte[] initVectorBytes = Encoding.ASCII.GetBytes(_initVector);
			byte[] saltValueBytes  = Encoding.ASCII.GetBytes(_saltValue);
        
			// Convert our plaintext into a byte array.
			// Let us assume that plaintext contains UTF8-encoded characters.
			byte[] plainTextBytes  = Encoding.UTF8.GetBytes(plainText);
        
			// First, we must create a password, from which the key will be derived.
			// This password will be generated from the specified passphrase and 
			// salt value. The password will be created using the specified hash 
			// algorithm. Password creation can be done in several iterations.
			PasswordDeriveBytes password = new PasswordDeriveBytes(
				_passPhrase, 
				saltValueBytes, 
				_hashAlgorithm, 
				_passwordIterations);
        
			// Use the password to generate pseudo-random bytes for the encryption
			// key. Specify the size of the key in bytes (instead of bits).
			byte[] keyBytes = password.GetBytes(_keySize / 8);
        
			// Create uninitialized Rijndael encryption object.
			RijndaelManaged symmetricKey = new RijndaelManaged();
        
			// It is reasonable to set encryption mode to Cipher Block Chaining
			// (CBC). Use default options for other symmetric key parameters.
			symmetricKey.Mode = CipherMode.CBC;        
        
			// Generate encryptor from the existing key bytes and initialization 
			// vector. Key size will be defined based on the number of the key 
			// bytes.
			ICryptoTransform encryptor = symmetricKey.CreateEncryptor(
				keyBytes, 
				initVectorBytes);
        
			// Define memory stream which will be used to hold encrypted data.
			MemoryStream memoryStream = new MemoryStream();        
                
			// Define cryptographic stream (always use Write mode for encryption).
			CryptoStream cryptoStream = new CryptoStream(memoryStream, 
				encryptor,
				CryptoStreamMode.Write);
			// Start encrypting.
			cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                
			// Finish encrypting.
			cryptoStream.FlushFinalBlock();

			// Convert our encrypted data from a memory stream into a byte array.
			byte[] cipherTextBytes = memoryStream.ToArray();
                
			// Close both streams.
			memoryStream.Close();
			cryptoStream.Close();
        
			// Convert encrypted data into a base64-encoded string.
			string cipherText = Convert.ToBase64String(cipherTextBytes);
        
			// Return encrypted string.
			return cipherText;
		}
    
		/// <SUMMARY>
		/// Dekryptuje vstupni text pomoci Rijndaelova symetrickeho algoritmu
		/// a vrati dekryptovany string.
		/// </SUMMARY>
		/// <PARAM name="cipherText">
		/// Base64-formatted zakryptovana hodnota.
		/// </PARAM>
		/// <RETURNS>
		/// Dekryptovany string.
		/// </RETURNS>
		public string Decrypt(string cipherText)
		{
			// Convert strings defining encryption key characteristics into byte
			// arrays. Let us assume that strings only contain ASCII codes.
			// If strings include Unicode characters, use Unicode, UTF7, or UTF8
			// encoding.
			byte[] initVectorBytes = Encoding.ASCII.GetBytes(_initVector);
			byte[] saltValueBytes  = Encoding.ASCII.GetBytes(_saltValue);
        
			// Convert our ciphertext into a byte array.
			byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
        
			// First, we must create a password, from which the key will be 
			// derived. This password will be generated from the specified 
			// passphrase and salt value. The password will be created using
			// the specified hash algorithm. Password creation can be done in
			// several iterations.
			PasswordDeriveBytes password = new PasswordDeriveBytes(
				_passPhrase, 
				saltValueBytes, 
				_hashAlgorithm, 
				_passwordIterations);
        
			// Use the password to generate pseudo-random bytes for the encryption
			// key. Specify the size of the key in bytes (instead of bits).
			byte[] keyBytes = password.GetBytes(_keySize / 8);
        
			// Create uninitialized Rijndael encryption object.
			RijndaelManaged symmetricKey = new RijndaelManaged();
        
			// It is reasonable to set encryption mode to Cipher Block Chaining
			// (CBC). Use default options for other symmetric key parameters.
			symmetricKey.Mode = CipherMode.CBC;
        
			// Generate decryptor from the existing key bytes and initialization 
			// vector. Key size will be defined based on the number of the key 
			// bytes.
			ICryptoTransform decryptor = symmetricKey.CreateDecryptor(
				keyBytes, 
				initVectorBytes);
        
			// Define memory stream which will be used to hold encrypted data.
			MemoryStream  memoryStream = new MemoryStream(cipherTextBytes);
                
			// Define cryptographic stream (always use Read mode for encryption).
			CryptoStream  cryptoStream = new CryptoStream(memoryStream, 
				decryptor,
				CryptoStreamMode.Read);

			// Since at this point we don't know what the size of decrypted data
			// will be, allocate the buffer long enough to hold ciphertext;
			// plaintext is never longer than ciphertext.
			byte[] plainTextBytes = new byte[cipherTextBytes.Length];
        
			// Start decrypting.
			int decryptedByteCount = cryptoStream.Read(plainTextBytes, 
				0, 
				plainTextBytes.Length);
                
			// Close both streams.
			memoryStream.Close();
			cryptoStream.Close();
        
			// Convert decrypted data into a string. 
			// Let us assume that the original plaintext string was UTF8-encoded.
			string plainText = Encoding.UTF8.GetString(plainTextBytes, 
				0, 
				decryptedByteCount);
        
			// Return decrypted string.   
			return plainText;
		}


		/// <summary>
		/// Porovna dva hashe a vrati true pokud jsou stejne a false
		///  pokud se lisi
		/// </summary>
		/// <param name="hash1">prvni hash</param>
		/// <param name="hash2">druhy hash</param>
		/// <returns>true/false</returns>
		public static bool Porovnej(byte[] hash1, byte[] hash2)
		{
			int i=0;
			bool stejne=true;
			do
			{
				if(hash1[i]!=hash2[i])
				{
					stejne=false;
					break;
				}
				i++;
			}while(i<hash1.Length);
			return stejne;
		}

		/// <summary>
		/// Porovna dva hashe a vrati true pokud jsou stejne a false
		/// pokud se lisi
		/// </summary>
		/// <param name="hash1">hash1 jako string</param>
		/// <param name="hash2">hash2 jako string</param>
		/// <returns>true/false</returns>
		public static bool Porovnej(string hash1, string hash2)
		{
			return Porovnej((new UnicodeEncoding()).GetBytes(hash1), (new UnicodeEncoding()).GetBytes(hash2));
		}

		public static string MD5Crypto(string text)
		{
			Byte[] dataToHash = new Crypto().ConvertStringToByteArrayASCII(text);
			byte[] hashValue = (new MD5CryptoServiceProvider()).ComputeHash(dataToHash);
			//return hashValue.ToString();
			return BitConverter.ToString(hashValue);
		}

		public static string MD5Crypto(int id, string text)
		{
			string unikatniString = DateTime.Now.Minute.ToString() + DateTime.Now.Millisecond.ToString() + id.ToString() + text;
			return Crypto.MD5Crypto(unikatniString);			
		}


		#endregion

		#region Private metody

		/// <summary>
		/// Konvertuje string na ByteArray
		/// </summary>
		/// <param name="s">vstupni string (napr. heslo)</param>
		/// <returns></returns>
		private Byte[] ConvertStringToByteArray(String s)
		{
			return (new UnicodeEncoding()).GetBytes(s);
		}

		private Byte[] ConvertStringToByteArrayASCII(String s)
		{
			return (new ASCIIEncoding()).GetBytes(s);
		}

		private Byte[] ConvertStringToByteArrayUTF8(String s)
		{
			return (new UTF8Encoding()).GetBytes(s);
		}


		private Byte[] ConvertStringToByteArrayUTF7(String s)
		{
			return (new UTF7Encoding()).GetBytes(s);
		}

		#endregion
	}
}
