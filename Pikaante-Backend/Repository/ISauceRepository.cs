using Pikaante_Backend.Models;

namespace Pikaante_Backend.Repository;


public interface ISauceRepository
{
    Task<List<Sauce>> GetAllAsync();
    
    Task<Sauce?> GetByIdAsync(int id);
    
    Task AddAsync(Sauce sauce);
    
    Task UpdateAsync(Sauce sauce);
    
    Task DeleteAsync(Sauce sauce);
}
