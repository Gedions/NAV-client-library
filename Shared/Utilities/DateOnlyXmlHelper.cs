namespace Shared.Utilities
{
    public static class DateOnlyXmlHelper
    {
        public static string? Format(DateOnly? date) => date?.ToString("yyyy-MM-dd");

        public static DateOnly? Parse(string? value) =>
            DateOnly.TryParse(value, out var d) ? d : null;
    }
}
