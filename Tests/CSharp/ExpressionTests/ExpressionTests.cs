﻿using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    public class ExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task ConversionOfNotUsesParensIfNeeded()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim rslt = Not 1 = 2
        Dim rslt2 = Not True
        Dim rslt3 = TypeOf True IsNot Boolean
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool rslt = !(1 == 2);
        bool rslt2 = !true;
        bool rslt3 = !(true is bool);
    }
}
1 source compilation errors:
BC30021: 'TypeOf ... Is' requires its left operand to have a reference type, but this operand has the value type 'Boolean'.");
        }

        [Fact]
        public async Task DateLiterals()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal date As Date = #1/1/1900#)
        Dim rslt = #1/1/1900#
        Dim rslt2 = #8/13/2002 12:14 PM#
    End Sub
End Class", @"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal partial class TestClass
{
    private void TestMethod([Optional, DateTimeConstant(599266080000000000L)] DateTime date)
    {
        var rslt = DateTime.Parse(""1900-01-01"");
        var rslt2 = DateTime.Parse(""2002-08-13 12:14:00"");
    }
}
2 source compilation errors:
BC30183: Keyword is not valid as an identifier.
BC32024: Default values cannot be supplied for parameters that are not declared 'Optional'.");
        }

        [Fact]
        public async Task NullInlineRefArgument()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class VisualBasicClass
  Public Sub UseStuff()
    Stuff(Nothing)
  End Sub

  Public Sub Stuff(ByRef strs As String())
  End Sub
End Class", @"
public partial class VisualBasicClass
{
    public void UseStuff()
    {
        string[] argstrs = null;
        Stuff(ref argstrs);
    }

    public void Stuff(ref string[] strs)
    {
    }
}");
        }

        [Fact]
        public async Task RefArgumentRValue()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Private Property C1 As Class1
    Private _c2 As Class1
    Private _o1 As Object

    Sub Foo()
        Bar(New Class1)
        Bar(C1)
        Bar(Me.C1)
        Bar(_c2)
        Bar(Me._c2)
        Bar(_o1)
        Bar(Me._o1)
    End Sub

    Sub Bar(ByRef class1)
    End Sub
End Class", @"
public partial class Class1
{
    private Class1 C1 { get; set; }

    private Class1 _c2;
    private object _o1;

    public void Foo()
    {
        object argclass1 = new Class1();
        Bar(ref argclass1);
        object argclass11 = C1;
        Bar(ref argclass11);
        object argclass12 = C1;
        Bar(ref argclass12);
        object argclass13 = _c2;
        Bar(ref argclass13);
        object argclass14 = _c2;
        Bar(ref argclass14);
        Bar(ref _o1);
        Bar(ref _o1);
    }

