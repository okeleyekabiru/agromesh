using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly (string Name, string Code)[] NigerianStates =
    [
        ("Abia", "AB"), ("Adamawa", "AD"), ("Akwa Ibom", "AK"), ("Anambra", "AN"),
        ("Bauchi", "BA"), ("Bayelsa", "BY"), ("Benue", "BE"), ("Borno", "BO"),
        ("Cross River", "CR"), ("Delta", "DE"), ("Ebonyi", "EB"), ("Edo", "ED"),
        ("Ekiti", "EK"), ("Enugu", "EN"), ("Gombe", "GO"), ("Imo", "IM"),
        ("Jigawa", "JI"), ("Kaduna", "KD"), ("Kano", "KN"), ("Katsina", "KT"),
        ("Kebbi", "KE"), ("Kogi", "KO"), ("Kwara", "KW"), ("Lagos", "LA"),
        ("Nasarawa", "NA"), ("Niger", "NI"), ("Ogun", "OG"), ("Ondo", "ON"),
        ("Osun", "OS"), ("Oyo", "OY"), ("Plateau", "PL"), ("Rivers", "RI"),
        ("Sokoto", "SO"), ("Taraba", "TA"), ("Yobe", "YO"), ("Zamfara", "ZA"),
        ("Federal Capital Territory", "FC"),
    ];

    public static async Task SeedAsync(AgroMeshDbContext db)
    {
        if (await db.States.AnyAsync())
            return;

        var states = NigerianStates.Select(s => State.Create(s.Name, s.Code)).ToList();
        await db.States.AddRangeAsync(states);
        await db.SaveChangesAsync();
    }
}
