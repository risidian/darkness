using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Text;
using Android.Content.PM;
using SQLite;
using Darkness.Android.Models;
using Android.Views;

namespace Darkness.Android
{
    [Activity(Label = "Darkness.Android"
        , Theme = "@style/Theme.StoryMode"
        , AlwaysRetainTaskState = true
        , LaunchMode = LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.Landscape
        , ConfigurationChanges = ConfigChanges.Orientation | 
                                 ConfigChanges.Keyboard | 
                                 ConfigChanges.KeyboardHidden | 
                                 ConfigChanges.ScreenSize)]

    public class StoryMode : Activity
    {
        TextView _displayUsername;
        ImageButton _homeButton;
        private ImageButton _storyBattleButton;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                SetContentView(Resource.Layout.StoryMode);

                _displayUsername = (TextView)FindViewById(Resource.Id.DisplayUsername);
                _displayUsername.Text = LoadUsername.Username;
                _homeButton = (ImageButton)FindViewById(Resource.Id.LoadMainButton);
                _homeButton.Click += (sender, e) =>
                {
                    Intent loadMainMode = new Intent(this, typeof(Main));
                    StartActivity(loadMainMode);
                };
                _storyBattleButton = (ImageButton)FindViewById(Resource.Id.StoryBattleButton);
                _storyBattleButton.Click += (sender, e) =>
                {
                    try
                    {
                        Intent loadStoryBattleMode = new Intent(this, typeof(StoryBattle));
                        StartActivity(loadStoryBattleMode);
                        //SetContentView((View)walkingBattleMode.Services.GetService(typeof(View)));

                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        throw;
                    }
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

