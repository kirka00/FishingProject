using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Common;

namespace FishingServer
{
    class Program
    {
        private static TcpListener listener;
        private static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private static GameState gameState = new GameState();

        static async Task Main(string[] args)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, 6000);
                listener.Start();
                Console.WriteLine("Сервер запущен и ожидает подключений...");

                // Запуск игрового цикла
                _ = Task.Run(GameLoopAsync);

                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string userName = "";
            try
            {
                NetworkStream stream = client.GetStream();

                // Ожидание имени пользователя
                byte[] buffer = new byte[1024];
                int byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0)
                {
                    throw new Exception("Не получено имя пользователя.");
                }

                string initialMessage = Encoding.UTF8.GetString(buffer, 0, byteCount);
                Message initMsg = JsonSerializer.Deserialize<Message>(initialMessage);
                userName = initMsg.UserName;

                if (string.IsNullOrEmpty(userName))
                {
                    throw new Exception("Имя пользователя не может быть пустым.");
                }

                // Проверка уникальности имени пользователя
                if (clients.ContainsKey(userName))
                {
                    Console.WriteLine($"Имя пользователя {userName} уже занято.");
                    // Отправьте клиенту сообщение об ошибке и закройте соединение
                    Message errorMsg = new Message { UserName = "Server", Text = "Имя пользователя уже занято." };
                    string errorJson = JsonSerializer.Serialize(errorMsg);
                    byte[] errorData = Encoding.UTF8.GetBytes(errorJson);
                    await stream.WriteAsync(errorData, 0, errorData.Length);
                    client.Close();
                    return;
                }

                clients.Add(userName, client);
                gameState.Players.Add(userName, new Player(userName));
                Console.WriteLine($"Новый игрок подключился: {userName}");

                // Ожидание дальнейших сообщений от клиента
                buffer = new byte[1024];
                while ((byteCount = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string jsonMessage = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    Message message = JsonSerializer.Deserialize<Message>(jsonMessage);
                    if (message != null && message.UserName == userName)
                    {
                        // Обновление действия игрока (ловля рыбы)
                        if (IsValidAction(message.Text))
                        {
                            gameState.Players[userName].CatchFish(message.Text);
                            Console.WriteLine($"Игрок {userName} попробовал поймать рыбу с {message.Text}");
                        }
                        else
                        {
                            Console.WriteLine($"Игрок {userName} отправил неверное действие: {message.Text}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Игрок {userName} отключился: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    clients.Remove(userName);
                    gameState.Players.Remove(userName);
                    Console.WriteLine($"Игрок отключился: {userName}");
                }
                client.Close();
            }
        }

        private static bool IsValidAction(string action)
        {
            return action == "Cast" || action == "ReelIn"; // Ловля рыбы
        }

        private static async Task GameLoopAsync()
        {
            while (true)
            {
                // Обновляем состояние игры
                gameState.Update();

                // Сериализация GameState
                string gameStateJson = JsonSerializer.Serialize(gameState);
                byte[] gameStateData = Encoding.UTF8.GetBytes(gameStateJson);

                // Логирование состояния игры
                Console.WriteLine($"Отправка состояния игры: Рыбы ({gameState.Fish.Count})");

                // Отправка состояния игры всем клиентам
                Console.WriteLine("Отправка состояния игры клиентам.");
                List<string> disconnectedClients = new List<string>();

                foreach (var kvp in clients)
                {
                    try
                    {
                        NetworkStream stream = kvp.Value.GetStream();
                        await stream.WriteAsync(gameStateData, 0, gameStateData.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при отправке данных клиенту {kvp.Key}: {ex.Message}");
                        disconnectedClients.Add(kvp.Key);
                    }
                }

                // Удаление отключённых клиентов
                foreach (var playerId in disconnectedClients)
                {
                    if (clients.ContainsKey(playerId))
                    {
                        clients[playerId].Close();
                        clients.Remove(playerId);
                        gameState.Players.Remove(playerId);
                        Console.WriteLine($"Игрок удалён из-за ошибки: {playerId}");
                    }
                }

                await Task.Delay(200); // Задержка
            }
        }
    }
}