using HappyBoxx.Billing.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.Billing.Api.Infrastructure.Persistence;

internal static class BillingSeeder
{
    private static readonly (string Name, string Address)[] Customers =
    [
        ("TOP ASIAN PENINSULA", "272 Anzac Avenue, Kippa-Ring, 4021 (Peninsula Fair Shopping Centre, Food Court area)"),
        ("TOP ASIAN KAWANA", "Kawana shopping centre Food court, next to McDonald’s, Shop 410, 119 point Cartwright drive, buddina 4575"),
        ("VIET HAUS - BIG TOP SHOPPING CENTRE", "9/20 Ocean St, Maroochydore QLD 4558/ ABN: 55 666 854 048"),
        ("ANN'S RESTAURANT", "118-120 Brisbane Rd, Mooloolaba"),
        ("THAI LOTUS MOOLOOLABA", "Shop 2/20 Brisbane roads Mooloolaba"),
        ("BUDDINA EATING HOUSE", "1/21 nicklin way buddina, qld 4575"),
        ("THAI LOTUS - WURTULLA", "1/56 moodara dr, Wurtulla"),
        ("COOPER SPOON THAI", "75 Esplanade, Golden Beach QLD 4551"),
        ("CHINESE HOLIDAY RESTAURANT", "106 Bulcock St, Caloundra QLD 4551"),
        ("ALI BABA", "Sunshine Plaza/ ABN 21672467163"),
        ("FINN THAI RESTAURANT", "Shop 3/ 46 Landsborough parade, Golden beach 4551"),
        ("KHAO GAENG THAI RESTAURANT", "82-98 Anzac Ave, Redcliffe QLD 4020, ABN 32670862629"),
        ("CHARM RESTAURANT", "95 Prince Edward Parade Scarborough 4020"),
        ("THAI ANTIQUE RESTAURANT", "14 Bertini Lane, Carina Heights QLD"),
        ("LEMAK GARDEN CITY", "Westfield Garden City Shopping Centre"),
        ("MK CHINESE", "WESTFIELD SHOPPING CENTRE NORTH LAKES"),
    ];

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (context is not BillingDbContext dbContext)
        {
            return;
        }

        var existingByName = await dbContext.Customers
            .ToDictionaryAsync(customer => customer.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var item in Customers)
        {
            if (existingByName.ContainsKey(item.Name))
            {
                continue;
            }

            dbContext.Customers.Add(Customer.Create(item.Name, item.Address, "Unknown", null, null, null, "Other"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
