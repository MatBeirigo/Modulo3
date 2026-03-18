namespace Core.Models;

public class Module6Packet
{
    public const string Module6Prefix = "#";
    public const string VisualizationPrefix = "!";
    public const int VisualizationModuleId = 99;

    public string RawPacket { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int RecipientId { get; set; }
    public Module6Command Command { get; set; }
    public string State { get; set; } = string.Empty;
    public string? RelayState { get; set; }
    public string? UniqueId { get; set; }
    public string? SourceIp { get; set; }
    public bool IsUnconfigured => Command == Module6Command.Unconfigured && UniqueId?.Length == 12;
    public bool IsRelayOpen => (RelayState ?? State) == "00";
    public bool IsRelayClosed => (RelayState ?? State) == "01";

    public static Module6Packet Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Pacote vazio ou nulo.");

        var prefix = raw[..1];
        if (prefix != Module6Prefix && prefix != VisualizationPrefix)
            throw new FormatException($"Prefixo inválido: '{prefix}'. Esperado '#' ou '!'.");

        var body = raw[1..];
        var parts = body.Split(';');

        if (parts.Length < 3)
            throw new FormatException($"Pacote com campos insuficientes: '{raw}'.");

        if (!int.TryParse(parts[0], out var recipientId))
            throw new FormatException($"ID inválido: '{parts[0]}'.");

        if (!int.TryParse(parts[1], out var commandInt) || !Enum.IsDefined(typeof(Module6Command), commandInt))
            throw new FormatException($"Comando inválido: '{parts[1]}'.");

        var command = (Module6Command)commandInt;
        var state = parts[2];

        string? relayState = null;
        string? uniqueId = null;

        if (parts.Length >= 4)
        {
            var field = parts[3];
            if (field.Length == 12)
                uniqueId = field;
            else if (field == "00" || field == "01")
                relayState = field;
        }

        return new Module6Packet
        {
            RawPacket = raw,
            Prefix = prefix,
            RecipientId = recipientId,
            Command = command,
            State = state,
            RelayState = relayState,
            UniqueId = uniqueId
        };
    }

    public static string CreateStateResponse(int recipientId, string relayState)
        => $"!{recipientId:D2};3;{relayState}";

    public static string CreateConfigPacket(int newId, string uniqueId)
    {
        if (newId <= 0 || newId > 99)
            throw new ArgumentOutOfRangeException(nameof(newId),
                $"ID inválido: {newId}. O ID deve estar entre 01 e 99. O ID 00 é reservado para broadcast.");

        return $"#00;9;{newId:D2};{uniqueId}";
    }

    public static string CreateCommandPacket(int recipientId, Module6Command command)
        => $"#{recipientId:D2};{(int)command};00";
}

public enum Module6Command
{
    Unconfigured = 0,
    CloseRelay = 1,
    OpenRelay = 2,
    CheckState = 3,
    ConfigureId = 9
}