using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Med.ORM.Mapping
{


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        public string Name { get; }

        public string Schema { get; set; } = "public";

        public TableAttribute(string name) => Name = name;
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ColumnAttribute : Attribute
    {
        public ColumnAttribute() { }

        public ColumnAttribute(string name) => Name = name;

        public string? Name { get; set; }

        public SqlType Type { get; set; } = SqlType.Inferred;

        public int Length { get; set; }

        public int Precision { get; set; }

        public int Scale { get; set; }
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
        public bool AutoIncrement { get; set; } = true;
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NotNullAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NullableColumnAttribute : Attribute { }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class UniqueAttribute : Attribute
    {
        public string? Group { get; set; }
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DefaultAttribute : Attribute
    {
        public string Expression { get; }

        public DefaultAttribute(string expression) => Expression = expression;
    }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        public Type Target { get; }

        public string OnDelete { get; set; } = "RESTRICT";

        public ForeignKeyAttribute(Type target) => Target = target;
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NotMappedAttribute : Attribute { }


    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class NavigationAttribute : Attribute
    {
        public string ForeignKey { get; }

        public NavigationAttribute(string foreignKey) => ForeignKey = foreignKey;
    }

  
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class InverseNavigationAttribute : Attribute
    {
        public string ForeignKey { get; }

        public InverseNavigationAttribute(string foreignKey) => ForeignKey = foreignKey;
    }

}
