using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Med.ORM.Mapping
{
    public enum SqlType
    {
        Inferred = 0,
        SmallInt,
        Int,
        BigInt,

        Decimal,
        Float,
        Real,

        Varchar,
        Char,
        Text,

        Boolean,
        Date,
        Timestamp,
        TimestampTz,
        Uuid
    }
}