    public void Bar(ref object class1)
    {
    }
}");
        }

        [Fact]
        public async Task RefArgumentRValue2()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Sub Foo()
        Dim x = True
        Bar(x = True)
    End Sub

    Sub Foo2()
        Return Bar(True = False)
    End Sub

    Sub Foo3()
        If Bar(True = False) Then Bar(True = False)
    End Sub

    Sub Foo4()
        If Bar(True = False) Then
            Bar(True = False)
        ElseIf Bar(True = False) Then
            Bar(True = False)
        Else
            Bar(True = False)
        End If
    End Sub

    Sub Foo5()
        Bar(Nothing)
    End Sub

    Sub Bar(ByRef b As Boolean)
    End Sub

    Function Bar2(ByRef c1 As Class1) As Integer
        If c1 IsNot Nothing AndAlso Len(Bar3(Me)) <> 0 Then
            Return 1
        End If
        Return 0
    End Function

    Function Bar3(ByRef c1 As Class1) As String
        Return """"
    End Function

End Class", @"using Microsoft.VisualBasic;

public partial class Class1
{
    public void Foo()
    {
        bool x = true;
        bool argb = x == true;
        Bar(ref argb);
    }

    public void Foo2()
    {
        bool argb = true == false;
        return Bar(ref argb);
    }

    public void Foo3()
    {
        bool argb1 = true == false;
        if (Bar(ref argb1))
        {
            bool argb = true == false;
            Bar(ref argb);
        }
    }

    public void Foo4()
    {
        bool argb3 = true == false;
        bool argb4 = true == false;
        if (Bar(ref argb3))
        {
            bool argb = true == false;
            Bar(ref argb);
        }
        else if (Bar(ref argb4))
        {
            bool argb2 = true == false;
            Bar(ref argb2);
        }
        else
        {
            bool argb1 = true == false;
            Bar(ref argb1);
        }
    }

    public void Foo5()
    {
        bool argb = default;
        Bar(ref argb);
    }

    public void Bar(ref bool b)
    {
    }

    public int Bar2(ref Class1 c1)
    {
        var argc1 = this;
        if (c1 is object && Strings.Len(Bar3(ref argc1)) != 0)
        {
            return 1;
        }

        return 0;
    }

    public string Bar3(ref Class1 c1)
    {
        return """";
    }
}
2 source compilation errors:
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
BC30491: Expression does not produce a value.
2 target compilation errors:
CS0127: Since 'Class1.Foo2()' returns void, a return keyword must not be followed by an object expression
CS0029: Cannot implicitly convert type 'void' to 'bool'");
        }

        [Fact]
        public async Task RefArgumentUsing()
        {
            await TestConversionVisualBasicToCSharp(@"Imports System.Data.SqlClient

Public Class Class1
    Sub Foo()
        Using x = New SqlConnection
            Bar(x)
        End Using
    End Sub
    Sub Bar(ByRef x As SqlConnection)

    End Sub
End Class", @"using System.Data.SqlClient;

public partial class Class1
{
    public void Foo()
    {
        using (var x = new SqlConnection())
        {
            var argx = x;
            Bar(ref argx);
        }
    }

    public void Bar(ref SqlConnection x)
    {
    }
}");
        }

        [Fact]
        public async Task RefArgumentPropertyInitializer()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Private _p1 As Class1 = Foo(New Class1)
    Public Shared Function Foo(ByRef c1 As Class1) As Class1
        Return c1
    End Function
End Class", @"
public partial class Class1
{
    static Class1 Foo__p1()
    {
        var argc1 = new Class1();
        return Foo(ref argc1);
    }

    private Class1 _p1 = Foo__p1();

    public static Class1 Foo(ref Class1 c1)
    {
        return c1;
    }
}");
        }

        [Fact]
        public async Task MethodCallWithImplicitConversion()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Sub Foo()
        Bar(True)
        Me.Bar(""4"")
        Dim ss(1) As String
        Dim y = ss(""0"")
    End Sub

    Sub Bar(x as Integer)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices;

public partial class Class1
{
    public void Foo()
    {
        Bar(Conversions.ToInteger(true));
        Bar(Conversions.ToInteger(""4""));
        var ss = new string[2];
        string y = ss[Conversions.ToInteger(""0"")];
    }

    public void Bar(int x)
    {
    }
}");
        }

        [Fact]
        public async Task IntToEnumArg()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Sub Foo(ByVal arg As TriState)
    End Sub

    Sub Main()
        Foo(0)
    End Sub
End Class",
@"using Microsoft.VisualBasic;

public partial class Class1
{
    public void Foo(TriState arg)
    {
    }

    public void Main()
    {
        Foo(0);
    }
}");
        }

        [Fact]
        public async Task EnumToIntCast()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class MyTest
    Public Enum TestEnum As Integer
        Test1 = 0
        Test2 = 1
    End Enum

    Sub Main()
        Dim EnumVariable = TestEnum.Test1
        Dim t1 As Integer = EnumVariable
    End Sub
End Class",
@"
public partial class MyTest
{
    public enum TestEnum : int
    {
        Test1 = 0,
        Test2 = 1
    }

