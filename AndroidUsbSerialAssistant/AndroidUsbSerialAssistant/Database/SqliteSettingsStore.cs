using System.Collections.Generic;
using System.Threading.Tasks;
using AndroidUsbSerialAssistant.Database.Interface;
using AndroidUsbSerialAssistant.Models;
using SQLite;

namespace AndroidUsbSerialAssistant.Database
{
    public class SqliteSettingsStore : IEntityStore<Settings>
    {
        private readonly SQLiteAsyncConnection _connection;

        public SqliteSettingsStore(ISqliteDatabase database)
        {
            _connection = database.GetConnection();
            _connection.CreateTableAsync<Settings>();
        }

        public async Task<IEnumerable<Settings>> GetAllAsync()
        {
            return await _connection.Table<Settings>().ToListAsync();
        }

        public async Task<Settings> GetAsync(int id)
        {
            return await _connection.FindAsync<Settings>(id);
        }

        public async Task<int> SaveAsync(Settings settings)
        {
            return await (settings.Id == 1
                ? _connection.UpdateAsync(settings)
                : _connection.InsertAsync(settings));
        }

        public async Task<int> DeleteAsync(Settings settings)
        {
            return await _connection.DeleteAsync(settings);
        }
    }
}