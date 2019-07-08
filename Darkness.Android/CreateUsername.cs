using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Text;
using Android.Content.PM;
using Android.Provider;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using Android.Service.Autofill;
using Darkness.Android.Data;
using Darkness.Android.Models;
using SQLite;

namespace Darkness.Android
{
    [Activity(Theme = "@style/Theme.Base"
    , ScreenOrientation = ScreenOrientation.Landscape
       )]

    public class CreateUsername : Activity
    {
        TextView _version;
        EditText _txtUsername;
        EditText _txtPassword;
        private EditText _txtEmail;
        ImageButton _btnCreateUsername;
        private readonly string _ver = ("Version:", typeof(CreateUsername).Assembly.GetName().Version).ToString();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "CreateUsername" layout resource  
            SetContentView(Resource.Layout.CreateUserName);
            // Create your application here  
            _btnCreateUsername = (ImageButton)FindViewById(Resource.Id.CreateUserButton);
            _txtUsername = FindViewById<EditText>(Resource.Id.UserUsername);
            _txtPassword = FindViewById<EditText>(Resource.Id.UserPassword);
            _txtEmail = FindViewById<EditText>(Resource.Id.UserEmail);
            _version = (TextView)FindViewById(Resource.Id.DisplayVersion);
            _version.Text = _ver;
            _btnCreateUsername.Click += CreateUser;

        }
        private void CreateUser(object sender, EventArgs e)
        {
            try
            {
                Toast.MakeText(this, $"Created user:{_txtUsername}", ToastLength.Short).Show();
                Console.WriteLine("Creating database, if it doesn't already exist");
                string dbPath = Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                    "Darkness.db3");
                var db = new SQLiteConnection(dbPath);
                db.CreateTable<Users>();
                
                // only insert the data if it doesn't already exist
                Users newUser = new Users
                {
                    Username = _txtUsername.Text,
                    Password = _txtPassword.Text,
                    Age = 18,
                    EmailAddress = _txtEmail.Text
                };
                db.Insert(newUser);
                Toast.MakeText(this, $"Created user:{newUser.Username}", ToastLength.Short).Show();
            }
            catch (Exception exception)
            {
                Toast.MakeText(this, $"Failed to create:{exception.Message}", ToastLength.Short).Show();
            }
            Intent loadMain = new Intent(this, typeof(Main));
            StartActivity(loadMain);

            /*
            var userDatabase =  new Users
            {
                ID = 0,
                Username = _txtUsername.Text,
                Password = _txtPassword.Text,
                Age = 18,
                EmailAddress = _txtEmail.Text

            };
            Toast.MakeText(this, "Username Created Successfully...,", ToastLength.Short).Show();
        }
        catch (Exception ex)
        {
            Toast.MakeText(this, ex.ToString(), ToastLength.Short).Show();
        }
        */
        }
        /*
        public static UserDatabase _database;
        public static UserDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    string _database = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Users.db3");
                }
                return _database;
            }
        }
        */
    }
}