    public void Main()
    {
        var EnumVariable = TestEnum.Test1;
        int t1 = (int)EnumVariable;
    }
}
");
        }

        [Fact]
        public async Task FlagsEnum()
        {
            await TestConversionVisualBasicToCSharp(@"<Flags()> Public Enum FilePermissions As Integer
    None = 0
    Create = 1
    Read = 2
    Update = 4
    Delete = 8
End Enum
Public Class MyTest
    Public MyEnum As FilePermissions = FilePermissions.None + FilePermissions.Create
End Class",
@"using System;

[Flags()]
public enum FilePermissions : int
{
    None = 0,
    Create = 1,
    Read = 2,
    Update = 4,
    Delete = 8
}

public partial class MyTest
{
    public FilePermissions MyEnum = (FilePermissions)((int)FilePermissions.None + (int)FilePermissions.Create);
}");
        }

        [Fact]
        public async Task EnumSwitch()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Enum E
        A
    End Enum

    Sub Main()
        Dim e1 = E.A
        Dim e2 As Integer
        Select Case e1
            Case 0
        End Select

        Select Case e2
            Case E.A
        End Select

    End Sub
End Class",
@"
public partial class Class1
{
    public enum E
    {
        A
    }

    public void Main()
    {
        var e1 = E.A;
        var e2 = default(int);
        switch (e1)
        {
            case 0:
                {
                    break;
                }
        }

        switch (e2)
        {
            case (int)E.A:
                {
                    break;
                }
        }
    }
}");
        }

        [Fact]
        public async Task DuplicateCaseDiscarded()
        {
            await TestConversionVisualBasicToCSharp(@"Imports System
    Friend Module Module1
    Sub Main()
        Select Case 1
            Case 1
                Console.WriteLine(""a"")

            Case 1
                Console.WriteLine(""b"")

        End Select

    End Sub
End Module",
@"using System;

internal static partial class Module1
{
    public static void Main()
    {
        switch (1)
        {
            case 1:
                {
                    Console.WriteLine(""a"");
                    break;
                }

            case var @case when @case == 1:
                {
                    Console.WriteLine(""b"");
                    break;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
        }

        [Fact]
        public async Task MethodCallWithoutParens()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Sub Foo()
        Dim w = Bar
        Dim x = Me.Bar
        Dim y = Baz()
        Dim z = Me.Baz()
    End Sub

    Function Bar() As Integer
        Return 1
    End Function
    Property Baz As Integer
End Class", @"
public partial class Class1
{
    public void Foo()
    {
        int w = Bar();
        int x = Bar();
        int y = Baz;
        int z = Baz;
    }

    public int Bar()
    {
        return 1;
    }

    public int Baz { get; set; }
}");
        }

        [Fact]
        public async Task ConversionOfCTypeUsesParensIfNeeded()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim rslt = Ctype(true, Object).ToString()
        Dim rslt2 = Ctype(true, Object)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        string rslt = true.ToString();
        object rslt2 = true;
    }
}");
        }

        [Fact]
        public async Task DateKeyword()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private DefaultDate as Date = Nothing
End Class", @"using System;

internal partial class TestClass
{
    private DateTime DefaultDate = default;
}");
        }

        [Fact]
        public async Task AccessSharedThroughInstance()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class A
    Public Shared x As Integer = 2
    Public Sub Test()
        Dim tmp = Me
        Dim y = Me.x
        Dim z = tmp.x
    End Sub
End Class", @"
public partial class A
{
    public static int x = 2;

    public void Test()
    {
        var tmp = this;
        int y = x;
        int z = x;
    }
}");
        }

        [Fact]
        public async Task EmptyArrayExpression()
        {
            await TestConversionVisualBasicToCSharp(@"
Public Class Issue495
    Public Function Empty() As Integer()
        Return {}
    End Function
End Class", @"using System;

public partial class Issue495
{
    public int[] Empty()
    {
        return Array.Empty<int>();
    }
}");
        }

        [Fact]
        public async Task ReducedTypeParametersInferrable()
        {
            await TestConversionVisualBasicToCSharp(@"Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = """".Split("",""c).Select(Of String)(Function(x) x)
    End Sub
End Class", @"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select(x => x);
    }
}");
        }

        [Fact]
        public async Task ReducedTypeParametersNonInferrable()
        {
            await TestConversionVisualBasicToCSharp(@"Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = """".Split("",""c).Select(Of Object)(Function(x) x)
    End Sub
End Class", @"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select<string, object>(x => x);
    }
}");
        }

        [Fact]
        public async Task EnumNullableConversion()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Sub Main()
        Dim x = DayOfWeek.Monday
        Foo(x)
    End Sub

    Sub Foo(x As DayOfWeek?)

    End Sub
End Class", @"using System;

public partial class Class1
{
    public void Main()
    {
        var x = DayOfWeek.Monday;
        Foo(x);
    }

