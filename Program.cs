using System.IO;
using System;
using System.Threading.Tasks;
using PuppeteerSharp;
using System.Net;
using System.Linq;

namespace PatreonDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false // so you can see if the threading sleep needs to be increased
            });


            var page = await browser.NewPageAsync();
            await page.GoToAsync("https://www.patreon.com/login");

            var login = "your@email";
            var password = "your password";


            await page.TypeAsync("#email", login);
            await page.TypeAsync("#password", password);

            System.Threading.Thread.Sleep(1000);

            await page.ClickAsync("[type=\"submit\"]");
            await page.WaitForNavigationAsync();

            var result = true;
            System.Threading.Thread.Sleep(5000);
            while (result)
            {
                System.Console.WriteLine("Loading page");
                result = await LoadMore(page);
            }

            await Download(page);
        }

        public static async Task<bool> LoadMore(Page page)
        {
            var result = await page.EvaluateFunctionAsync<Boolean>(@"() => {
                window.scrollTo(0,document.body.scrollHeight);

                var buttons = document.querySelectorAll(""button"");

                var loadButton = Array.from(buttons).find(b => b.innerHTML.toLowerCase().indexOf(""load more"") > -1);

                if(!loadButton){
                    return false;
                }

                loadButton.click();

                return true;
            }");

            System.Threading.Thread.Sleep(5000);

            return result;
        }

        public static async Task Download(Page page)
        {
            await page.Client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", downloadPath = "./patreon-downloads" });

            var links = await page.EvaluateFunctionAsync<string[]>(@"() => {return Array.from(document.querySelectorAll(""[href*=file]"")).map(a => a.href)}");

            foreach (var link in links)
            {
                await page.EvaluateFunctionAsync($"() => {{document.querySelector('[href=\"{link}\"]').click()}}");
                System.Threading.Thread.Sleep(3000);
            }


            // TODO: download files directly with c# webclient
            // var links = await page.EvaluateFunctionAsync<string[]>(@"() => {return Array.from(document.querySelectorAll(""[href*=file]"")).map(a => a.href+"";""+a.innerHTML)}");

            // // Array.from(document.querySelectorAll(""[href*=file]"")).map(a => a.href);
            // var cookies = await page.GetCookiesAsync();

            // WebClient client = new WebClient();

            // string cookieString = string.Join(";", cookies.Select(cookie => $"${cookie.Name}=${cookie.Value}"));
            // client.Headers.Add(HttpRequestHeader.Cookie, cookieString);

            // for (int i = 1; i <= links.Length; i++)
            // {
            //     var values = links[i].Split(";");
            //     System.Console.WriteLine($"Downloading file {values[1]}");
            //     client.DownloadFile(values[0], $"./patreonDownloads/{values[1]}");
            // }
        }
    }
}
