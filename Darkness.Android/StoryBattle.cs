using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;

namespace Darkness.Android
{
    [Activity(Label = "StoryBattle")]
    public class StoryBattle : Activity
    {
        private TextView _displayUsername;
        ProgressBar _experienceBar;
        TextView tv;
        private ImageButton _settingsModeButton;
        private int UserExperienceBar { get; set; }
        private int Ally1Health { get; set; }
        private int Ally2Health { get; set; }
        private int Ally3Health { get; set; }
        private int Ally4Health { get; set; }
        private int Ally5Health { get; set; }

        private int Foe1Health { get; set; }
        private int Foe2Health { get; set; }
        private int Foe3Health { get; set; }
        private int Foe4Health { get; set; }
        private int Foe5Health { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Create your application here
            SetContentView(Resource.Layout.StoryBattle);
            _displayUsername = (TextView) FindViewById(Resource.Id.DisplayUsername);
            _displayUsername.Text = LoadUsername.LoadedUsername;
            /*_settingsModeButton = (ImageButton) FindViewById(Resource.Id.LoadSettingsOverlay);
            _settingsModeButton.Click += (sender, e) => { SetContentView(Resource.Layout.SettingsMode); };


            _settingsModeButton.Click += (sender, e) => { };*/
            _experienceBar = FindViewById<ProgressBar>(Resource.Id.ExperienceBar);

            UpdatePB updateTask = new UpdatePB(this, _experienceBar, tv);
            updateTask.Execute(100);
        }

        public class UpdatePB : AsyncTask<int, int, string>
        {
            Activity mcontext;
            ProgressBar _experienceBar;
            TextView ExperienceBarText;

            public UpdatePB(Activity context, ProgressBar pb, TextView tv)
            {
                this.mcontext = context;
                this._experienceBar = pb;
                this.ExperienceBarText = tv;
            }

            protected override string RunInBackground(params int[] @params)
            {
                // TODO Auto-generated method stub
                for (int i = 1; i <= 4; i++)
                {
                    try
                    {
                        Thread.Sleep(3000);
                    }
                    catch (Exception e)
                    {
                        // TODO Auto-generated catch block
                        //Toast.MakeText(this, ("User not matched: {0}", e.Message), ToastLength.Long).Show();
                        Log.Error($"lv==", $"" + "");
                    }

                    _experienceBar.IncrementProgressBy(25);
                    PublishProgress(i * 25);

                }

                return "finish";
            }

            protected override void OnProgressUpdate(params int[] values)
            {
                //ExperienceBarText.Text = (values[0]).ToString();
                Log.Error("lv==", values[0] + "");
            }

            protected override void OnPostExecute(string result)
            {
                mcontext.Title = result;
            }


        }
    }
}