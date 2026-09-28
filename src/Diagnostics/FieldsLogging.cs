namespace MmoGame3d.Diagnostics;

using Microsoft.Extensions.Logging;

public static class FieldsLogging
{
    private static readonly Func<Fields, Exception?, string> Format = FormatFields;

    public static void Write(this ILogger logger, LogLevel level, Fields fields, Exception? exception = null)
    {
        logger.Log(level, default, fields, exception, Format);
    }

    private static string FormatFields(Fields fields, Exception? exception)
    {
        return fields.Message;
    }
}
