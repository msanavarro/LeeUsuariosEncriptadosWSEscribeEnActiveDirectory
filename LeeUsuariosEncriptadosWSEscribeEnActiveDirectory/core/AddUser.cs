using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;
using System.Linq;
using System.Threading.Tasks;
using System.Security;
using System.Runtime.InteropServices;
using System.Net;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
using System.Configuration;
using LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.loop;

namespace LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.core
{
    class AddUser
    {
        public static string ChangePassword20(string adminUser, string adminPassword, string path, string userName, string newPassword)
        {
            const AuthenticationTypes authenticationTypes = AuthenticationTypes.Secure |
                AuthenticationTypes.Sealing | AuthenticationTypes.ServerBind;

            DirectoryEntry searchRoot = null;
            DirectorySearcher searcher = null;
            DirectoryEntry userEntry = null;

            try
            {
                searchRoot = new DirectoryEntry(path,
                    adminUser, adminPassword, authenticationTypes);
                Console.WriteLine("Se crea search root");
                searcher = new DirectorySearcher(searchRoot);
                searcher.Filter = String.Format("sAMAccountName={0}", userName);
                searcher.SearchScope = SearchScope.Subtree;
                searcher.CacheResults = false;
                Console.WriteLine("Se cargan parametros de busqueda");
                SearchResult searchResult = searcher.FindOne();
                Console.WriteLine("Se busca resultado");
                if (searchResult == null) return "User Not Found In This Domain";

                userEntry = searchResult.GetDirectoryEntry();
                Console.WriteLine("Se asigna usuario encontrado");
                userEntry.Path = userEntry.Path.Replace(":389", "");
                Console.WriteLine(String.Format("sAMAccountName={0}, User={1}, path={2}", userEntry.Properties["sAMAccountName"].Value, userEntry.Username, userEntry.Path));
                userEntry.Invoke("SetPassword", new object[] { newPassword });
                userEntry.Properties["userAccountControl"].Value = 0x0200 | 0x10000;
                userEntry.CommitChanges();
                Console.WriteLine("Se ha cambiado la contraseña");
                return "New password set";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            finally
            {
                if (userEntry != null) userEntry.Dispose();
                if (searcher != null) searcher.Dispose();
                if (searchRoot != null) searchRoot.Dispose();
            }
        }

        public static bool CreateUserAccount(string ldapPath, string adminUsername, string adminPassword, string userName, string userPassword)
        {
            bool pudoAgregar = false;
            try
            {
                string connectionPrefix = "LDAP://" + ldapPath;
                DirectoryEntry dirEntry = new DirectoryEntry(connectionPrefix);
                dirEntry.Username = adminUsername;
                dirEntry.Password = adminPassword;
                DirectoryEntry newUser = dirEntry.Children.Add("CN=" + userName, "User");
                newUser.Properties["sAMAccountName"].Value = userName;
                newUser.CommitChanges();
                int val = (int)newUser.Properties["userAccountControl"].Value;
                newUser.CommitChanges();
                Console.WriteLine("Password: " + userPassword + "User: " + newUser.Path);
                newUser.Close();
                dirEntry.Close();
                Console.WriteLine("El usuario se ha creado exitosamente");
                Console.WriteLine(ChangePassword20(adminUsername, adminPassword, connectionPrefix, userName, userPassword));
                pudoAgregar = true;
            }
            catch (Exception e)
            {
                if (e.HResult != -2147019886)
                {
                    LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.ErrorFormat("Error en CreateUserAccount para el usuario {0}. El error fue: {1}", userName, e.Message);
                }
            }
            return pudoAgregar;
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an AesManaged object
            // with the specified key and IV.
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an AesManaged object
            // with the specified key and IV.
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }

        // Returns JSON string
        public static string getJson(string url)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            try
            {
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.ErrorFormat("Error en getJson, con código {0} y mensaje {1}", ex.HResult, ex.Message);
                throw;
            }
        }

        public static System.Collections.Generic.IDictionary<string, Newtonsoft.Json.Linq.JToken> getJSONObject(string json)
        {
            object obj = null;
            try
            {
                obj = JsonConvert.DeserializeObject(json);

            }
            catch (Exception e)
            {
                LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.ErrorFormat("Error en getJSONObject, con código {0} y mensaje {1}", e.HResult, e.Message);
                return null;
            }

            return (System.Collections.Generic.IDictionary<string, Newtonsoft.Json.Linq.JToken>)obj;
        }

