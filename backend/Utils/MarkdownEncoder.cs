namespace MagicPot.Backend.Utils
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    public static class MarkdownEncoder
    {
        private static readonly char[] MarkdownToEscape = ['\\', '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!'];

        [return: NotNullIfNotNull(nameof(source))]
        public static string? Escape(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return source;
            }

            if (source.IndexOfAny(MarkdownToEscape) == -1)
            {
                return source;
            }

            var sb = new StringBuilder(source.Length + 50);
            foreach (var c in source)
            {
                if (MarkdownToEscape.Contains(c))
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
