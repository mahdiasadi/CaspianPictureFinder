using Microsoft.EntityFrameworkCore;
using PhotoAI.Core.Interfaces;
using PhotoAI.Core.Models;

namespace PhotoAI.Storage.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly Func<PhotoAiDbContext> _contextFactory;

    public PersonRepository(Func<PhotoAiDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Person?> GetByIdAsync(long id)
    {
        using var context = _contextFactory();
        return await context.Persons.FindAsync(id);
    }

    public async Task<Person?> GetByNameAsync(string name)
    {
        using var context = _contextFactory();
        return await context.Persons.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IReadOnlyList<Person>> GetAllAsync()
    {
        using var context = _contextFactory();
        return await context.Persons.OrderByDescending(p => p.LastSeen).ToListAsync();
    }

    public async Task<IReadOnlyList<Person>> SearchAsync(string query)
    {
        using var context = _contextFactory();
        return await context.Persons
            .Where(p => p.Name.Contains(query))
            .OrderByDescending(p => p.LastSeen)
            .ToListAsync();
    }

    public async Task AddAsync(Person person)
    {
        using var context = _contextFactory();
        await context.Persons.AddAsync(person);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Person person)
    {
        using var context = _contextFactory();
        context.Persons.Update(person);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(long id)
    {
        using var context = _contextFactory();
        var person = await context.Persons.FindAsync(id);
        if (person != null)
        {
            context.Persons.Remove(person);
            await context.SaveChangesAsync();
        }
    }

    public async Task<long> GetCountAsync()
    {
        using var context = _contextFactory();
        return await context.Persons.CountAsync();
    }
}
