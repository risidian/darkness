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
using Darkness.Android.Data;
using Darkness.Android.Models;
using SQLite;

namespace Darkness.Android
{
    [Activity(Label = "StoryBattle")]
    public class StoryBattle : Activity
    {
        private TextView displayUsername;
        ProgressBar experienceBar;
        ProgressBar ally1HealthBar;
        ProgressBar foe1HealthBar;
        TextView tv;
        private TextView ally1HealthView;
        private TextView ally1NameView;
        private TextView foe1HealthView;
        private TextView foe1NameView;
        private ImageButton settingsModeButton;
        private Button Ability1;
        private Button Ability2;
        private Button Ability3;
        private Button Ability4;
        public string DbPath { get; set; }
        private int UserExperienceBar { get; set; }
        public int DamageDealt { get; set; }
        private int RatioOfDamageDealt { get; set; }
        private string Ally1Name { get; set; }
        private static int Ally1Health { get; set; }
        private int Ally1Armour { get; set; }
        private int Ally1Attack { get; set; }
        private int Ally1Speed { get; set; }
        private int Ally1Ratio { get; set; }
        private int Ally2Health { get; set; }
        private int Ally3Health { get; set; }
        private int Ally4Health { get; set; }
        private int Ally5Health { get; set; }


        private string Foe1Name { get; set; }
        private int Foe1Health { get; set; }
        private int Foe1Armour { get; set; }
        private int Foe1Attack { get; set; }
        private int Foe1Speed { get; set; }
        private int Foe1Ratio { get; set; }
        private int Foe2Health { get; set; }
        private int Foe3Health { get; set; }
        private int Foe4Health { get; set; }
        private int Foe5Health { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            DbPath = DatabaseHelper.GetLocalFilePath("Darkness.db3");
            var db = new SQLiteConnection(DbPath);

            var loadCharacters = db.Query<Characters>("SELECT * FROM Characters");
            base.OnCreate(savedInstanceState);
            //Create your application here
            SetContentView(Resource.Layout.StoryBattle);
            displayUsername = (TextView) FindViewById(Resource.Id.DisplayUsername);
            displayUsername.Text = LoadUsername.Username;
            experienceBar = FindViewById<ProgressBar>(Resource.Id.ExperienceBar);
            experienceBar.Progress = LoadUsername.Experience;
            

            ally1HealthBar = FindViewById<ProgressBar>(Resource.Id.Ally1HealthBar);
            Ally1Ratio = 20;
            ally1HealthBar.Progress = 100;

            foe1HealthBar = FindViewById<ProgressBar>(Resource.Id.Foe1HealthBar);
            Foe1Ratio = 13;
            foe1HealthBar.Progress = 100;


            Ability1 = (Button)FindViewById(Resource.Id.Ability1);
            Ability2 = (Button)FindViewById(Resource.Id.Ability2);
            Ability3 = (Button)FindViewById(Resource.Id.Ability3);
            Ability4 = (Button)FindViewById(Resource.Id.Ability4);

            ally1HealthView = (TextView)FindViewById(Resource.Id.Ally1Health);
            ally1NameView = (TextView)FindViewById(Resource.Id.Ally1Name);

            foe1NameView = (TextView)FindViewById(Resource.Id.Foe1Name);
            foe1HealthView = (TextView)FindViewById(Resource.Id.Foe1Health);
            /*_settingsModeButton = (ImageButton) FindViewById(Resource.Id.LoadSettingsOverlay);
            _settingsModeButton.Click += (sender, e) => { SetContentView(Resource.Layout.SettingsMode); };
            _settingsModeButton.Click += (sender, e) => { };*/

            foreach (var character in loadCharacters)
            {
                if (character.Name == "Garth")
                {
                    Ally1Name = character.Name;
                    Ally1Health = character.Health;
                    Ally1Armour = character.Armour;
                    Ally1Attack = character.Attack;
                    Ally1Speed = character.Speed;

                    ally1NameView.Text = Ally1Name;
                    ally1HealthView.Text = Ally1Health.ToString();

                    Console.Write(Ally1Health.ToString());
                    Console.Write(Ally1Armour.ToString());
                    Console.Write(Ally1Attack.ToString());
                    Console.Write(Ally1Speed.ToString());
                }
                if (character.Name == "Atriartous")
                {
                    Foe1Name = character.Name;
                    Foe1Health = character.Health;
                    Foe1Armour = character.Armour;
                    Foe1Attack = character.Attack;
                    Foe1Speed = character.Speed;

                    foe1NameView.Text = Foe1Name;
                    foe1HealthView.Text = Foe1Health.ToString();

                    Console.Write(Foe1Health.ToString());
                    Console.Write(Foe1Armour.ToString());
                    Console.Write(Foe1Attack.ToString());
                    Console.Write(Foe1Speed.ToString());
                }
            }

            
            Ability1.Click += (sender, e) =>
            {
                DamageDealt = 0;
                DamageDealt = Ally1Attack - Foe1Armour;
                RatioOfDamageDealt = DamageDealt * Foe1Ratio;
                if (DamageDealt <= 0)
                {
                    DamageDealt = 0;
                }
                Foe1Health = Foe1Health - DamageDealt;
                foe1HealthBar.Progress = Foe1Health * Foe1Ratio;
                if (Foe1Health <= 0)
                {
                    Toast.MakeText(ApplicationContext, $"{Foe1Name} is Dead", ToastLength.Long).Show();
                    LoadUsername.Experience = LoadUsername.Experience++;
                    Intent loadStoryMode = new Intent(this, typeof(StoryMode));
                    StartActivity(loadStoryMode);

                }
                Toast.MakeText(this, $"{Ally1Name} attacks {Foe1Name} for {DamageDealt} damage, {Foe1Name} has {Foe1Health} health remaining", ToastLength.Long).Show();
            };
            Ability2.Click += (sender, e) =>
            {
                DamageDealt = 0;
                DamageDealt = Foe1Attack - Ally1Armour;
                RatioOfDamageDealt = DamageDealt * Ally1Ratio;
                if (DamageDealt <= 0)
                {
                    DamageDealt = 0;
                }
                Ally1Health = Ally1Health - DamageDealt;
                ally1HealthBar.Progress = Ally1Health * Ally1Ratio;
                if (Ally1Health <= 0)
                {
                    Toast.MakeText(ApplicationContext, $"{Ally1Name} is Dead", ToastLength.Long).Show();

                    Intent loadStoryMode = new Intent(this, typeof(StoryMode));
                    StartActivity(loadStoryMode);
                }
                Toast.MakeText(this, $"{Foe1Name} attacks {Ally1Name} for {DamageDealt} damage, {Ally1Name} has {Ally1Health} health remaining", ToastLength.Long).Show();
            };
            Ability3.Click += (sender, e) =>
            {
                int DamageDealt = Foe1Health - (Ally1Attack - Foe1Armour);
                Toast.MakeText(this, $"{Ally1Name} attacks {Foe1Name} for {DamageDealt} damage", ToastLength.Long).Show();
            };
            Ability4.Click += (sender, e) =>
            {
                int DamageDealt = Foe1Health - (Ally1Attack - Foe1Armour);
                Toast.MakeText(this, $"{Ally1Name} attacks {Foe1Name} for {DamageDealt} damage", ToastLength.Long).Show();
            };
            //UpdatePB updateTask = new UpdatePB(this, _experienceBar, tv);
            //updateTask.Execute(100);
        }
        /*
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

           /protected override string RunInBackground(params int[] @params)
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


        }*/
    }
}