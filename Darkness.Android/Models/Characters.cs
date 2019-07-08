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
    public class Characters
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Health { get; set; }
        public int Armour { get; set; }
        public int Attack { get; set; }
        public int Speed { get; set; }

    }
}