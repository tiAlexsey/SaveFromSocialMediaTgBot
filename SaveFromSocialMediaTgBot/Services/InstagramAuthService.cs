using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace SaveFromSocialMediaTgBot.Services;

public static class InstagramAuthService
{
    private static readonly Random _random = new();
    private static readonly LaunchOptions _launchOptions = new()
    {
        Headless = true,
        ExecutablePath = "/usr/bin/chromium",
        Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage"]
    };

    public static CookieParam[] Cookies { get; private set; } = { };

    public static async Task<CookieParam[]> UpdateCookies(string login, string password)
    {
        using var browser = await Puppeteer.LaunchAsync(_launchOptions);
        using var page = await browser.NewPageAsync();

        // Переход на страницу входа Instagram
        await page.GoToAsync("https://www.instagram.com/accounts/login/");

        // Ожидание появления полей ввода
        await page.WaitForSelectorAsync("input[name='username']");
        await page.WaitForSelectorAsync("input[name='password']");

        await Task.Delay(_random.Next(800, 1000));
        // Ввод данных
        await page.TypeAsync("input[name='username']", login, new TypeOptions { Delay = 150 });
        await Task.Delay(_random.Next(500, 1000));
        await page.TypeAsync("input[name='password']", password, new TypeOptions { Delay = 150 });

        // Нажатие кнопки входа
        await page.ClickAsync("button[type='submit']");
        // Ждем авторизацию
        await page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = [WaitUntilNavigation.Networkidle0] });

        using var page2 = await browser.NewPageAsync();
        await page2.GoToAsync("https://www.instagram.com/", WaitUntilNavigation.Networkidle0);
        Cookies = await page2.GetCookiesAsync();
        return Cookies;
    }
}