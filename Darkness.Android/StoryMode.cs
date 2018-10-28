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
        ImageButton HomeButton;
        protected override void OnCreate(Bundle SavedInstance)
        {
            HomeButton = (ImageButton)FindViewById(Resource.Id.LoadMainButton);
            HomeButton.Click += LoadMain_Click;
        }
        protected void LoadMain_Click(object sender, EventArgs e)
        {
            Intent loadMainMode = new Intent(this, typeof(Main));
            StartActivity(loadMainMode);
        }
    }
}

