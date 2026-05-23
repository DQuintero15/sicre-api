using System.Security.Cryptography;

namespace Sicre.Api.Shared;

public interface IPasswordService
{
    string GenerateSecurePassword(int length = 15);
}

public class PasswordService : IPasswordService
{
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string Special = "_-@#$!";

    public string GenerateSecurePassword(int length = 15)
    {
        if (length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");

        var chars = new List<char>
        {
            Pick(Uppercase),
            Pick(Lowercase),
            Pick(Digits),
            Pick(Special),
        };

        var all = Uppercase + Lowercase + Digits + Special;
        while (chars.Count < length)
            chars.Add(Pick(all));

        return new string(Shuffle(chars));
    }

    private static char Pick(string source) =>
        source[RandomNumberGenerator.GetInt32(source.Length)];

    private static char[] Shuffle(List<char> list)
    {
        var arr = list.ToArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }
}
