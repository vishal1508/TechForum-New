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
    
    public partial class Question_Bank_Table
    {
        public int QuestionID { get; set; }
        public string Question { get; set; }
        public string Options { get; set; }
        public string CorrectAnswer { get; set; }
        public int DomainId { get; set; }
        public int TechnologyId { get; set; }
    
        public virtual Domain_Table Domain_Table { get; set; }
        public virtual Technology_Table Technology_Table { get; set; }
    }
}