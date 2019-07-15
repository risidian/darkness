using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using Darkness.Android.Data;
using Darkness.Android.Game;
using Darkness.Android.Models;
using Microsoft.Xna.Framework;
using SQLite;

namespace Darkness.Android
{
    public class StoryBattle : AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {/*
            base.OnCreate(bundle);

            var g = new BattleMode();
            SetContentView((View)g.Services.GetService(typeof(View)));
            g.Run();
            */
        }

    }
}