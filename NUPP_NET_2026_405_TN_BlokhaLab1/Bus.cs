public class Bus : IEntity
{
    private static readonly string[] Models =
        { "Neoplan Skyliner", "Mercedes Tourismo", "MAN Lion's Coach", "Setra S 516", "Bogdan A092", "Ikarus 280" };

    public Guid Id { get; set; }
    public string Model { get; set; } = "";
    public string PlateNumber { get; set; } = "";
    public int Capacity { get; set; }
    public int ProductionYear { get; set; }
    public double Mileage { get; set; }
    public double FuelConsumption { get; set; }   // л/100 км
    public bool IsActive { get; set; }

    public static Bus CreateNew()
    {
        var r = Random.Shared;   // thread-safe
        return new Bus
        {
            Id = Guid.NewGuid(),
            Model = Models[r.Next(Models.Length)],
            PlateNumber = $"AA{r.Next(1000, 9999)}{(char)r.Next('A', 'Z' + 1)}{(char)r.Next('A', 'Z' + 1)}",
            Capacity = r.Next(20, 90),
            ProductionYear = r.Next(1995, 2025),
            Mileage = Math.Round(r.NextDouble() * 900_000, 1),
            FuelConsumption = Math.Round(18 + r.NextDouble() * 22, 2),
            IsActive = r.Next(2) == 1
        };
    }

    public override string ToString() =>
        $"{Model} [{PlateNumber}] {Capacity} місць, {ProductionYear} р., {Mileage:F0} км";
}
