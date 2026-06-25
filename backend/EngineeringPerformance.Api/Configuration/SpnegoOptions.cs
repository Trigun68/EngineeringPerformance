namespace EngineeringPerformance.Api.Configuration;

/// <summary>
/// Настройки доверенного SPNEGO-прокси (Nginx auth_gss).
/// По умолчанию отключено — X-Remote-User игнорируется.
/// </summary>
public sealed class SpnegoOptions
{
    public const string SectionName = "Spnego";

    /// <summary>Заголовок, который прокси добавляет вместе с X-Remote-User.</summary>
    public const string TrustedProxyTokenHeader = "X-Trusted-Proxy-Token";

    /// <summary>Включить обработку X-Remote-User (только за доверенным прокси).</summary>
    public bool Enabled { get; set; }

    /// <summary>Общий секрет между Nginx и API; без совпадения заголовок X-Remote-User отбрасывается.</summary>
    public string TrustedProxySecret { get; set; } = string.Empty;
}
