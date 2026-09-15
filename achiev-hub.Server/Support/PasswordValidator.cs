using System.Text.RegularExpressions;

namespace achiev_hub.Server.Support;

public static class PasswordValidator
{
    public const int MinLength = 12;
    private static readonly Regex Uppercase = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex Digit = new("[0-9]", RegexOptions.Compiled);
    private static readonly Regex Special = new(@"[!@#$%^&*(),.?"":{}|<>]", RegexOptions.Compiled);

    public static bool IsValid(string? password, out string error)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            error = $"Password must be at least {MinLength} characters.";
            return false;
        }

        if (!Uppercase.IsMatch(password))
        {
            error = "Password must include at least one uppercase letter.";
            return false;
        }

        if (!Digit.IsMatch(password))
        {
            error = "Password must include at least one number.";
            return false;
        }

        if (!Special.IsMatch(password))
        {
            error = "Password must include at least one special character.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
