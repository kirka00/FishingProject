using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public class GameState
    {
        public Dictionary<string, Player> Players { get; set; }
        public List<Fish> Fish { get; set; }
        private Random rand = new Random();

        public GameState()
        {
            Players = new Dictionary<string, Player>();
            Fish = new List<Fish>();
            GenerateFish();
        }

        // Генерация рыбы
        public void GenerateFish()
        {
            for (int i = 0; i < 5; i++) // создаём 5 рыб
            {
                Fish.Add(new Fish
                {
                    X = rand.Next(1, 49),
                    Y = rand.Next(1, 19),
                    Type = "Bass",
                    Speed = rand.Next(1, 3) // скорость рыбы от 1 до 2
                });
            }
        }

        // Обновление состояния игры: движение рыбы и проверка на ловлю
        public void Update()
        {
            foreach (var fish in Fish)
            {
                fish.Move();
            }

            foreach (var player in Players.Values)
            {
                Fish fishCaught = Fish.FirstOrDefault(f => f.X == player.X && f.Y == player.Y);
                if (fishCaught != null && player.IsReeling)
                {
                    player.CaughtFish++;
                    Fish.Remove(fishCaught); // Удаляем пойманную рыбу
                    Console.WriteLine($"Игрок {player.PlayerId} поймал рыбу {fishCaught.Type}!");
                }
            }
        }
    }

    public class Player
    {
        public string PlayerId { get; set; }
        public int CaughtFish { get; set; }
        public int X { get; set; } // Позиция игрока по оси X
        public int Y { get; set; } // Позиция игрока по оси Y
        public bool IsReeling { get; set; } // Признак, что игрок пытается выудить рыбу

        public Player(string playerId)
        {
            PlayerId = playerId;
            CaughtFish = 0;
            X = 10; // начальная позиция игрока
            Y = 10; // начальная позиция игрока
        }

        // Логика ловли рыбы
        public void CatchFish(string action)
        {
            if (action == "Cast")
            {
                Console.WriteLine($"{PlayerId} закинул удочку!");
            }
            else if (action == "ReelIn")
            {
                IsReeling = true;
                Console.WriteLine($"{PlayerId} тянет рыбу!");
            }
        }

        // Логика движения игрока
        public void Move(string direction)
        {
            switch (direction)
            {
                case "up":
                    if (Y > 0) Y--;
                    break;
                case "down":
                    if (Y < 18) Y++;
                    break;
                case "left":
                    if (X > 0) X--;
                    break;
                case "right":
                    if (X < 48) X++;
                    break;
            }
        }
    }

    public class Fish
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Type { get; set; }
        public int Speed { get; set; } // Скорость рыбы

        // Движение рыбы
        public void Move()
        {
            Random rand = new Random();
            int moveDirection = rand.Next(0, 2); // 0 - по X, 1 - по Y

            if (moveDirection == 0) // Двигаем по оси X
            {
                X += rand.Next(-Speed, Speed + 1); // Случайное движение влево или вправо
            }
            else // Двигаем по оси Y
            {
                Y += rand.Next(-Speed, Speed + 1); // Случайное движение вверх или вниз
            }

            // Ограничиваем позиции рыбы
            if (X < 0) X = 0;
            if (X > 48) X = 48;
            if (Y < 0) Y = 0;
            if (Y > 18) Y = 18;
        }
    }
    public class Message
    {
        public string UserName { get; set; }
        public string Text { get; set; }
    }
}