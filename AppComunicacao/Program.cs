using System;
using System.IO.Ports;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeitorSerial
{
    class Program
    {
        private static readonly string nodeApiUrl = "http://localhost:3000/api/medicao";
        private static readonly HttpClient httpClient = new HttpClient();

        static void Main(string[] args)
        {
            // Substitua 'COM3' pela porta serial em que o STM32 está conectado
            string portaCOM = "COM8"; 
            int baudRate = 115200;

            SerialPort serialPort = new SerialPort(portaCOM, baudRate);

            try
            {
                serialPort.Open();
                Console.WriteLine($"[C# LPR] Conectado na porta {portaCOM} a {baudRate} baud.");
                Console.WriteLine("[C# LPR] Aguardando dados do STM32... (CTRL+C para sair)");

                while (true)
                {
                    string linhaLida = serialPort.ReadLine().Trim();

                    if (!string.IsNullOrEmpty(linhaLida))
                    {
                        Console.WriteLine($"[C# Serial] Leitura: {linhaLida}");

                        if (double.TryParse(linhaLida, out double valorSensor))
                        {
                            _ = EnviarParaNodeAsync(valorSensor);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C# Erro] Falha na comunicação serial: {ex.Message}");
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }
        }

        private static async Task EnviarParaNodeAsync(double valor)
        {
            try
            {
                var payload = new { valor = valor };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(nodeApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[C# -> Node.js] Enviado: {valor}");
                }
                else
                {
                    Console.WriteLine($"[C# -> Node.js] Erro HTTP: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C# Erro HTTP] Falha ao conectar ao Node.js: {ex.Message}");
            }
        }
    }
}