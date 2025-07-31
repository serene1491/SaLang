using System.Collections.Generic;
using SaLang.Analyzers;
using SaLang.Common;
namespace SaLang.Runtime;

public readonly struct Value
{
    public readonly ValueKind Kind;
    public readonly double?        Number;
    public readonly string         String;
    public readonly bool?          Bool;
    public readonly FuncValue      Func;
    public readonly Error?         Error;
    public readonly Dictionary<string,Value> Table;

    private Value(ValueKind kind,
                  double? number = null,
                  string str = null,
                  bool? boolean = null,
                  Dictionary<string,Value> tbl = null,
                  FuncValue fn = null,
                  Error? error = null)
    {
        Kind          = kind;
        Number        = number;
        String        = str;
        Bool          = boolean;
        Table         = tbl;
        Func          = fn;
        Error         = error;
    }
    
    #region  Factory
    public static Value FromNumber(double n)
        => new Value(ValueKind.Number, number: n);
    public static Value FromString(string s)        
        => new Value(ValueKind.String, str:s);
    public static Value FromBool(bool b)        
        => new Value(ValueKind.Bool, boolean: b);
    public static Value FromTable(Dictionary<string, Value> t)
        => new Value(ValueKind.Table, tbl: t);
    public static Value FromFunc(FuncValue f)        
        => new Value(ValueKind.Function, fn:f);
    public static Value Nil()                      
        => new Value(ValueKind.Nil);
    public static Value FromError(Error error)
        => new Value(ValueKind.Error, error: error);
    #endregion Factory

    public bool IsError => Kind == ValueKind.Error;

    public override string ToString()
    {
        return Kind switch
        {
          ValueKind.Number   => Number.Value.ToString(),
          ValueKind.String   => String,
          ValueKind.Bool     => Bool.Value? "true" : "false"  ?? "nil<bool>",
          ValueKind.Table    => "table",
          ValueKind.Function => "function",
          ValueKind.Nil      => "nil",
          ValueKind.Error    => Error?.Build() ?? "nil<error>",
          _                  => "<?>"
        };
    }
}
