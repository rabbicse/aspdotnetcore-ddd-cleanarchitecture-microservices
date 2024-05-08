using Microsoft.AspNetCore.ResponseCompression;
using KYC.Application;
using KYC.Write.MsSql.Infrastructure;
using KYC.Read.Mongo.Infrastructure;
using KYC.EventStoreDB.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Compression
builder.Services.Configure<GzipCompressionProviderOptions>(compressionOptions => compressionOptions.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.AddResponseCompression(compressionOptions =>
 {
     compressionOptions.EnableForHttps = true;
     compressionOptions.Providers.Add<GzipCompressionProvider>();
 });
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add dependencies from Application layer
builder.Services.AddApplications();

// Add dependencies from Infra layer
builder.Services.AddWriteInfrastructureServices(builder.Configuration);
builder.Services.AddReadInfrastructureServices(builder.Configuration);
builder.Services.AddEventStoreInfrastructureServices(builder.Configuration);
//builder.Services.AddCacheInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();


app.UseResponseCompression();
app.UseHttpsRedirection();
//app.UseMiniProfiler();
//app.UseCorrelationId();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
