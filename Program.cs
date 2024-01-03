using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorApp.Data;
using Microsoft.AspNetCore.ResponseCompression;
using BlazorApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

//retrieve port from environment variables
var port = Environment.GetEnvironmentVariable("PORT");

//set listening urls
builder.WebHost.UseUrls($"http://localhost:3000");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddResponseCompression(opts => {
  opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
      new[] { "application/octet-stream" });
});

builder.Services.AddCors(o => o.AddPolicy("CorsPolicy", builder => {
  builder
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowAnyHeader();
}));

builder.Services.AddSignalR();

var app = builder.Build();

app.UseResponseCompression();
// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//}

app.UseCors("CorsPolicy");

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapHub<ClientHub>("/clienthub");
app.MapFallbackToPage("/_Host");

app.Run();