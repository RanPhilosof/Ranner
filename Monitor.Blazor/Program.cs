using AppMonitoring.SharedTypes;
using Blazored.LocalStorage;
using CommandLine;
using Monitor.Blazor.Interfaces;
using Monitor.Blazor.Services;
using Monitor.Infra;
using Monitor.Infra.LogSink;
using Monitor.Services;
using Monitor.SharedTypes;
using MudBlazor.Services;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


if (!DebugerChecker.IsDebug && Environment.UserInteractive)
{
	WindowsServiceHandler.Stop("Monitor_Blazor");
    WindowsServiceHandler.Delete("Monitor_Blazor");
    WindowsServiceHandler.Create("Monitor_Blazor", System.Environment.ProcessPath);
    WindowsServiceHandler.Start("Monitor_Blazor");

    return;
}

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var parsedArgs = Parser.Default.ParseArguments<BlazorOptions>(Environment.GetCommandLineArgs());

var loggerFilePath = "logs/ui.log";
if (!string.IsNullOrEmpty(parsedArgs?.Value?.LoggerFilePath))
	loggerFilePath = parsedArgs.Value.LoggerFilePath;

var logQueue = new ConcurrentQueue<LogInfo>();

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
                                                  flushToDiskInterval: TimeSpan.FromSeconds(5))
										  .WriteTo.Sink(new ConcurrentQueueLogSink(logQueue, 10_000))
										  .CreateLogger();

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
builder.Host.UseWindowsService();

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
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddSingleton<IMonitorService, MonitorAgentService>();
builder.Services.AddSingleton<IMonitorAgentCommunicationLayer, MonitorAgentCommunicationLayer>();
builder.Services.AddSingleton<IConsoleRunnerService, ConsoleRunnerServier>();
builder.Services.AddSingleton<ILoggingService, LoggingService>();
builder.Services.AddSingleton(logQueue);


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
//else
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Services.GetService<IMonitorService>();
app.Services.GetService<IMonitorAgentCommunicationLayer>();
app.Services.GetService<IConsoleRunnerService>();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

//RestRunner.Run(args);
app.UseCors("AllowAll");

app.MapControllers();

app.Run();