    public void Foo(DayOfWeek? x)
    {
    }
}");
        }

        [Fact]
        public async Task UninitializedVariable()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Sub New()
        Dim needsInitialization As Integer
        Dim notUsed As Integer
        Dim y = needsInitialization
    End Sub

    Sub Foo()
        Dim needsInitialization As Integer
        Dim notUsed As Integer
        Dim y = needsInitialization
    End Sub

    Sub Bar()
        Dim i As Integer, temp As String = String.Empty
        i += 1
    End Sub

    Sub Bar2()
        Dim i As Integer, temp As String = String.Empty
        i = i + 1
    End Sub

    Sub Bar3()
        Dim i As Integer, temp As String = String.Empty
        Dim k As Integer = i + 1
    End Sub

    Sub Bar4()
        Dim i As Integer, temp As String = String.Empty
        Dim k As Integer = i + 1
        i = 1
    End Sub

    Public ReadOnly Property State As Integer
        Get
            Dim needsInitialization As Integer
            Dim notUsed As Integer
            Dim y = needsInitialization
            Return y
        End Get
    End Property
End Class", @"
public partial class Class1
{
    public Class1()
    {
        var needsInitialization = default(int);
        int notUsed;
        int y = needsInitialization;
    }

    public void Foo()
    {
        var needsInitialization = default(int);
        int notUsed;
        int y = needsInitialization;
    }

    public void Bar()
    {
        var i = default(int);
        string temp = string.Empty;
        i += 1;
    }

    public void Bar2()
    {
        var i = default(int);
        string temp = string.Empty;
        i = i + 1;
    }

    public void Bar3()
    {
        var i = default(int);
        string temp = string.Empty;
        int k = i + 1;
    }

    public void Bar4()
    {
        var i = default(int);
        string temp = string.Empty;
        int k = i + 1;
        i = 1;
    }

    public int State
    {
        get
        {
            var needsInitialization = default(int);
            int notUsed;
            int y = needsInitialization;
            return y;
        }
    }
}");
        }

        [Fact]
        public async Task FullyTypeInferredEnumerableCreation()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim strings = { ""1"", ""2"" }
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        var strings = new[] { ""1"", ""2"" };
    }
}");
        }

        [Fact]
        public async Task GetTypeExpression()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim typ = GetType(String)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        var typ = typeof(string);
    }
}");
        }

        [Fact]
        public async Task NullableInteger()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Function Bar(value As String) As Integer?
        Dim result As Integer
        If Integer.TryParse(value, result) Then
            Return result
        Else
            Return Nothing
        End If
    End Function
End Class", @"
internal partial class TestClass
{
    public int? Bar(string value)
    {
        int result;
        if (int.TryParse(value, out result))
        {
            return result;
        }
        else
        {
            return default;
        }
    }
}");
        }

        [Fact]
        public async Task NothingInvokesDefaultForValueTypes()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Sub Bar()
        Dim number As Integer
        number = Nothing
        Dim dat As Date
        dat = Nothing
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Bar()
    {
        int number;
        number = default;
        DateTime dat;
        dat = default;
    }
}");
        }

        [Fact]
        public async Task ConditionalExpression()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Boolean = If((str = """"), True, False)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        bool result = string.IsNullOrEmpty(str) ? true : false;
    }
}
1 target compilation errors:
CS0103: The name 'string' does not exist in the current context");
        }

        [Fact]
        public async Task ConditionalExpressionInUnaryExpression()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Boolean = Not If((str = """"), True, False)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        bool result = !(string.IsNullOrEmpty(str) ? true : false);
    }
}
1 target compilation errors:
CS0103: The name 'string' does not exist in the current context");
        }

        [Fact]
        public async Task NullCoalescingExpression()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Console.WriteLine(If(str, ""<null>""))
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        Console.WriteLine(str ?? ""<null>"");
    }
}");
        }

        [Fact]
        public async Task OmittedArgumentInInvocation()
        {
            await TestConversionVisualBasicToCSharp(@"Imports System

Public Module MyExtensions
    public sub NewColumn(type As Type , Optional strV1 As String = nothing, optional code As String = ""code"")
    End sub

    public Sub CallNewColumn()
        NewColumn(GetType(MyExtensions))
        NewColumn(Nothing, , ""otherCode"")
        NewColumn(Nothing, ""fred"")
    End Sub
End Module", @"using System;

public static partial class MyExtensions
{
    public static void NewColumn(Type type, string strV1 = null, string code = ""code"")
    {
    }

    public static void CallNewColumn()
    {
        NewColumn(typeof(MyExtensions));
        NewColumn(null, code: ""otherCode"");
        NewColumn(null, ""fred"");
    }
}");
        }

        [Fact]
        public async Task OmittedArgumentInCallInvocation()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Issue445MissingParameter
    Public Sub First(a As String, b As String, c As Integer)
        Call mySuperFunction(7, , New Object())
    End Sub


    Private Sub mySuperFunction(intSomething As Integer, Optional p As Object = Nothing, Optional optionalSomething As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Class", @"using System;

public partial class Issue445MissingParameter
{
    public void First(string a, string b, int c)
    {
        mySuperFunction(7, optionalSomething: new object());
    }

    private void mySuperFunction(int intSomething, object p = null, object optionalSomething = null)
    {
        throw new NotImplementedException();
    }
}");
        }

        [Fact]
        public async Task ExternalReferenceToOutParameter()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim d = New Dictionary(Of string, string)
        Dim s As String
        d.TryGetValue(""a"", s)
    End Sub
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var d = new Dictionary<string, string>();
        string s;
        d.TryGetValue(""a"", out s);
    }
}");
        }

        [Fact]
        public async Task ElvisOperatorExpression()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass3
    Private Class Rec
        Public ReadOnly Property Prop As New Rec
    End Class
    Private Function TestMethod(ByVal str As String) As Rec
        Dim length As Integer = If(str?.Length, -1)
        Console.WriteLine(length)
        Console.ReadKey()
        Return New Rec()?.Prop?.Prop?.Prop
    End Function
