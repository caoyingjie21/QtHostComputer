using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QT.Packaging.Base.PackagingDbContext
{
    public interface IData
    {
        public Guid Id { get; }

        public string Name { get; }

        public DateTime CreateTime { get; }

        public bool IsPublished { get; set; }
    }
}
