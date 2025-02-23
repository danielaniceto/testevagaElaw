using System;
using Microsoft.EntityFrameworkCore;

namespace testevagaElaw.Data
{
    public class ProxyExecution
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; } 
        public int TotalPages { get; set; }
        public int TotalProxies { get; set; }
        public string JsonFile { get; set; }
    }

    public class DatabaseContext : DbContext
    {
        public DbSet<ProxyExecution> Executions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=database.db");
        }

        // verificar conexão com o banco de dados
        public void VerificarConexao()
        {
            try
            {
                this.Database.OpenConnection(); // Tentando abrir a conexão
                Console.WriteLine("Conexão com o banco de dados estabelecida com sucesso!");
                this.Database.CloseConnection(); // Fechando a conexão
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao conectar ao banco de dados: {ex.Message}");
                throw;
            }
        }
    }
}