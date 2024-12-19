using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common;

namespace FishingClient
{
    class Program
    {
        private static string userName;
        private static TcpClient client;
        private static NetworkStream stream;
        private static GameState gameState;

        static async Task Main(string[] args)
        {
            try
            {
                // Запрос имени пользователя
                Console.Write("Введите ваше имя: ");
                userName = Console.ReadLine();

                // Подключение к серверу
                client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", 6000);
                Console.WriteLine("Подключение к серверу...");
                stream = client.GetStream();

                // Отправка имени пользователя серверу
                Message initMessage = new Message { UserName = userName, Text = "Init" };
                string initJson = JsonSerializer.Serialize(initMessage);
                byte[] initData = Encoding.UTF8.GetBytes(initJson);
                await stream.WriteAsync(initData, 0, initData.Length);

                // Запуск задач для отправки и получения сообщений
                Task receiveTask = ReceiveGameStateAsync();
                Task sendTask = SendCommandsAsync();

                await Task.WhenAll(receiveTask, sendTask);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        private static async Task SendCommandsAsync()
        {
            Console.WriteLine("Используйте 'C' для закидывания удочки, 'R' для выуживания рыбы, WASD для движения.");
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    string action = "";

                    switch (key)
                    {
                        case ConsoleKey.C:
                            action = "Cast";
                            break;
                        case ConsoleKey.R:
                            action = "ReelIn";
                            break;
                        case ConsoleKey.W:
                            action = "up";
                            break;
                        case ConsoleKey.S:
                            action = "down";
                            break;
                        case ConsoleKey.A:
                            action = "left";
                            break;
                        case ConsoleKey.D:
                            action = "right";
                            break;
                        case ConsoleKey.Escape:
                            Console.WriteLine("Выход из игры...");
                            Environment.Exit(0);
                            break;
                    }

                    if (!string.IsNullOrEmpty(action))
                    {
                        Message message = new Message
                        {
                            UserName = userName,
                            Text = action
                        };
                        string jsonMessage = JsonSerializer.Serialize(message);
                        byte[] data = Encoding.UTF8.GetBytes(jsonMessage);
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }

                await Task.Delay(50); // Пауза для снижения нагрузки
            }
        }

        private static async Task ReceiveGameStateAsync()
        {
            byte[] buffer = new byte[4096];
            int byteCount;
            try
            {
                while ((byteCount = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string jsonMessage = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    gameState = JsonSerializer.Deserialize<GameState>(jsonMessage);
                    Console.Clear();
                    Console.WriteLine("Состояние игры:");

                    // Вывод состояния игры
                    Console.WriteLine($"Игроки: {gameState.Players.Count}");
                    Console.WriteLine($"Рыбы: {gameState.Fish.Count}");
                    foreach (var fish in gameState.Fish)
                    {
                        Console.WriteLine($"Рыба {fish.Type} на позиции ({fish.X}, {fish.Y})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных от сервера: {ex.Message}");
            }
        }
    }
}