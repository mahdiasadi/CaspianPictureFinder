using PhotoAI.Core.Models;

namespace PhotoAI.Core.Interfaces;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(long id);
    Task<Person?> GetByNameAsync(string name);
    Task<IReadOnlyList<Person>> GetAllAsync();
    Task<IReadOnlyList<Person>> SearchAsync(string query);
    Task AddAsync(Person person);
    Task UpdateAsync(Person person);
    Task DeleteAsync(long id);
    Task<long> GetCountAsync();
}
