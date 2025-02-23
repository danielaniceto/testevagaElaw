using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using testevagaElaw.Data;

class Program
{
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(3);
    private static readonly HttpClient Client = new HttpClient();
    private static int totalPages = 0;
    private static int totalRows = 0;

    static async Task Main()
    {
        using var db = new DatabaseContext();
        Console.WriteLine("Verificando conexão com o banco de dados...");
        db.VerificarConexao();

        var execution = new ProxyExecution()
        {
            StartTime = DateTime.Now,
        };
        db.Executions.Add(execution);
        await db.SaveChangesAsync();

        Console.WriteLine("Iniciando WebCrawler Multithreading...");

        List<Task<List<ProxyData>>> tasks = new List<Task<List<ProxyData>>>();
        int page = 1;

        while (true)
        {
            string url = $"https://proxyservers.pro/proxy/list/order/updated/order_dir/desc?page={page}";
            tasks.Add(ProcessPage(url, page));

            // não sobrecarregar o site
            await Task.Delay(1000);

            // Simular máximo de páginas (vamos melhorar depois)
            if (page >= 5) break;

            page++;
        }

        // Aguardar todas as tarefas finalizarem
        var results = await Task.WhenAll(tasks);
        List<ProxyData> allProxies = new List<ProxyData>();
        foreach (var result in results)
        {
            allProxies.AddRange(result);
        }

        // Salva no JSON
        string jsonPath = "proxies.json";
        File.WriteAllText(jsonPath, JsonConvert.SerializeObject(allProxies, Formatting.Indented));

        // Atualiza os dados da execução no banco
        execution.EndTime = DateTime.Now;
        execution.TotalPages = totalPages;
        execution.TotalProxies = totalRows;
        execution.JsonFile = jsonPath;
        await db.SaveChangesAsync();

        Console.WriteLine($"Extração finalizada! Total de {totalPages} páginas e {totalRows} proxies salvos em {jsonPath}");
    }

    static async Task<List<ProxyData>> ProcessPage(string url, int pageNumber)
    {
        await Semaphore.WaitAsync();
        List<ProxyData> proxies = new List<ProxyData>();

        try
        {
            Console.WriteLine($"Baixando página {pageNumber}...");

            HttpResponseMessage response = await Client.GetAsync(url);
            string html = await response.Content.ReadAsStringAsync();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//tr[contains(@class, 'proxy-row')]");
            if (rows != null)
            {
                Interlocked.Add(ref totalRows, rows.Count); // Contador de proxies extraídos
                foreach (var row in rows)
                {
                    var columns = row.SelectNodes("td");
                    if (columns != null && columns.Count >= 4)
                    {
                        proxies.Add(new ProxyData
                        {
                            IpAddress = columns[0].InnerText.Trim(),
                            Port = columns[1].InnerText.Trim(),
                            Country = columns[2].InnerText.Trim(),
                            Protocol = columns[3].InnerText.Trim()
                        });
                    }
                }
            }

            Interlocked.Increment(ref totalPages); // Contador de páginas processadas
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar página {pageNumber}: {ex.Message}");
        }
        finally
        {
            Semaphore.Release();
        }

        return proxies;
    }
}

class ProxyData
{
    public string IpAddress { get; set; }
    public string Port { get; set; }
    public string Country { get; set; }
    public string Protocol { get; set; }
}