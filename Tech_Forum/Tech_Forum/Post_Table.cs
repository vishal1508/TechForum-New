//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Tech_Forum
{
    using System;
    using System.Collections.Generic;
    
    public partial class Post_Table
    {
        public int postid { get; set; }
        public string title { get; set; }
        public string domain { get; set; }
        public string technology { get; set; }
        public string content_ { get; set; }
        public string tags { get; set; }
        public string rating { get; set; }
        public System.DateTime date { get; set; }
        public string comments { get; set; }
        public bool category { get; set; }
        public string userid { get; set; }
    
        public virtual Subscriber_Table Subscriber_Table { get; set; }
    }
}