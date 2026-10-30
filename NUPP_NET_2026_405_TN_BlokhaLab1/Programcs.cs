using System.Diagnostics;
Console.OutputEncoding = System.Text.Encoding.UTF8;

const int Count = 1500;
var service = new CrudServiceAsync<Bus>(Path.Combine("data", "buses.json"));

// 1. Паралельне створення (Parallel + CreateNew + CRUD)
var sw = Stopwatch.StartNew();
await Parallel.ForEachAsync(
    Enumerable.Range(0, Count),
    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
    async (_, _) => await service.CreateAsync(Bus.CreateNew()));
sw.Stop();
Console.WriteLine($"Створено {service.Count()} об'єктів за {sw.ElapsedMilliseconds} мс " +
                  $"(потоків: {Environment.ProcessorCount})\n");

// 2. LINQ: min / max / avg
Console.WriteLine($"{"Поле",-18}{"Min",12}{"Max",12}{"Avg",12}");
void Stat(string name, Func<Bus, double> sel) =>
    Console.WriteLine($"{name,-18}{service.Min(sel),12:F2}{service.Max(sel),12:F2}{service.Average(sel),12:F2}");

Stat("Capacity", b => b.Capacity);
Stat("ProductionYear", b => b.ProductionYear);
Stat("Mileage", b => b.Mileage);
Stat("FuelConsumption", b => b.FuelConsumption);

// 3. Додатковий LINQ
Console.WriteLine("\nКількість за моделями:");
foreach (var g in service.GroupBy(b => b.Model).OrderByDescending(g => g.Count()))
    Console.WriteLine($"  {g.Key,-20}{g.Count(),5}   сер. пробіг {g.Average(b => b.Mileage):F0}");
Console.WriteLine($"\nАктивних автобусів: {service.Count(b => b.IsActive)}");

// 4. Пагінація
var page2 = await service.ReadAllAsync(page: 2, amount: 5);
Console.WriteLine("\nСторінка 2 (по 5):");
foreach (var b in page2) Console.WriteLine("  " + b);

// 5. CRUD
var first = (await service.ReadAllAsync(1, 1)).First();
first.Capacity = 100;
Console.WriteLine($"\nUpdate: {await service.UpdateAsync(first)}, " +
                  $"Read: {(await service.ReadAsync(first.Id)).Capacity} місць, " +
                  $"Remove: {await service.RemoveAsync(first)}");

// 6. Збереження
Console.WriteLine($"Save: {await service.SaveAsync()} -> {Path.GetFullPath(service.FilePath)}\n");

// 7. Примітиви синхронізації
await SyncDemo.RunAllAsync();