End Class", @"using System;

internal partial class TestClass3
{
    private partial class Rec
    {
        public Rec Prop { get; private set; } = new Rec();
    }

    private Rec TestMethod(string str)
    {
        int length = str?.Length ?? -1;
        Console.WriteLine(length);
        Console.ReadKey();
        return new Rec()?.Prop?.Prop?.Prop;
    }
}");
        }

        [Fact]
        public async Task ObjectInitializerExpression()
        {
            await TestConversionVisualBasicToCSharp(@"Class StudentName
    Public LastName, FirstName As String
End Class

Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim student2 As StudentName = New StudentName With {.FirstName = ""Craig"", .LastName = ""Playstead""}
    End Sub
End Class", @"
internal partial class StudentName
{
    public string LastName, FirstName;
}

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var student2 = new StudentName() { FirstName = ""Craig"", LastName = ""Playstead"" };
    }
}");
        }

        [Fact]
        public async Task ObjectInitializerWithInferredName()
        {
            await TestConversionVisualBasicToCSharp(@"Class Issue480
    Public Foo As Integer

    Sub Test()
        Dim x = New With {Foo}
    End Sub

End Class", @"
internal partial class Issue480
{
    public int Foo;

    public void Test()
    {
        var x = new { Foo };
    }
}");
        }

        [Fact]
        public async Task ObjectInitializerExpression2()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim student2 = New With {Key .FirstName = ""Craig"", Key .LastName = ""Playstead""}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var student2 = new { FirstName = ""Craig"", LastName = ""Playstead"" };
    }
}");
        }
        [Fact]
        public async Task CollectionInitializers()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub DoStuff(a As Object)
    End Sub
    Private Sub TestMethod()
        DoStuff({1, 2})
        Dim intList As New List(Of Integer) From {1}
        Dim dict As New Dictionary(Of Integer, Integer) From {{1, 2}, {3, 4}}
    End Sub
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    private void DoStuff(object a)
    {
    }

    private void TestMethod()
    {
        DoStuff(new[] { 1, 2 });
        var intList = new List<int>() { 1 };
        var dict = new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } };
    }
}
1 target compilation errors:
CS7036: There is no argument given that corresponds to the required formal parameter 'value' of 'Dictionary<int, int>.Add(int, int)'");
        }

        [Fact]
        public async Task DelegateExpression()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim test As Func(Of Integer, Integer) = Function(ByVal a As Integer) a * 2
        test(3)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = (a) => a * 2;
        test(3);
    }
}");
        }

        [Fact]
        public async Task LambdaBodyExpression()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim test As Func(Of Integer, Integer) = Function(a) a * 2
        Dim test2 As Func(Of Integer, Integer, Double) = Function(a, b)
            If b > 0 Then Return a / b
            Return 0
        End Function

        Dim test3 As Func(Of Integer, Integer, Integer) = Function(a, b) a Mod b
        test(3)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = a => a * 2;
        Func<int, int, double> test2 = (a, b) =>
        {
            if (b > 0)
                return a / (double)b;
            return 0;
        };
        Func<int, int, int> test3 = (a, b) => a % b;
        test(3);
    }
}");
        }

        [Fact]
        public async Task TypeInferredLambdaBodyExpression()
        {
            // BUG: Should actually call:
            // * Operators::DivideObject(object, object)
            // * Operators::ConditionalCompareObjectGreater(object, object, bool)
            // * Operators::MultiplyObject(object, object)
            // * Operators::ModObject(object, object)
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim test = Function(a) a * 2
        Dim test2 = Function(a, b)
            If b > 0 Then Return a / b
            Return 0
        End Function

        Dim test3 = Function(a, b) a Mod b
        test(3)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices;

internal partial class TestClass
{
    private void TestMethod()
    {
        object test(object a) => a * 2;
        object test2(object a, object b)
        {
            if (Conversions.ToBoolean(b > 0))
                return a / b;
            return 0;
        };
        object test3(object a, object b) => a % b;
        test(3);
    }
}
4 target compilation errors:
CS0019: Operator '*' cannot be applied to operands of type 'object' and 'int'
CS0019: Operator '>' cannot be applied to operands of type 'object' and 'int'
CS0019: Operator '/' cannot be applied to operands of type 'object' and 'object'
CS0019: Operator '%' cannot be applied to operands of type 'object' and 'object'");
        }

        [Fact]
        public async Task SingleLineLambdaWithStatementBody()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 1
        Dim simpleAssignmentAction As System.Action = Sub() x = 1
        Dim nonBlockAction As System.Action = Sub() Console.WriteLine(""Statement"")
        Dim ifAction As Action = Sub() If True Then Exit Sub
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        int x = 1;
        Action simpleAssignmentAction = () => x = 1;
        Action nonBlockAction = () => Console.WriteLine(""Statement"");
        Action ifAction = () => { if (true) return; };
    }
}");
        }

        [Fact]
        public async Task Await()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Function SomeAsyncMethod() As Task(Of Integer)
        Return Task.FromResult(0)
    End Function

    Private Async Sub TestMethod()
        Dim result As Integer = Await SomeAsyncMethod()
        Console.WriteLine(result)
    End Sub
