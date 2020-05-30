using System.Collections.Generic;
using System.Threading.Tasks;
using AndroidUsbSerialAssistant.Database.Interface;
using AndroidUsbSerialAssistant.Models;
using SQLite;

namespace AndroidUsbSerialAssistant.Database
{
    public class SqliteRecordsStore : IEntityStore<Records>
    {
        private readonly SQLiteAsyncConnection _connection;

        public SqliteRecordsStore(ISqliteDatabase database)
        {
            _connection = database.GetConnection();
            _connection.CreateTableAsync<Records>();
        }

        public async Task<IEnumerable<Records>> GetAllAsync()
        {
            return await _connection.Table<Records>().ToListAsync();
        }

        public async Task<IEnumerable<Records>> GetAllSentAsync()
        {
            return await _connection.Table<Records>().ToListAsync();
        }

        public async Task<Records> GetAsync(int id)
        {
            return await _connection.FindAsync<Records>(id);
        }

        public async Task<int> SaveAsync(Records records)
        {
            return await (records.Id != 0
                ? _connection.UpdateAsync(records)
                : _connection.InsertAsync(records));
        }

        public async Task<int> DeleteAsync(Records records)
        {
            return await _connection.DeleteAsync(records);
        }
    }
}