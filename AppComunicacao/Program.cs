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
            int baudRate = 115200;

            // 1. Detecta automaticamente a porta COM
            string portaCOM = EncontrarPortaSTM32(baudRate);

            if (string.IsNullOrEmpty(portaCOM))
            {
                Console.WriteLine("[C# Erro] Nenhuma porta COM ativa foi encontrada. Verifique o cabo da STM32.");
                return;
            }

            SerialPort serialPort = new SerialPort(portaCOM, baudRate);

            try
            {
                serialPort.Open();
                Console.WriteLine($"[C# LPR] Conectado com sucesso na porta {portaCOM} a {baudRate} baud.");
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

        /// <summary>
        /// Varre todas as portas COM disponíveis e retorna a primeira válida.
        /// </summary>
        private static string EncontrarPortaSTM32(int baudRate)
        {
            string[] portasDisponiveis = SerialPort.GetPortNames();

            if (portasDisponiveis.Length == 0)
            {
                return null;
            }

            Console.WriteLine($"[C# LPR] Portas encontradas: {string.Join(", ", portasDisponiveis)}");

            foreach (string porta in portasDisponiveis)
            {
                try
                {
                    using (SerialPort testePorta = new SerialPort(porta, baudRate))
                    {
                        // Define um tempo limite curto para não travar a busca
                        testePorta.ReadTimeout = 1500;
                        testePorta.Open();

                        // Tenta ler uma linha para ver se é a STM32 transmitindo
                        string leituraTeste = testePorta.ReadLine();

                        if (!string.IsNullOrEmpty(leituraTeste))
                        {
                            Console.WriteLine($"[C# LPR] STM32 identificada na porta: {porta}");
                            return porta;
                        }
                    }
                }
                catch
                {
                    // Se a porta estiver ocupada ou não responder a tempo, ignora e testa a próxima
                    continue;
                }
            }

            // Se nenhuma respondeu a tempo mas existe apenas 1 porta conectada no PC, assume ela por padrão
            if (portasDisponiveis.Length == 1)
            {
                Console.WriteLine($"[C# LPR] Assumindo única porta disponível: {portasDisponiveis[0]}");
                return portasDisponiveis[0];
            }

            return null;
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