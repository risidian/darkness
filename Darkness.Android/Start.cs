using Android.App;
using Android.OS;
using Android.Widget;
using Android.Content.PM;
using Android.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Android.Content;
using Android.Text;
using Darkness.Android.Data;
using SQLite;



namespace Darkness.Android
{
    [Activity(Label = "Darkness.Android"
    , MainLauncher = true
    , Icon = "@drawable/icon"
    , ScreenOrientation = ScreenOrientation.Landscape
    , Theme = "@style/Theme.Base")]
    public class Start : Activity
    {
        public string DbPath { get; set; }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Start);

            DbPath = DatabaseHelper.GetLocalFilePath("Darkness.db3");

            // New code will go here
            ImageButton CreateUserButton = (ImageButton)FindViewById(Resource.Id.CreateUserButton);
            CreateUserButton.Click += (sender, e) =>
            {
                Intent openCreateUser = new Intent(this, typeof(CreateUsername));
                StartActivity(openCreateUser);
            };
            ImageButton LoadUserButton = (ImageButton)FindViewById(Resource.Id.LoadUserButton);
            LoadUserButton.Click += (sender, e) =>
            {
                Intent openLoadUserName = new Intent(this, typeof(LoadUsername));
                StartActivity(openLoadUserName);
            };
        }
    }
}