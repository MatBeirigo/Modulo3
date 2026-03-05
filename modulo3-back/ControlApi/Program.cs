using Microsoft.OpenApi;
using Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Control API - Sistema de Monitoramento de Subestações",
        Version = "v1.0",
        Description = @"API para monitoramento em tempo real de módulos de controle de curtos em subestações de energia. 
                       
**Funcionalidades:**
- Recebe dados via broadcast TCP/UDP
- Monitoramento em tempo real de dispositivos IED/MU
- Gestão de eventos de proteção
- Relatórios agregados de eventos
- Controle de chaves e disjuntores

**Portas:**
- API HTTP: 5151
- API HTTPS: 7012
- TCP Broadcast: 5555
- UDP Broadcast: 5002
- Módulo 6 (Atuação): 5006",
        Contact = new OpenApiContact
        {
            Name = "Equipe de Desenvolvimento - Módulo 3",
            Email = "contato@exemplo.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    options.TagActionsBy(api =>
    {
        if (api.GroupName != null)
        {
            return new[] { api.GroupName };
        }

        if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
        {
            return new[] { controllerActionDescriptor.ControllerName };
        }

        return new[] { "Endpoints" };
    });

    options.DocInclusionPredicate((name, api) => true);
    options.OrderActionsBy(desc => desc.RelativePath);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<DataAggregationService>();
builder.Services.AddSingleton<CommandBroadcastService>();
builder.Services.AddHostedService<BroadcastReceiverService>();

var app = builder.Build();

app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        swagger.Servers = new List<OpenApiServer>
        {
            new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
        };
    });
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Control API v1.0");
    options.RoutePrefix = string.Empty;
    options.DocumentTitle = "Control API - Documentação Interativa";
    options.DefaultModelsExpandDepth(2);
    options.DefaultModelExpandDepth(2);
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
    options.EnableDeepLinking();
    options.DisplayOperationId();
    options.ShowExtensions();
    options.EnableFilter();
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

app.UseCors();

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Service = "Control API - Módulo 3",
    Version = "v1.0"
}))
.WithName("HealthCheck")
.WithTags("Sistema")
.Produces(200);

app.Run();