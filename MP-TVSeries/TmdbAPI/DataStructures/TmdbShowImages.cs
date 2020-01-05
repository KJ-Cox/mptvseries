﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WindowPlugins.GUITVSeries.TmdbAPI.DataStructures
{
    [DataContract]
    public class TmdbShowImages : TmdbRequestAge
    {
        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "backdrops")]
        public List<TmdbImage> Backdrops { get; set; }

        [DataMember(Name = "posters")]
        public List<TmdbImage> Posters { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as TmdbShowImages;
            return other != null && Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
