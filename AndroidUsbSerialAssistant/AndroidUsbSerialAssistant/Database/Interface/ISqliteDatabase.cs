using SQLite;

namespace AndroidUsbSerialAssistant.Database.Interface
{
    public interface ISqliteDatabase
    {
        SQLiteAsyncConnection GetConnection();
    }
}