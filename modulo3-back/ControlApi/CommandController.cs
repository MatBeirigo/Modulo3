using Microsoft.AspNetCore.Mvc;
using Services;
using Core.DTO;

namespace ControlApi;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Comandos")]
public class CommandController : ControllerBase
{
    private readonly CommandBroadcastService _commandService;
    private readonly DataAggregationService _aggregationService;
    private readonly ILogger<CommandController> _logger;

    public CommandController(
        CommandBroadcastService commandService,
        DataAggregationService aggregationService,
        ILogger<CommandController> logger)
    {
        _commandService = commandService;
        _aggregationService = aggregationService;
        _logger = logger;
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(CommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommandResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CommandResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CommandResponse>> SendCommand([FromBody] CommandRequest request)
    {
        if (string.IsNullOrEmpty(request.DeviceId) || string.IsNullOrEmpty(request.CommandType))
        {
            return BadRequest(new CommandResponse
            {
                Status = "ERROR",
                Message = "DeviceId e CommandType são obrigatórios"
            });
        }

        try
        {
            var commandId = await _commandService.SendCommand(request.DeviceId, request.CommandType, request.TargetState);

            return Ok(new CommandResponse
            {
                CommandId = commandId,
                Status = "SENT",
                IssuedAt = DateTime.UtcNow,
                Message = "Comando enviado com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar comando");
            return StatusCode(500, new CommandResponse
            {
                Status = "ERROR",
                Message = ex.Message
            });
        }
    }

    [HttpGet("status/{deviceId}")]
    [ProducesResponseType(typeof(Core.Models.SwitchCommand), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetCommandStatus(string deviceId)
    {
        var command = _aggregationService.GetCommandStatus(deviceId);

        if (command == null)
        {
            return NotFound(new { Message = "Nenhum comando pendente para este dispositivo" });
        }

        return Ok(command);
    }
}