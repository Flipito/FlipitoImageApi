using FlipitoImageServer.Abstraction;
using FlipitoImageServer.Services;

static void LoadFileBackedEnvVars()
{
    foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
    {
        var key = e.Key?.ToString();
        var path = e.Value?.ToString();
        if (string.IsNullOrEmpty(key) || !key.EndsWith("__File", StringComparison.OrdinalIgnoreCase))
            continue;

        if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
        {
            var targetKey = key[..^"__File".Length]; // strip "__File"
            var value = System.IO.File.ReadAllText(path).Trim();
            Environment.SetEnvironmentVariable(targetKey, value);
        }
    }
}

LoadFileBackedEnvVars();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IBlobStorageService, BlobStorageService>(provider =>
{
    var connectionString = builder.Configuration["FlipitoBlob:BlobStorageConnection"] ?? throw new ArgumentNullException("Blob Storage connection string cannot be null.");
    var containerName = builder.Configuration["FlipitoBlob:ContainerName"] ?? throw new ArgumentNullException("Blob Storage container name cannot be null.");

    return new BlobStorageService(connectionString, containerName);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseStaticFiles();
app.MapControllers();

app.Run();
