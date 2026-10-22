using RWCustom;

namespace AbsoluteFriends.Utils;

public static class Translation
{
    public const string Placeholder = "<PLACEHOLDER>";

    public static string Of(string text)
    {
        try
        {
            return Custom.rainWorld?.inGameTranslator?.Translate(text) ?? text;
        }
        catch
        {
            return text;
        }
    }

    extension(string template)
    {
        public string FillPlaceholders(params object[] values)
        {
            foreach (var value in values)
            {
                int index = template.IndexOf(Placeholder, StringComparison.Ordinal);

                if (index < 0)
                {
                    break;
                }

                template = template.Remove(index, Placeholder.Length).Insert(index, value.ToString());
            }

            return template;
        }
    }
}
