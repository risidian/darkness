using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Text;
using Android.Content.PM;
using SQLite;
using Darkness.Android.Models;

namespace Darkness.Android
{
    [Activity(Label = "Darkness.Android"
        , Theme = "@style/Theme.Base"
        , AlwaysRetainTaskState = true
        , LaunchMode = LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.Landscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class SmithMode : Microsoft.Xna.Framework.AndroidGameActivity
    {
        TextView _displayUsername;
        ImageButton _homeButton;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                SetContentView(Resource.Layout.SmithMode);
                _displayUsername = (TextView)FindViewById(Resource.Id.DisplayUsername);
                _displayUsername.Text = LoadUsername.LoadedUsername;
                _homeButton = (ImageButton)FindViewById(Resource.Id.LoadMainButton);
                _homeButton.Click += (sender, e) =>
                {
                    Intent loadMainMode = new Intent(this, typeof(Main));
                    StartActivity(loadMainMode);
                };
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.ToString(), ToastLength.Short).Show();
                throw;
            }
        }
    }
}