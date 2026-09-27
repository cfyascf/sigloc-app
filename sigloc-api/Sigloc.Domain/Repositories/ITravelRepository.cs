using Sigloc.Domain.Entities;

namespace Sigloc.Domain.Repositories
{
   
    public interface ITravelRepository
    {
        Task<Travel?> GetByIdAsync(Guid id);
        
        // Essencial para a listagem macro (Endpoint 1 da sua User Story)
        Task<IEnumerable<Travel>> GetAllActiveAsync(); 
        
        // Essencial para a tela de detalhes (Endpoint 2 da sua User Story)
        Task<Travel?> GetByIdWithMonitoringAsync(Guid id); 
        
        Task AddAsync(Travel travel);
        Task UpdateAsync(Travel travel);
    }
}