End Class", @"using System;
using System.Threading.Tasks;

internal partial class TestClass
{
    private Task<int> SomeAsyncMethod()
    {
        return Task.FromResult(0);
    }

    private async void TestMethod()
    {
        int result = await SomeAsyncMethod();
        Console.WriteLine(result);
    }
}");
        }

        [Fact]
        public async Task NameQualifyingHandlesInheritance()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClassBase
    Sub DoStuff()
    End Sub
End Class
Class TestClass
    Inherits TestClassBase
    Private Sub TestMethod()
        DoStuff()
    End Sub
End Class", @"
internal partial class TestClassBase
{
    public void DoStuff()
    {
    }
}

internal partial class TestClass : TestClassBase
{
    private void TestMethod()
    {
        DoStuff();
    }
}");
        }

        [Fact]
        public async Task UsingGlobalImport()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Function TestMethod() As String
         Return vbCrLf
    End Function
End Class", @"using Microsoft.VisualBasic;

internal partial class TestClass
{
    public string TestMethod()
    {
        return Constants.vbCrLf;
    }
}");
        }

        [Fact]
        public async Task ValueCapitalisation()
        {
            await TestConversionVisualBasicToCSharp(@"public Enum TestState
one
two
end enum
public class test
private _state as TestState
    Public Property State As TestState
        Get
            Return _state
        End Get
        Set
            If Not _state.Equals(Value) Then
                _state = Value
            End If
        End Set
    End Property
end class", @"
public enum TestState
{
    one,
    two
}

public partial class test
{
    private TestState _state;

    public TestState State
    {
        get
        {
            return _state;
        }

        set
        {
            if (!_state.Equals(value))
            {
                _state = value;
            }
        }
    }
}");
        }

        [Fact]
        public async Task ConstLiteralConversionIssue329()
        {
            await TestConversionVisualBasicToCSharp(
                @"Module Module1
    Const a As Boolean = 1
    Const b As Char = ChrW(1)
    Const c As Single = 1
    Const d As Double = 1
    Const e As Decimal = 1
    Const f As SByte = 1
    Const g As Short = 1
    Const h As Integer = 1
    Const i As Long = 1
    Const j As Byte = 1
    Const k As UInteger = 1
    Const l As UShort = 1
    Const m As ULong = 1

    Sub Main()
        Const x As SByte = 4
    End Sub
End Module", @"
internal static partial class Module1
{
    private const bool a = true;
    private const char b = (char)1;
    private const float c = 1;
    private const double d = 1;
    private const decimal e = 1;
    private const sbyte f = 1;
    private const short g = 1;
    private const int h = 1;
    private const long i = 1;
    private const byte j = 1;
    private const uint k = 1;
    private const ushort l = 1;
    private const ulong m = 1;

    public static void Main()
    {
        const sbyte x = 4;
    }
}
");
        }

        [Fact]
        public async Task SelectCaseIssue361()
        {
            await TestConversionVisualBasicToCSharp(
                @"Module Module1
    Enum E
        A = 1
    End Enum

    Sub Main()
        Dim x = 1
        Select Case x
            Case E.A
                Console.WriteLine(""z"")
        End Select
    End Sub
End Module", @"using System;

internal static partial class Module1
{
    public enum E
    {
        A = 1
    }

    public static void Main()
    {
        int x = 1;
        switch (x)
        {
            case (int)E.A:
                {
                    Console.WriteLine(""z"");
                    break;
                }
        }
    }
}");
        }

        [Fact]
        public async Task Tuple()
        {
            await TestConversionVisualBasicToCSharp(
                @"Public Function GetString(yourBoolean as Boolean) As Boolean
    Return 1 <> 1 OrElse if (yourBoolean, True, False)
End Function",
                @"public bool GetString(bool yourBoolean)
{
    return 1 != 1 || (yourBoolean ? true : false);
}");
        }

        [Fact]
        public async Task UseEventBackingField()
        {
            await TestConversionVisualBasicToCSharp(
                @"Public Class Foo
    Public Event Bar As EventHandler(Of EventArgs)

    Protected Sub OnBar(e As EventArgs)
        If BarEvent Is Nothing Then
            System.Diagnostics.Debug.WriteLine(""No subscriber"")
        Else
            RaiseEvent Bar(Me, e)
        End If
    End Sub
End Class",
                @"using System;
using System.Diagnostics;

public partial class Foo
{
    public event EventHandler<EventArgs> Bar;

    protected void OnBar(EventArgs e)
    {
        if (Bar is null)
        {
            Debug.WriteLine(""No subscriber"");
        }
        else
        {
            Bar?.Invoke(this, e);
        }
    }
}");
        }

        [Fact]
        public async Task DateTimeToDateAndTime()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Sub Foo()
        Dim x = DateAdd(""m"", 5, Now)
    End Sub
End Class", @"using Microsoft.VisualBasic;

public partial class Class1
{
    public void Foo()
    {
        var x = DateAndTime.DateAdd(""m"", 5, DateAndTime.Now);
    }
}");
        }

        [Fact]
        public async Task BaseFinalizeRemoved()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class", @"
public partial class Class1
{
    ~Class1()
    {
    }
}");
        }

        [Fact]
        public async Task GlobalNameIssue375()
        {
            await TestConversionVisualBasicToCSharp(@"Module Module1
    Sub Main()
        Dim x = Microsoft.VisualBasic.Timer
    End Sub
End Module", @"using Microsoft.VisualBasic;

internal static partial class Module1
{
    public static void Main()
    {
        double x = DateAndTime.Timer;
    }
}");
        }

        [Fact]
        public async Task TernaryConversionIssue363()
        {
            await TestConversionVisualBasicToCSharp(@"Module Module1
    Sub Main()
        Dim x As Short = If(True, CShort(50), 100S)
    End Sub
End Module", @"using Microsoft.VisualBasic.CompilerServices;

internal static partial class Module1
{
    public static void Main()
    {
        short x = true ? Conversions.ToShort(50) : Conversions.ToShort(100);
    }
}
");
        }

        [Fact]
        public async Task GenericMethodCalledWithAnonymousType()
        {
            await TestConversionVisualBasicToCSharp(
                @"Public Class MoreParsing
    Sub DoGet()
        Dim anon = New With {
            .ANumber = 5
        }
        Dim sameAnon = Identity(anon)
        Dim repeated = Enumerable.Repeat(anon, 5).ToList()
    End Sub

    Private Function Identity(Of TType)(tInstance As TType) As TType
        Return tInstance
    End Function
End Class",
                @"using System.Linq;

public partial class MoreParsing
{
    public void DoGet()
    {
        var anon = new { ANumber = 5 };
        var sameAnon = Identity(anon);
        var repeated = Enumerable.Repeat(anon, 5).ToList();
    }

    private TType Identity<TType>(TType tInstance)
    {
        return tInstance;
    }
}");
        }

    }
}