using System.Security;

public class SmtpSettings
{
    public string? Host { get; internal set; }
    public int Port { get; internal set; }
    public string? Username { get; internal set; }
    public SecureString? Password { get; internal set; }
    public bool EnableSsl { get; internal set; }
}