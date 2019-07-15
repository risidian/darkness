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
    [Activity(Label = "CharacterLibrary")]
    public class CharacterLibrary : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.CharacterLibrary);
        }
    }
}