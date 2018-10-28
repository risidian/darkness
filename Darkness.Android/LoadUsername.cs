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
using SQLite;

namespace Darkness.Android
{
    class LoadUsername : Activity
    {
        EditText txtUsername;
        EditText txtPassword;
        ImageButton btnLoadUsername;
        TextView DisplayVersion;
        TextView Version;

        readonly private String Ver = ("Version:", typeof(CreateUsername).Assembly.GetName().Version).ToString();
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "CreateUsername" layout resource  
            SetContentView(Resource.Layout.LoadUsername);
            // Create your application here  
            btnLoadUsername = (ImageButton)FindViewById(Resource.Id.LoadUserButton);
            txtUsername = FindViewById<EditText>(Resource.Id.UsernameText);
            txtPassword = FindViewById<EditText>(Resource.Id.PasswordText);
            btnLoadUsername.Click += LoadUser_click;

            DisplayVersion = (TextView)FindViewById(Resource.Id.DisplayVersion);
            Version = (TextView)FindViewById(Resource.Id.DisplayVersion);
            Version.Text = Ver;

        }
        private void LoadUser_click(object sender, EventArgs e)
        {
            try
            {
                string dpPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "user.db3"); //Call Database  
                var db = new SQLiteConnection(dpPath);
                var data = db.Table<UserTable>(); //Call Table  
                var data1 = data.Where(x => x.Username == txtUsername.Text && x.Password == txtPassword.Text).FirstOrDefault(); //Linq Query  
                if (data1 != null)
                {
                    Toast.MakeText(this, "Login Success", ToastLength.Short).Show();
                }
                else
                {
                    Toast.MakeText(this, "Username or Password invalid", ToastLength.Short).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.ToString(), ToastLength.Short).Show();
            }
        }
    }
}
