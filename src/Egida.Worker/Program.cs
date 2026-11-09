using Egida.Application;
using Egida.Infrastructure;
using Egida.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AgregarInfraestructura(builder.Configuration);
builder.Services.AgregarClientesExternos(builder.Configuration);
builder.Services.AgregarCasosUsoWorker();

builder.Services.AddHostedService<ProcesadorEscaneos>();
builder.Services.AddHostedService<ReintentadorAnalisisIA>();
builder.Services.AddHostedService<EnviadorAlertas>();

builder.Build().Run();
