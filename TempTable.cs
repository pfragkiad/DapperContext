using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperContext;

public readonly struct TempTable
{
    public string TableName { get; init; }

    public DateTime CreatedDate { get; init; }

    public override string ToString() => $"{TableName}, created at @{CreatedDate: yyyy-MM-dd HH:mm:ss}";
}
