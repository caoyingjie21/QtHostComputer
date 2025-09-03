using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using Material.Icons;
using QT.Packaging.Base.PackagingDbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QT.Packaging.Base
{
    public class ViewModelBase : ObservableObject, IData
    {
        public ViewModelBase()
        {
            SetDefaultValues();
        }

        protected virtual void SetDefaultValues()
        {
            this.Name = "Default";
            this.IconKind = MaterialIconKind.Settings;
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public MaterialIconKind IconKind { get; set; } = MaterialIconKind.Marker;
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public bool IsPublished { get; set; } = true;
    }
}
