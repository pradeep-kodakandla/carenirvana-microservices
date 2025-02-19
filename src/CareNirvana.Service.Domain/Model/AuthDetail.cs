using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Domain.Model
{
    public class AuthDetail
    {
        public int Id { get; set; }
        public List<object> Data { get; set; } // Change from string to List<object>
        public DateTime CreatedOn { get; set; }
    }
}
