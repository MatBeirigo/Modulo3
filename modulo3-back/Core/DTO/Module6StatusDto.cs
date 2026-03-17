namespace Core.DTO;

public record Module6StatusDto(
    int ModuleId,
    string UniqueId,
    string RelayState,
    DateTime LastUpdate,
    bool IsOnline
);