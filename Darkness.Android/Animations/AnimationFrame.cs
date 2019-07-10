using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework;

namespace Darkness.Android.Animations
{
    class AnimationFrame
    { 
        public Rectangle SourceRectangle { get; set; }
        public TimeSpan Duration { get; set; }
    }
}

