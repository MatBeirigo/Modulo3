using Core.DTO;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi;

[ApiController]
[Route("api/module6")]
[Produces("application/json")]
[Tags("Módulo 6 - Relés")]
public class Module6Controller : ControllerBase
{
    private readonly DataAggregationService _aggregationService;
    private readonly Module6CommandService _commandService;
    private readonly ILogger<Module6Controller> _logger;

    public Module6Controller(
        DataAggregationService aggregationService,
        Module6CommandService commandService,
        ILogger<Module6Controller> logger)
    {
        _aggregationService = aggregationService;
        _commandService = commandService;
        _logger = logger;
    }

    [HttpGet("modules")]
    [ProducesResponseType(typeof(List<Module6StatusDto>), StatusCodes.Status200OK)]
    public ActionResult<List<Module6StatusDto>> GetConfiguredModules()
    {
        var modules = _aggregationService.GetConfiguredModules();
        return Ok(modules);
    }

    [HttpGet("unconfigured")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public ActionResult<List<string>> GetUnconfiguredModules()
    {
        var modules = _aggregationService.GetUnconfiguredModules();
        return Ok(modules);
    }

    [HttpPost("configure")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ConfigureModule([FromBody] ConfigureModuleRequest request)
    {
        if (request.NewId <= 0 || request.NewId > 99)
            return BadRequest(new
            {
                Message = "NewId deve estar entre 01 e 99.",
                Detail = "O ID 00 é reservado para broadcast interno do protocolo e não pode ser atribuído a um módulo físico."
            });

        if (string.IsNullOrWhiteSpace(request.UniqueId) || request.UniqueId.Length != 12)
            return BadRequest(new { Message = "UniqueId deve ter exatamente 12 caracteres." });

        var success = await _commandService.SendConfigureIdAsync(request.NewId, request.UniqueId);

        if (!success)
            return StatusCode(StatusCodes.Status502BadGateway,
                new { Message = "Falha ao enviar configuração TCP para o Módulo 6." });

        return Ok(new { Message = $"ID {request.NewId:D2} configurado para UniqueID {request.UniqueId}." });
    }

    [HttpPost("{moduleId:int}/relay/close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> CloseRelay(int moduleId)
    {
        var success = await _commandService.SendRelayCommandAsync(moduleId, Module6Command.CloseRelay);

        if (!success)
            return StatusCode(StatusCodes.Status502BadGateway,
                new { Message = $"Falha ao fechar relé do módulo {moduleId:D2}." });

        return Ok(new { Message = $"Comando fechar relé enviado para módulo {moduleId:D2}." });
    }

    [HttpPost("{moduleId:int}/relay/open")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> OpenRelay(int moduleId)
    {
        var success = await _commandService.SendRelayCommandAsync(moduleId, Module6Command.OpenRelay);

        if (!success)
            return StatusCode(StatusCodes.Status502BadGateway,
                new { Message = $"Falha ao abrir relé do módulo {moduleId:D2}." });

        return Ok(new { Message = $"Comando abrir relé enviado para módulo {moduleId:D2}." });
    }

    [HttpGet("{moduleId:int}/relay/state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetRelayState(int moduleId)
    {
        var success = await _commandService.SendRelayCommandAsync(moduleId, Module6Command.CheckState);

        if (!success)
            return StatusCode(StatusCodes.Status502BadGateway,
                new { Message = $"Falha ao consultar estado do módulo {moduleId:D2}." });

        return Ok(new { Message = $"Consulta de estado enviada para módulo {moduleId:D2}. Resposta chegará via SignalR." });
    }
}

public record ConfigureModuleRequest(int NewId, string UniqueId);