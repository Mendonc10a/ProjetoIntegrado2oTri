using System;
using System.IO.Ports;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeitorSerial
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string nodeUrl = "http://localhost:3000/api/medicao";

        static void Main()
        {
            string? portaCOM = DetectarPortaAtivaSTM32();

            if (string.IsNullOrEmpty(portaCOM))
            {
                Console.WriteLine("\n[ERRO] Nenhuma porta COM ativa do STM32 foi encontrada!\n");
                return;
            }

            Console.WriteLine($"\n[C# Serial] Conectando na porta: {portaCOM}...");

            using (SerialPort serial = new SerialPort(portaCOM, 115200))
            {
                try
                {
                    serial.DtrEnable = true;
                    serial.RtsEnable = true;
                    serial.ReadTimeout = 2000;

                    serial.Open();
                    serial.DiscardInBuffer();
                    Console.WriteLine($"[C# Serial] Conectado em {portaCOM}! Aguardando leituras (ASCII)...\n");

                    while (true)
                    {
                        try
                        {
                            // Lê a linha de texto enviada pela STM32 até o '\n'
                            string linha = serial.ReadLine().Trim();

                            if (!string.IsNullOrEmpty(linha))
                            {
                                // Converte o texto recebido (ex: "3796") para número inteiro
                                if (int.TryParse(linha, out int valorLDR))
                                {
                                    Console.WriteLine($"[C# RX] Valor LDR Recebido: {valorLDR}");

                                    // Envia para o Node.js -> IA -> Front
                                    _ = EnviarParaNodeAsync(valorLDR);
                                }
                                else
                                {
                                    Console.WriteLine($"[C# Aviso] Dado recebido não é um número válido: '{linha}'");
                                }
                            }
                        }
                        catch (TimeoutException)
                        {
                            // Timeout normal caso a STM32 demore mais de 2s entre envios
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Erro Serial] Falha em {portaCOM}: {ex.Message}");
                }
            }
        }

        private static string? DetectarPortaAtivaSTM32()
        {
            string[] termosBusca = { "STMicroelectronics", "STLink", "ST-Link", "STM", "USB Serial" };

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%' AND Status = 'OK'"))
                using (var dispositivos = searcher.Get())
                {
                    foreach (var device in dispositivos)
                    {
                        string nome = device["Caption"]?.ToString() ?? "";

                        foreach (string termo in termosBusca)
                        {
                            if (nome.Contains(termo, StringComparison.OrdinalIgnoreCase))
                            {
                                int inicio = nome.LastIndexOf("(COM");
                                int fim = nome.LastIndexOf(")");

                                if (inicio != -1 && fim != -1 && fim > inicio)
                                {
                                    return nome.Substring(inicio + 1, fim - inicio - 1);
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            string[] portasSistema = SerialPort.GetPortNames();
            return portasSistema.Length > 0 ? portasSistema[portasSistema.Length - 1] : null;
        }

        private static async Task EnviarParaNodeAsync(int valor)
        {
            try
            {
                var payload = new { valor = valor };
                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(nodeUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[C# -> Node.js] Enviado com sucesso: {valor}");
                }
                else
                {
                    Console.WriteLine($"[C# -> Node.js Erro HTTP] Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[C# Erro HTTP] Não foi possível conectar ao Node.js: {ex.Message}");
            }
        }
    }
}