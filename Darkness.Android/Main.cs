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
        private TextView DisplayUsername;
        private ImageButton StoryModeButton;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Create your application here
            SetContentView(Resource.Layout.Main);

            DisplayUsername = (TextView)FindViewById(Resource.Id.ShowUsername);
            DisplayUsername = (EditText)FindViewById(Resource.Id.UsernameText);
            StoryModeButton = (ImageButton)FindViewById(Resource.Id.StoryModeButton);
            StoryModeButton.Click += (sender, e) =>
            {
                Intent loadStoryMode = new Intent(this, typeof(StoryMode));
                StartActivity(loadStoryMode);
            };
        }
    }
}