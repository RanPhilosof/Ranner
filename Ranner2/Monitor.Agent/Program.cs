using AppMonitoring.SharedTypes;
using CommandLine;
using Monitor.Agent.Services;
using Monitor.Infra;
using Monitor.SharedTypes;
using Serilog;
using ILogger = Serilog.ILogger;

if (!DebugerChecker.IsDebug && Environment.UserInteractive)
{
    WindowsServiceHandler.Stop("Monitor_Agent");
    WindowsServiceHandler.Delete("Monitor_Agent");
    WindowsServiceHandler.Create("Monitor_Agent", System.Environment.ProcessPath);
    WindowsServiceHandler.Start("Monitor_Agent");

    return;
}

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var parsedArgs = Parser.Default.ParseArguments<MonitorAgentOptions>(Environment.GetCommandLineArgs());

var loggerFilePath = "logs/agent.log";
if (!string.IsNullOrEmpty(parsedArgs?.Value?.LoggerFilePath))
	loggerFilePath = parsedArgs.Value.LoggerFilePath;

Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
                                          .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                                          .WriteTo.Console()
                                          .WriteTo.File(
												  loggerFilePath,
                                                  rollingInterval:RollingInterval.Infinite,
                                                  fileSizeLimitBytes:1024*1024*100,
                                                  retainedFileCountLimit:5,
                                                  rollOnFileSizeLimit: true, // start new file when limit reached
                                                  shared: false,
                                                  flushToDiskInterval: TimeSpan.FromSeconds(5)
                                                  ).CreateLogger();

#if (RELEASE)
{
var defaultPort = 4231;
if (parsedArgs?.Value?.Port != null)
    if (int.TryParse(parsedArgs?.Value?.Port, out int port))
        defaultPort = port;

builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(defaultPort));
}
#endif


//use serilog
builder.Host.UseSerilog();
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddSingleton(logger);
builder.Services.AddSingleton<IMonitorAgentService, VmService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// this is here to call the service constructor on startup.
_ = app.Services.GetService<IMonitorAgentService>();

app.Run();
