namespace Core.Models;

public class BroadcastPacket
{
    public string Origin { get; set; }
    public long Sequence { get; set; }
    public string Module { get; set; }
    public string OperationType { get; set; }
    public string Data { get; set; }
    public DateTime Timestamp { get; set; }

    public static BroadcastPacket Parse(string rawPacket)
    {
        var parts = rawPacket.Split(';');
        if (parts.Length < 6)
            throw new ArgumentException("Pacote inválido");

        return new BroadcastPacket
        {
            Origin = parts[0],
            Sequence = long.Parse(parts[1]),
            Module = parts[2],
            OperationType = parts[3],
            Data = parts[4],
            Timestamp = DateTime.Parse(parts[5])
        };
    }

    public string ToPacketString()
    {
        return $"{Origin};{Sequence};{Module};{OperationType};{Data};{Timestamp:O}";
    }

    public static string CreatePacket(string origin, long sequence, string module, string operationType, string data)
    {
        var timestamp = DateTime.UtcNow;
        return $"{origin};{sequence};{module};{operationType};{data};{timestamp:O}";
    }
}