        public static bool getJSONString(object obj, out string jsonOutput)
        {
            bool result = true;
            try
            {
                jsonOutput = JsonConvert.SerializeObject(obj);

            }
            catch (Exception e)
            {
                result = false;
                jsonOutput = "{}";
                LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.ErrorFormat("Error en getJSONString, con código {0} y mensaje {1}", e.HResult, e.Message);
            }
            return result;
        }

        public void Execute()
        {
            LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.InfoFormat("Se ejecutó el programa");
            String LDAP = LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Properties.Settings.Default.LDAP;
            String adminUsername = LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Properties.Settings.Default.adminUsername;
            String adminPassword = LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Properties.Settings.Default.adminPassword;

            try
            {

                using (AesManaged myAes = new AesManaged())
                {

                    string secureID = LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Properties.Settings.Default.secureID;
                    string securePassword = LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Properties.Settings.Default.securePassword;
                    string llave = LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Properties.Settings.Default.key;
                    string vi = LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Properties.Settings.Default.iv;

                    myAes.Key = Encoding.UTF8.GetBytes(llave);
                    myAes.IV = Encoding.UTF8.GetBytes(vi);

                    // Encrypt the string to an array of bytes.
                    byte[] secureIDEncr = EncryptStringToBytes_Aes(secureID, myAes.Key, myAes.IV);
                    byte[] securePasswordEncr = EncryptStringToBytes_Aes(securePassword, myAes.Key, myAes.IV);
                    string stringsecureIDEncr = BitConverter.ToString(secureIDEncr).Replace("-", "");
                    string stringsecurePasswordEncr = BitConverter.ToString(securePasswordEncr).Replace("-", "");

                    string urlwsIse = "https://webservices.buap.mx/wsIse.asmx/ObtenerDatos?secureID=" + stringsecureIDEncr + "&securePassword=" + stringsecurePasswordEncr;

                    string json = getJson(urlwsIse);

                    System.Collections.Generic.IDictionary<string, Newtonsoft.Json.Linq.JToken> JSONObj = null;
                    JSONObj = getJSONObject(json);

                    int contador = 1;
                    int usuariosAgregados = 0;
                    bool pudoAgregar = false;
                    if (JSONObj != null)
                    {
                        System.Collections.Generic.IDictionary<string, Newtonsoft.Json.Linq.JToken> JSONP =
                            (System.Collections.Generic.IDictionary<string, Newtonsoft.Json.Linq.JToken>)JSONObj;

                        Newtonsoft.Json.Linq.JToken token = JSONObj["secureResponse"]["WebServiceDt"].First;

                        while (token != null)
                        {
                            string idEncrString = (string)token["SWVACDI_ID"];
                            byte[] idEncrByte = StringToByteArray(idEncrString);
                            string idString = DecryptStringFromBytes_Aes(idEncrByte, myAes.Key, myAes.IV);
                            string nombreEncrString = (string)token["SWVACDI_NAME"];
                            byte[] nombreEncrByte = StringToByteArray(nombreEncrString);
                            string nombreString = DecryptStringFromBytes_Aes(nombreEncrByte, myAes.Key, myAes.IV);
                            string pwdEncrString = (string)token["SWVACDI_PIN"];
                            byte[] pwdEncrByte = StringToByteArray(pwdEncrString);
                            string pwdString = DecryptStringFromBytes_Aes(pwdEncrByte, myAes.Key, myAes.IV);
                            string tipoEncrString = (string)token["SWVACDI_TIPO_CODE"];
                            byte[] tipoEncrByte = StringToByteArray(tipoEncrString);
                            string tipoString = DecryptStringFromBytes_Aes(tipoEncrByte, myAes.Key, myAes.IV);
                            pudoAgregar = CreateUserAccount(LDAP, adminUsername, adminPassword, idString, pwdString);
                            if (pudoAgregar.Equals(true))
                            {
                                usuariosAgregados += 1;
                            }
                            contador += 1;
                            token = token.Next;
                        }
                        LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.Info("Se agregaron " + usuariosAgregados + " nuevos usuarios");
                    }
                }
            }
            catch (Exception e)
            {
                LeeUsuariosEncriptadosWSEscribeEnActiveDirectory.Program.log.ErrorFormat("Error en Main. El error fue {0} con código {1}", e.Message, e.HResult);
            }
        }

    }//AddUser
}
