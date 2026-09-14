namespace TemplarCMS.Domain.Content;

public static class AuthoringLimits
{
    public const int Name = 255;
    public const int Icon = 255;
    public const int FieldKey = 450;
    public const int FileName = 255;
    public const int StoredFileName = 500;
    public const int ContentType = 255;
    public const int AltText = 500;
    public const int Title = 255;

    // String.Length counts UTF-16 units, matching the approved nvarchar bounds.
    public static void Check(string? value, int maximum, string parameter)
    {
        if (value?.Length > maximum)
            throw new ArgumentException($"{parameter} must not exceed {maximum} UTF-16 characters.", parameter);
    }
}
