using CefSharp.OffScreen;
using CefSharp;
using ShopScoutWebApplication.Controllers;
using ShopScoutWebApplication;
using ShopScoutWebApplication.Models;

internal class Program
{
    private static void Main(string[] args)
    {
        CefSharp.BrowserSubprocess.SelfHost.Main((string[])args.Clone());         // инициализация Cef
        var settings = new CefSettings()
        {
            BrowserSubprocessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
            AcceptLanguageList = "ru-RU,ru;q=0.9",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36"
        };
        settings.DisableGpuAcceleration();
        Cef.Initialize(settings, performDependencyCheck: false);

        var builder = WebApplication.CreateBuilder(args);                         // инициализация самого приложения
        builder.Services.AddMemoryCache();
        builder.Services.AddControllersWithViews();
        builder.Services.AddSingleton<IProductSorter, ProductSorter>();
        builder.Services.AddTransient<IProductsDBController, EmptyProductsDBController>();
        builder.Services.AddSingleton<IParseController, ParseController>();

        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.Use(async (context, next) =>
        {
            await Task.Run(() =>
            {
                var logger = context.RequestServices.GetService<ILogger<Program>>();
                if (logger != null)
                    logger.LogInformation($"|{DateTime.Now}| Connection: RemoteIpAddress:{context.Connection.RemoteIpAddress}, RemotePort:{context.Connection.RemotePort}, LocalIpAddress:{context.Connection.LocalIpAddress}, LocalPort:{context.Connection.LocalPort}, Path:{context.Request.Path}");
            });
            await next.Invoke();
        });



        app.Run();
        Cef.Shutdown();
    }
}