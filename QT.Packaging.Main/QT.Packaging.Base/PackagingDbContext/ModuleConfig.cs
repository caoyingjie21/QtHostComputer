using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QT.Packaging.Base.PackagingDbContext
{
    public class ModuleConfig : DataBase
    {
        [Required]
        public string? Assembly { get; set; }
        [Required]
        public string? ViewModelType { get; set; }
        [Required]
        public string? ViewType { get; set; }
    }
}
