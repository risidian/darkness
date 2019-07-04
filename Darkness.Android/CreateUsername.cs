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
            _txtUsername = FindViewById<EditText>(Resource.Id.UsernameText);
            _txtPassword = FindViewById<EditText>(Resource.Id.PasswordText);
            _txtEmail = FindViewById<EditText>(Resource.Id.UserEmail);
            _btnCreateUsername.Click += CreateUser;
            _version = (TextView)FindViewById(Resource.Id.DisplayVersion);
            _version.Text = _ver;
        }
        private void CreateUser(object sender, EventArgs e)
        {
            try
            {
                Users _database = new Users
                {
                    Username = _txtUsername.Text,
                    Password = _txtPassword.Text,
                    Age = 18,
                    EmailAddress = _txtEmail.Text

                };
                Database.SaveUsersAsync(_database);
                Toast.MakeText(this, "Username Created Successfully...,", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.ToString(), ToastLength.Short).Show();
            }
            Intent loadMain = new Intent(this, typeof(Main));
            StartActivity(loadMain);
        }

        static UserDatabase _database;

        public static UserDatabase Database
        {
            get
            {
                if (_database == null)
                {
                    _database = new UserDatabase(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Users.db3"));
                }
                return _database;
            }
        }
    }
}