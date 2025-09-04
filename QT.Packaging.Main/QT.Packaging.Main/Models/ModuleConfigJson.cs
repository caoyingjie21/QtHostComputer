using System;

namespace QT.Packaging.Main.Models
{
    public class ModuleConfigJson
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Assembly { get; set; } = string.Empty;
        public string ViewModelType { get; set; } = string.Empty;
        public string ViewType { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = true;
    }
}
