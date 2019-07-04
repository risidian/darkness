using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Text;
using Android.Content.PM;
using Android.Provider;
using Darkness.Android.Data;
using SQLite;
using Darkness.Android.Models;

namespace Darkness.Android
{
    [Activity(Theme = "@style/Theme.Base"
        , ScreenOrientation = ScreenOrientation.Landscape
    )]
    public class LoadUsername : Activity
    {
        //TextView _displayVersion;
        TextView _version;
        EditText _txtUsername;
        EditText _txtPassword;
        ImageButton _btnLoadUsername;

        private readonly string _ver = ("Version:", typeof(CreateUsername).Assembly.GetName().Version).ToString();
        protected override void OnCreate(Bundle savedBundle)
        {
            base.OnCreate(savedBundle);
            // Set our view from the "LoadUsername" layout resource  
            SetContentView(Resource.Layout.LoadUserName);
            // Create your application here  
            _btnLoadUsername = (ImageButton)FindViewById(Resource.Id.LoadUserButton);
            _txtUsername = FindViewById<EditText>(Resource.Id.UsernameText);
            _txtPassword = FindViewById<EditText>(Resource.Id.PasswordText);

            try
            {
                _btnLoadUsername.Click += LoadUser;
            }
            catch (Exception e)
            {
                Toast.MakeText(this, $"This button is broken!: {e}", ToastLength.Long).Show();
            }

            //_displayVersion = (TextView)FindViewById(Resource.Id.DisplayVersion);
            _version = (TextView)FindViewById(Resource.Id.DisplayVersion);
            _version.Text = _ver;

        }

        private void LoadUser(object sender, EventArgs e)
        {
            //TODO: rewrite this code to retrieve from UserDatabase
            try
            {
                Database.GetUsersAsync(_txtUsername.Text);
                Toast.MakeText(this, "Login Success", ToastLength.Short).Show();
                
                Intent loadMain = new Intent(this, typeof(Main));
                StartActivity(loadMain);
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.ToString(), ToastLength.Long).Show();
            }
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
