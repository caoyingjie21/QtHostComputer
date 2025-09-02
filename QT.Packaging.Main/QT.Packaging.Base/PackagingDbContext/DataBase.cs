using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QT.Packaging.Base.PackagingDbContext
{
    public class DataBase : IData
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public bool IsPublished { get; set; } = true;
    }
}
