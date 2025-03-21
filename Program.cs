using CefSharp.OffScreen;
using CefSharp;

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

        app.Run();
        Cef.Shutdown();
    }
}