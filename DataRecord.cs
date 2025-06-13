using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Exporter
{
    // Class representing a data record
    public class DataRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SurName { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

    // Class representing a filter for data export
    public class ExportFilter
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SurName { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}
