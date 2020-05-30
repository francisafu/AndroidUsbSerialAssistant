using System;
using System.IO;
using AndroidUsbSerialAssistant.Database.Interface;
using SQLite;

namespace AndroidUsbSerialAssistant.Database
{
    public class SqliteDatabase : ISqliteDatabase
    {
        public SQLiteAsyncConnection GetConnection()
        {
            var path =
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder
                        .LocalApplicationData),
                    "SerialAssistant.db3");
            return new SQLiteAsyncConnection(path);
        }
    }
}