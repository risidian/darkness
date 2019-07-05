using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.Serialization;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Text;
using Android.Content.PM;
using Android.Views;
using SQLite;

namespace Darkness.Android
{ 
    [Activity(ScreenOrientation = ScreenOrientation.Landscape
    , Theme = "@style/Theme.Base")]
    public class Main : Activity
    {
        TextView _displayUsername;
        private ImageButton _storyModeButton;
        private ImageButton _smithModeButton;
        private ImageButton _settingsModeButton;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Create your application here
            SetContentView(Resource.Layout.Main);
            _displayUsername = (TextView)FindViewById(Resource.Id.DisplayUsername);
            _displayUsername.Text = LoadUsername.LoadedUsername;
            _storyModeButton = (ImageButton)FindViewById(Resource.Id.StoryModeButton);
            _smithModeButton = (ImageButton)FindViewById(Resource.Id.LoadSmithMode);
            _settingsModeButton = (ImageButton) FindViewById(Resource.Id.LoadSettingsOverlay);
            _storyModeButton.Click += (sender, e) =>
            {
                Intent loadStoryMode = new Intent(this, typeof(StoryMode));
                StartActivity(loadStoryMode);
            };
            _smithModeButton.Click += (sender, e) =>
            {
                Intent loadSmithMode = new Intent(this, typeof(SmithMode));
                StartActivity(loadSmithMode);
            };
            _settingsModeButton.Click += (sender, e) =>
            {
                Intent loadSmithMode = new Intent(this, typeof(SmithMode));
                SetContentView(Resource.Layout.SettingsMode);
            };


            _settingsModeButton.Click += (sender, e) => { };
        }
    }
}