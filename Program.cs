using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorApp.Data;
using Microsoft.AspNetCore.ResponseCompression;
using BlazorApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

//retrieve port from environment variables
var port = Environment.GetEnvironmentVariable("PORT");

//set listening urls
builder.WebHost.UseUrls($"http://0.0.0.0:{port};http://localhost:3000");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddResponseCompression(opts =>
{
  opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
      new[] { "application/octet-stream" });
});

builder.Services.AddSignalR(o =>
{
  o.EnableDetailedErrors = true;
});

builder.Services.AddCors(options =>
{
  options.AddPolicy("CorsPolicy",
      builder => builder
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .AllowAnyOrigin()
      );
});

var app = builder.Build();

app.UseResponseCompression();
// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.UseCors("CorsPolicy");
app.MapHub<ClientHub>("/clienthub");
app.MapFallbackToPage("/_Host");

app.Run();