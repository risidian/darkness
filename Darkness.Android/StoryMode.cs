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
        , Theme = "@style/Theme.StoryMode"
        , AlwaysRetainTaskState = true
        , LaunchMode = LaunchMode.SingleInstance
        //, ScreenOrientation = ScreenOrientation.FullUser
        , ScreenOrientation = ScreenOrientation.Landscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class StoryMode : Microsoft.Xna.Framework.AndroidGameActivity
    {
        ImageButton _homeButton;
        protected override void OnCreate(Bundle savedInstance)
        {
            try
            {
                _homeButton = (ImageButton)FindViewById(Resource.Id.LoadMainButton);
                _homeButton.Click += LoadMain_Click;
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.ToString(), ToastLength.Short).Show();
                throw;
            }
        }
        protected void LoadMain_Click(object sender, EventArgs e)
        {
            Intent loadMainMode = new Intent(this, typeof(Main));
            StartActivity(loadMainMode);
        }
    }
}

