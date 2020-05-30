using System.Collections.Generic;
using System.Threading.Tasks;

namespace AndroidUsbSerialAssistant.Database.Interface
{
    public interface IEntityStore<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetAsync(int id);
        Task<int> SaveAsync(T t);
        Task<int> DeleteAsync(T t);
    }
}