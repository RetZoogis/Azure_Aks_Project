using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

//TestSample of AKS Project
var env = Environment.GetEnvironmentVariable("api_env") ?? "local";
var auditrestapi = Environment.GetEnvironmentVariable("api_name") ?? "auditapi";

//KEYVAULT + AZURE APP CONFIGURATION SET UP
AzureAppConfiguration appsettings = new();
KeyVaultConnectionString keyvaultconnectionstring = new();
AzureAppSettings _azureappsettings = new ();

ConfigurationManager configuration = builder.Configuration;

builder.Host.ConfigureAppConfiguration((_, config) =>
{
    #region Manual Keyvault Config
    //Manual Keyvault Configuration - Connecting Directly to Azure
    // var keyvaultendpoint = builder.Configuration["Secrets:KevaultUriTest"];
    // AzureServiceTokenProvider azureServiceTokenProvider = null;
    // azureServiceTokenProvider = new AzureServiceTokenProvider("RunAs=developer; DeveloperTool=VisualStudio");
    // var keyVaultClient = new KeyVaultClient (
    //      new KeyVaultClient.AuthenticationCallBack (
    //          azureServiceTokenProvier.KeyVaultTokenCallBack));
    // config.AddAzureKeyvault (
    //  keyvaultendpoint, keyvaultclient, new defaultkeyvaultsecretmanager());
    // 

    #endregion
    if (env == "local")
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "mnt/secrets-store");
        config.AddKeyPerFile(path, optional: true, reloadOnChange: true);
    }
    else
    {
        config.AddKeyPerFile("/mnt/secrets-store", optional: true, reloadOnChange: true);
    }
    //Build KEYVAULT so we can use azureappconfiguration connection string
    builder.Services.Configure<KeyVaultConnectionString>(configuration.GetSection("connectionstrings"));
    keyvaultconnectionstring = _azureappsettings.getkeyvaultconfiguration(builder.Services);

    //AZURE APP SETTINGS configuration
    config.AddAzureAppConfiguration(options =>
    {
        //this inserts whatever you have in azureappconfig with filter (appconfiguration) into the config.providers
        options.Connect(keyvaultconnectionstring.azureappconfig).Select("AppConfiguration:*", $"{env}_{auditrestapi}")
                .ConfigureRefresh(refresh =>
                {

                });
    });

    var settings = config.Build();

}).ConfigureServices(services =>
{
    services.AddControllers();
});

//AZUREAPPCONFIGURATION to Class
builder.Services.Configure<AzureAppConfiguration>(configuration.GetSection($"AppConfiguration:Settings:{env}:{auditrestapi}"));
//initiating auzre app configuration in startup
builder.Services.AddAzureAppConfiguration();
appsettings = _azureappsettings.getazureappconfiguration(builder.Services);

////Add Azure Queue Service Configuration. StorageAccount coming from keyvault
//    //Azure service in this example is to set up for Dependency injection the azure operations such like  send to queue or send to blob
//var storageaccountcs = keyvaultconnectionstring.storageaccountconnectionstring;
//builder.Services.AddScoped<IAzureServices>(x => new AzureService(storageaccountcs));


//APPLICATION INSIGHTS Configuration

var appinsights = appsettings.appinsights;
var loglevel = appsettings.loglevel;
var levelswitch = new Serilog.Core.LoggingLevelSwitch
{
    MinimumLevel = (LogEventLevel)Enum.Parse(typeof(LogEventLevel), loglevel)
};

builder.Host.ConfigureLogging((hostincontext, config) =>
{
    config.ClearProviders();
})
    .UseSerilog((hostingContext, logger) =>
    logger
    .ReadFrom.Configuration(hostingContext.Configuration)
    .Enrich.WithProperty("Environment", env)
    .WriteTo.ApplicationInsights(appinsights, TelemetryConverter.Traces)
    .MinimumLevel.ControlledBy(levelswitch)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Fatal)
    ).ConfigureServices((_, services) =>
     services.AddSingleton(levelswitch));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public class AzureAppConfiguration
{
    public string? appinsights { get; set; }
    public string? loglevel { get; set; }
    public string? odsqueue { get; set; }
    public string? identityserveraddress { get; set; }  
    public string? apiqueue { get; set; }   
    public string? blobcontainer {  get; set; } 
}
public class KeyVaultConnectionString
{
    public string? azureappconfig { get; set; }
    public string? auditconnectionstring { get; set; }
    public string? storageaccountconnectionstring { get; set; }
    public string? appuserlog { get; set; }
    public string? appdiagnosticlog { get; set; }
}
public class AzureAppSettings
{
    public AzureAppConfiguration getazureappconfiguration (IServiceCollection builder)
    {
        var serviceprovider = builder.BuildServiceProvider();
        var azureappconfig = serviceprovider.GetService<IOptionsSnapshot<AzureAppConfiguration>> ();
        return azureappconfig.Value;
    }
    public KeyVaultConnectionString getkeyvaultconfiguration(IServiceCollection builder)
    {
        var serviceprovider = builder.BuildServiceProvider();
        var keyvaultcs = serviceprovider.GetService<IOptionsSnapshot<KeyVaultConnectionString>>();
        return keyvaultcs.Value;
    }
}