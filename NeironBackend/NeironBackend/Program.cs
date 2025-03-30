using NeironBackend.Configurations;
using NeironBackend.Services;
using NeironBackend.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// FileShare
builder.Services.Configure<DownloadURL>(builder.Configuration.GetSection("FileShare"));

builder.Services.Configure<RabbitmqSettings>(builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddSingleton<IRabbitProducer, RabbitProducerService>();

builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddHostedService<RabbitConsumerService>();

string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}

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
