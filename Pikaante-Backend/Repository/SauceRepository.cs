using Microsoft.EntityFrameworkCore;
using Pikaante_Backend.Data;
using Pikaante_Backend.Models;

namespace Pikaante_Backend.Repository;


public class SauceRepository(AppDbContext context) : ISauceRepository
{
    public async Task<List<Sauce>> GetAllAsync()
    {
        return await context.Sauces.ToListAsync();
    }

    public async Task<Sauce?> GetByIdAsync(int id)
    {
        return await context.Sauces.FindAsync(id);
    }

    public async Task AddAsync(Sauce sauce)
    {
        context.Sauces.Add(sauce);
        await context.SaveChangesAsync();   
    }

    public async Task UpdateAsync(Sauce sauce)
    {
        context.Sauces.Update(sauce);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Sauce sauce)
    {
        context.Sauces.Remove(sauce);
        await context.SaveChangesAsync();
    }
}
