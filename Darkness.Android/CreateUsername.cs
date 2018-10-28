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
using SQLite;

namespace Darkness.Android
{
    [Activity(Theme = "@style/Theme.Base"
    , ScreenOrientation = ScreenOrientation.Landscape
       )]

    public class CreateUsername : Activity
    {
        TextView DisplayVersion;
        TextView Version;
        EditText txtUsername;
        EditText txtPassword;
        ImageButton btnCreateUsername;
        readonly private String Ver = ("Version:", typeof(CreateUsername).Assembly.GetName().Version).ToString();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "CreateUsername" layout resource  
            SetContentView(Resource.Layout.CreateUserName);
            // Create your application here  
            btnCreateUsername = (ImageButton)FindViewById(Resource.Id.CreateUserButton);
            txtUsername = FindViewById<EditText>(Resource.Id.UsernameText);
            txtPassword = FindViewById<EditText>(Resource.Id.PasswordText);
            btnCreateUsername.Click += Btncreate_Click;
            CreateDB();
            DisplayVersion = (TextView)FindViewById(Resource.Id.DisplayVersion);
            Version = (TextView)FindViewById(Resource.Id.DisplayVersion);
            Version.Text = Ver;

        }
        private void Btncreate_Click(object sender, EventArgs e)
        {
            try
            {
                string dpPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "user.db3");
                var db = new SQLiteConnection(dpPath);
                db.CreateTable<UserTable>();
                UserTable tbl = new UserTable
                {
                    Username = txtUsername.Text,
                    Password = txtPassword.Text
                };
                db.Insert(tbl);
                Toast.MakeText(this, "Username Created Successfully...,", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.ToString(), ToastLength.Short).Show();
            }
            Intent loadMain = new Intent(this, typeof(Main));
            StartActivity(loadMain);
        }
        public string CreateDB()
        {
            var output = "";
            output += "Creating Databse if it doesnt exists";
            string dpPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "user.db3"); //Create New Database  
            var db = new SQLiteConnection(dpPath);
            output += "\n Database Created....";
            return output;
        }
    }
}