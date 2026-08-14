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
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string nodeUrl = "http://localhost:3000/api/medicao";

        static void Main()
        {
            // 1. Tenta identificar automaticamente a porta COM em que a STM32 está conectada
            string? portaCOM = EncontrarPortaSTM32(115200);

            // 2. Se nenhuma porta responder, avisa o usuário e encerra o programa
            if (string.IsNullOrEmpty(portaCOM))
            {
                Console.WriteLine("\n[AVISO] Nenhuma porta COM ativa com o protocolo STM32 foi encontrada!");
                Console.WriteLine("[AVISO] Verifique se o cabo USB está conectado e tente novamente.\n");
                return;
            }

            // 3. Inicia a comunicação na porta identificada
            using (SerialPort serial = new SerialPort(portaCOM, 115200))
            {
                try
                {
                    serial.Open();
                    Console.WriteLine($"\n[C# Serial] Conectado com sucesso em {portaCOM} (Hexadecimal)...");

                    while (true)
                    {
                        // Espera ter pelo menos 4 bytes no buffer
                        if (serial.BytesToRead >= 4)
                        {
                            // Procura o Byte de Início (0xFF)
                            if (serial.ReadByte() == 0xFF)
                            {
                                byte msb = (byte)serial.ReadByte();      // Parte Alta
                                byte lsb = (byte)serial.ReadByte();      // Parte Baixa
                                byte checksum = (byte)serial.ReadByte(); // Soma de validação

                                // Valida a integridade do pacote
                                if ((byte)(msb + lsb) == checksum)
                                {
                                    int valorLDR = (msb << 8) | lsb;
                                    Console.WriteLine($"[Hex RX] {msb:X2} {lsb:X2} | Valor LDR: {valorLDR}");
                                    
                                    _ = EnviarParaNodeAsync(valorLDR);
                                }
                                else
                                {
                                    Console.WriteLine("[Erro] Pacote corrompido (Checksum inválido).");
                                }
                            }
                        }

                        Task.Delay(10).Wait();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Erro Serial] Conexão perdida: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Varre todas as portas COM e testa qual delas responde no protocolo correto (0xFF).
        /// </summary>
        private static string? EncontrarPortaSTM32(int baudRate)
        {
            string[] portas = SerialPort.GetPortNames();

            if (portas.Length == 0)
            {
                return null; // Nenhuma porta física encontrada
            }

            Console.WriteLine($"[Busca] Portas encontradas: {string.Join(", ", portas)}");

            foreach (string porta in portas)
            {
                try
                {
                    using (SerialPort teste = new SerialPort(porta, baudRate))
                    {
                        teste.ReadTimeout = 1000; // Tempo limite para resposta
                        teste.Open();

                        // Verifica se há dados chegando na porta
                        if (teste.BytesToRead >= 4)
                        {
                            if (teste.ReadByte() == 0xFF)
                            {
                                Console.WriteLine($"[Busca] STM32 identificada na porta: {porta}");
                                return porta;
                            }
                        }
                    }
                }
                catch
                {
                    // Se a porta estiver ocupada por outro programa ou não responder, testa a próxima
                    continue;
                }
            }

            // Se encontrou apenas 1 porta COM no Windows, assume ela por padrão mesmo sem leitura prévia
            if (portas.Length == 1)
            {
                Console.WriteLine($"[Busca] Assumindo a única porta disponível: {portas[0]}");
                return portas[0];
            }

            return null;
        }

        private static async Task EnviarParaNodeAsync(int valor)
        {
            try
            {
                var payload = new { valor = valor };
                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await httpClient.PostAsync(nodeUrl, content);
                Console.WriteLine($"[Node.js] Enviado: {valor}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Erro HTTP] {ex.Message}");
            }
        }
    }
}