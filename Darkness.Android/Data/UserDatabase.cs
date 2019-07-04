
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Darkness.Android.Models;
using SQLite;
using System.Threading.Tasks;
using Android.Net;

namespace Darkness.Android.Data
{
    public class UserDatabase
    {
        readonly SQLiteAsyncConnection _database;

        public UserDatabase(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Users>().Wait();
        }

        public Task<List<Users>> GetUsersAsync()
        {
            return _database.Table<Users>().ToListAsync();
        }

        public Task<Users> GetUsersAsync(string username)
        {
            return _database.Table<Users>()
                .Where(i => i.Username == username)
                .FirstOrDefaultAsync();
            
        }

        public Task<int> SaveUsersAsync(Users Users)
        {
            if (Users.ID != 0)
            {
                return _database.UpdateAsync(Users);
            }
            else
            {
                return _database.InsertAsync(Users);
            }
        }

        public Task<int> DeleteUsersAsync(Users Users)
        {
            return _database.DeleteAsync(Users);
        }
    }
}