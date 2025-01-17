using FlipitoImageServer.Abstraction;
using FlipitoImageServer.Services;

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
