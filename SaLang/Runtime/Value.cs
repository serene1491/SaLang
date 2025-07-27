using System.Collections.Generic;
namespace SaLang.Runtime;

public enum ValueKind { Number, String, Table, Function, Nil, Error }

public readonly struct Value
{
    public readonly ValueKind Kind;
    public readonly double?        Number;
    public readonly string         String;
    public readonly Dictionary<string,Value> Table;
    public readonly FuncValue      Func;
    public readonly string         ErrorMessage;
    public readonly List<TraceFrame> ErrorStack;

    private Value(ValueKind kind,
                  double? number = null,
                  string str = null,
                  Dictionary<string,Value> tbl = null,
                  FuncValue fn = null,
                  string errMsg = null,
                  List<TraceFrame> errStack = null)
    {
        Kind          = kind;
        Number        = number;
        String        = str;
        Table         = tbl;
        Func          = fn;
        ErrorMessage  = errMsg;
        ErrorStack    = errStack;
    }

    public static Value FromNumber(double n)        
        => new Value(ValueKind.Number, number:n);
    public static Value FromString(string s)        
        => new Value(ValueKind.String, str:s);
    public static Value FromTable(Dictionary<string,Value> t) 
        => new Value(ValueKind.Table, tbl:t);
    public static Value FromFunc(FuncValue f)        
        => new Value(ValueKind.Function, fn:f);
    public static Value Nil()                      
        => new Value(ValueKind.Nil);
    public static Value Error(string message, List<TraceFrame> stack = null) 
        => new Value(ValueKind.Error, errMsg:message, errStack:stack);
    
    public bool IsError => Kind == ValueKind.Error;

    public override string ToString()
    {
        return Kind switch
        {
          ValueKind.Number   => Number.Value.ToString(),
          ValueKind.String   => String,
          ValueKind.Table    => "table",
          ValueKind.Function => "function",
          ValueKind.Nil      => "nil",
          ValueKind.Error    => $"<error: {ErrorMessage}>",
          _                  => "<?>"
        };
    }
}
