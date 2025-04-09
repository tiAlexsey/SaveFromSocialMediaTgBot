using PuppeteerSharp;

namespace SaveFromSocialMediaTgBot.Services;

public static class InstagramAuthService
{
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

        // Ввод данных
        await page.TypeAsync("input[name='username']", login);
        await page.TypeAsync("input[name='password']", password);

        // Нажатие кнопки входа
        await page.ClickAsync("button[type='submit']");
        // Ждем авторизацию
        await Task.Delay(10000);

        using var page2 = await browser.NewPageAsync();
        await page2.GoToAsync("https://www.instagram.com/", WaitUntilNavigation.Networkidle0);
        Cookies = await page2.GetCookiesAsync();
        return Cookies;
    }
}