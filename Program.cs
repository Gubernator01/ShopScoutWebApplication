using CefSharp.OffScreen;
using CefSharp;
using ShopScoutWebApplication.Controllers;
using ShopScoutWebApplication;

internal class Program
{
    private static void Main(string[] args)
    {
        CefSharp.BrowserSubprocess.SelfHost.Main((string[])args.Clone());         // инициализация Cef
        var settings = new CefSettings()
        {
            BrowserSubprocessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName
        };
        settings.DisableGpuAcceleration();
        Cef.Initialize(settings, performDependencyCheck: false);

        var builder = WebApplication.CreateBuilder(args);                         // инициализация самого приложения
        builder.Services.AddMemoryCache();
        builder.Services.AddControllersWithViews();

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
            var _ = Task.Run(() =>
            {
                var logger = context.RequestServices.GetService<ILogger<Program>>();
                if (logger != null)
                    logger.LogInformation($"Connection: RemoteIpAddress:{context.Connection.RemoteIpAddress}, RemotePort:{context.Connection.RemotePort}, LocalIpAddress:{context.Connection.LocalIpAddress}, LocalPort:{context.Connection.LocalPort}, Path:{context.Request.Path}");
            });
            await next.Invoke();
        });
        var test = new ParseController(LoggerFactory.Create(builder => builder.AddConsole()), new EmptyProductsDBController(LoggerFactory.Create(builder => builder.AddConsole())));
        var task = test.ParseAsync("хочу то, не знаю что", Sort.Popular, [MarketName.Ozon, MarketName.Wildberries]);
        task.Wait();
        foreach (var item in task.Result)
        {
            Console.WriteLine($"{item.MarketName}, {item.Name}");
        }

        app.Run();
        Cef.Shutdown();
    }
}