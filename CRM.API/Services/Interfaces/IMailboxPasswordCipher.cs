namespace CRM.API.Services.Interfaces;

public interface IMailboxPasswordCipher
{
    short CurrentVersion { get; }
    string Encrypt(string plainText);
    string Decrypt(string cipherText, short cryptoVersion = 1);
}
