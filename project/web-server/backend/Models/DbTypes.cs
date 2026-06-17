namespace WebServer.Models;

public static class DbTypes
{
    // Postgres-safe alias for money columns
    public const string Money = "numeric(18,2)";
}
