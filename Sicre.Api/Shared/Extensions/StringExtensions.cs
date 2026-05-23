namespace Sicre.Api.Shared.Extensions;

public static class StringExtensions
{
    public static string Mask(this string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return email;

        var parts = email.Split('@');
        var local = parts[0];
        var masked = local.Length <= 2 ? local : local[..2] + new string('*', local.Length - 2);
        return $"{masked}@{parts[1]}";
    }
}
