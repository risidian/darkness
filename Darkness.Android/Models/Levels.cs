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
using SQLite;

namespace Darkness.Android.Models
{
    public class Levels
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int Level { get; set; }
        public int ExperienceRequired { get; set; }
    }
}