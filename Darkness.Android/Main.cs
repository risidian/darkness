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
    [Activity(ScreenOrientation = ScreenOrientation.Landscape
    , Theme = "@style/Theme.Base")]
    public class Main : Activity
    {
        TextView _displayUsername;
        private ImageButton _storyModeButton;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Create your application here
            SetContentView(Resource.Layout.Main);

            _displayUsername = (TextView)FindViewById(Resource.Id.DisplayUsername);
            _storyModeButton = (ImageButton)FindViewById(Resource.Id.StoryModeButton);
            _storyModeButton.Click += (sender, e) =>
            {
                Intent loadStoryMode = new Intent(this, typeof(StoryMode));
                StartActivity(loadStoryMode);
            };
        }
    }
}