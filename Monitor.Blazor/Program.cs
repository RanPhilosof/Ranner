using CommandLine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Monitor.Blazor.Interfaces;
using Monitor.Blazor.Services;
using Monitor.Services;
using MudBlazor.Services;
using Serilog;

var parsedArgs = Parser.Default.ParseArguments<BlazorOptions>(Environment.GetCommandLineArgs());

var loggerFilePath = "logs/ui.log";
if (!string.IsNullOrEmpty(parsedArgs?.Value?.LoggerFilePath))
	loggerFilePath = parsedArgs.Value.LoggerFilePath;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
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
	var defaultPort = 5087;
	if (parsedArgs?.Value?.Port != null)
		if (int.TryParse(parsedArgs?.Value?.Port, out int port))
			defaultPort = port;

	builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(defaultPort));
}
#endif

//use serilog
builder.Host.UseSerilog();

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", builder =>
	{
		builder.AllowAnyOrigin()
			   .AllowAnyMethod()
			   .AllowAnyHeader();
	});
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddSingleton<IMonitorService, MonitorAgentService>();
builder.Services.AddSingleton<IMonitorAgentCommunicationLayer, MonitorAgentCommunicationLayer>();
builder.Services.AddSingleton<IConsoleRunnerService, ConsoleRunnerServier>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}
else
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Services.GetService<IMonitorService>();
app.Services.GetService<IMonitorAgentCommunicationLayer>();
app.Services.GetService<IConsoleRunnerService>();

app.MapGet("/api/Rest/KillAll",
	(IMonitorService monitorService) =>
	{
		monitorService.KillAll();
		return Results.Ok();
	}).WithName("KillAll").WithTags("Api v1");

app.MapGet("/api/Rest/StartAll",
	(IMonitorService monitorService) =>
	{
		monitorService.StartAll();
		return Results.Ok();
	}).WithName("StartAll").WithTags("Api v1");

app.MapGet("/api/Rest/GetAllCsProjs",
    (IMonitorService monitorService) =>
    {
        return Results.Ok(monitorService.GetAllCsprojs());
    }).WithName("GetAllCsProjs").WithTags("Api v1");

app.MapGet("/api/Rest/GetAllCsProjForBuild",
	(IMonitorService monitorService) =>
	{
		return Results.Ok(monitorService.GetAllCsprojForBuild());
	}).WithName("GetAllCsProjForBuild").WithTags("Api v1");

app.MapGet("/api/Rest/GetAllCsProjForPublish",
    (IMonitorService monitorService) =>
    {
        return Results.Ok(monitorService.GetAllCsProjForPublish());
    }).WithName("GetAllCsProjForPublish").WithTags("Api v1");

app.MapGet("/api/Rest/GetAllDeployedFoldersToCopy",
    (IMonitorService monitorService) =>
    {
        return Results.Ok(monitorService.GetAllDeployedFoldersToCopy());
    }).WithName("GetAllDeployedFolders").WithTags("Api v1");

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

//RestRunner.Run(args);
app.UseCors("AllowAll");

app.Run();
