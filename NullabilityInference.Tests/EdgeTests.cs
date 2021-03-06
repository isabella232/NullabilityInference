﻿// Copyright (c) 2020 Daniel Grunwald
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using Xunit;

namespace ICSharpCode.NullabilityInference.Tests
{
    /// <summary>
    /// Unit tests where we check that an edge from parameter to return type was detected.
    /// </summary>
    public class EdgeTests : NullabilityTestHelper
    {
        [Fact]
        public void SimpleReturn()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input)
    {
        return input;
    }
}"));
        }

        [Fact]
        public void SimpleExpressionReturn()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) => input;
}"));
        }

        [Fact]
        public void ReturnConstant()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input)
    {
        return ""abc"";
    }
}"));
        }

        [Fact]
        public void UseLocal()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input)
    {
        string local = input;
        return local;
    }
}"));
        }

        [Fact]
        public void UseLocalVar()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input)
    {
        var local = input;
        return local;
    }
}"));
        }

        [Fact]
        public void NullCheck()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input)
    {
        if (input == null) {
            return ""null"";
        }
        return input;
    }
}"));
        }

        [Fact]
        public void UseLocalWithNullCheck()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input)
    {
        string local = input;
        if (local == null) {
            return ""null"";
        }
        return local;
    }
}"));
        }

        [Fact]
        public void UseField()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    static string field;

    public static string Test(string input)
    {
        field = input;
        return field;
    }
}"));
        }

        [Fact]
        public void UseFieldWithNullCheck()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    static string field;

    public static string Test(string input)
    {
        field = input;
        if (field == null) {
            return ""null"";
        }
        return field;
    }
}"));
        }

        [Fact]
        public void UseAutoProperty()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    static string Property { get; set; }

    public static string Test(string input)
    {
        Property = input;
        return Property;
    }
}"));
        }

        [Fact]
        public void UseAutoPropertyWithNullCheck()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    static string Property { get; set; }

    public static string Test(string input)
    {
        Property = input;
        if (Property == null) {
            return ""null"";
        }
        return Property;
    }
}"));
        }

        [Fact]
        public void UseCustomPropertyGetter()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    static string field;

    static string Property { get { return field; } }

    public static string Test(string input)
    {
        field = input;
        return Property;
    }
}"));
        }

        [Fact]
        public void UseCustomPropertySetter()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    static string field;

    static string Property { set { field = value; } }

    public static string Test(string input)
    {
        Property = input;
        return field;
    }
}"));
        }

        [Fact]
        public void Call()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) => Identity(input);
    public static string Identity(string input) => input;
}"));
        }

        [Fact]
        public void CallExtensionMethod()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
static class Program {
    public static string Test(string input) => input.Identity();
    public static string Identity(this string input) => input;
}"));
        }

        [Fact]
        public void CallViaParamsArray()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
static class Program {
    public static string Test(string input) => Identity(input);
    public static string Identity(params string[] inputs) => inputs[0];
}"));
        }

        [Fact]
        public void CallViaRefReturn()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) => Identity(ref input);
    public static ref string Identity(ref string input) => ref input;
}"));
        }

        [Fact]
        public void CallLocalFunction()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
static class Program {
    public static string Test(string input) {
        return Identity(input);

        string Identity(string input) => input;
    }
}"));
        }

        [Fact]
        public void CallAsyncFunction()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Threading.Tasks;
static class Program {
    public static string Test(string input) {
        return Identity(input).Result;
    }
    static async Task<string> Identity(string input) => input;
}"));
        }

        [Fact]
        public void CallAsyncLocalFunction()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Threading.Tasks;
static class Program {
    public static string Test(string input) {
        return Identity(input).Result;

        async Task<string> Identity(string input) => input;
    }
}"));
        }

        [Fact]
        public void CallAsyncFunctionWithAwait()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Threading.Tasks;
static class Program {
    public static string Test(string input) {
        return Identity1(input).Result;
    }
    static async Task<string> Identity1(string input) => await Identity2(input);
    static async Task<string> Identity2(string input) => input;
}"));
        }

        [Fact]
        public void CallAsyncLambda()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Threading.Tasks;
static class Program {
    public static string Test(string input) {
        Func<string, Task<string>> identity = async input => input;

        return identity(input).Result;
    }
}"));
        }

        [Fact]
        public void NullCoalescingLeft()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) => input ?? ""abc"";
}"));
        }

        [Fact]
        public void NullCoalescingRight()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    static string field;
    public static string Test(string input) => field ?? input;
}"));
        }

        [Fact]
        public void NullCoalescingDifferentTypes()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    static object field;
    public static object Test(string input) => input ?? field;
}"));
        }

        [Fact]
        public void NullCoalescingWithThrowExpr()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) => input ?? throw null;
}"));
        }

        [Fact]
        public void NullCoalescingTypeArgumentLeft()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) => (new string[] { input } ?? new string[] { })[0];
}"));
        }

        [Fact]
        public void NullCoalescingTypeArgumentRight()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) => (new string[] { } ?? new string[] { input })[0];
}"));
        }

        [Fact]
        public void NullCoalescingTypeArgumentWithThrowExpr()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) => (new string[] { input } ?? throw null)[0];
}"));
        }

        [Fact]
        public void ListElement()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var list = new System.Collections.Generic.List<string>();
        list.Add(input);
        return list[0];
    }
}"));
        }

        [Fact]
        public void ListElementViaCollectionInit()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var list = new System.Collections.Generic.List<string> { input };
        return list[0];
    }
}"));
        }

        [Fact]
        public void ListElementViaEnumerator()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var list = new System.Collections.Generic.List<string>();
        list.Add(input);
        using (var e = list.GetEnumerator()) {
            e.MoveNext();
            return e.Current;
        }
    }
}"));
        }

        [Fact]
        public void ArrayElement()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var arr = new string[1];
        arr[0] = input;
        return arr[0];
    }
}"));
        }

        [Fact]
        public void ArrayElementViaArrayInitializer()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var arr = new string[1] { input };
        return arr[0];
    }
}"));
        }

        [Fact]
        public void MultiDimArrayElement()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var arr = new string[1, 1];
        arr[0,0 ] = input;
        return arr[0, 0];
    }
}"));
        }

        [Fact]
        public void MultiDimArrayElementViaArrayInitializer()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var arr = new string[1, 1] { { input } };
        return arr[0, 0];
    }
}"));
        }

        [Fact]
        public void NestedArrayElement()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var arr = new string[1][];
        arr[0][0] = input;
        return arr[0][0];
    }
}"));
        }

        [Fact]
        public void NestedArrayElementViaArrayInitializer()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var arr = new string[][] { new string[] { input } };
        return arr[0][0];
    }
}"));
        }

        [Fact]
        public void CallIndexer()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public string Test(string input) => this[input];
    public string this[string input] => input;
}"));
        }

        [Fact]
        public void CallConstructor()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class C {
    internal readonly string X;
    public C(string x) { this.X = x; }
}
class Program {
    public static string Test(string input) {
        return new C(input).X;
    }
}"));
        }

        [Fact]
        public void CallConstructorWithArray()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class C {
    internal readonly ulong[] X;
    public C(ulong[] x) { this.X = x; }
}
class Program {
    public static ulong[] Test(ulong[] input) {
        return new C(input).X;
    }
}"));
        }

        [Fact]
        public void Tuple()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var tuple = (input, 1);
        (string a, _) = tuple;
        return a;
    }
}"));
        }

        [Fact]
        public void TupleItemAccess1()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var tuple = (input, string.Empty);
        return tuple.Item1;
    }
}"));
        }

        [Fact]
        public void TupleItemAccess2()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var tuple = (input, string.Empty);
        return tuple.Item2;
    }
}"));
        }

        [Fact]
        public void TupleNamedItemAccess1()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var tuple = (input, string.Empty);
        return tuple.input;
    }
}"));
        }

        [Fact]
        public void TupleNamedItemAccess2()
        {
            Assert.False(HasPathFromParameterToReturnType(@"
class Program {
    public static string Test(string input) {
        var tuple = (input, string.Empty);
        return tuple.Empty;
    }
}"));
        }

        [Fact]
        public void LambdaImplicitlyTyped()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
class Program {
    public static string Test(string input) {
        Func<string, string> f = input => input;
        return f(input);
    }
}"));
        }

        [Fact]
        public void LambdaExplicitlyTyped()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
class Program {
    public static string Test(string input) {
        Func<string, string> f = (string input) => input;
        return f(input);
    }
}"));
        }

        [Fact]
        public void YieldReturn()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
using System.Collections.Generic;
class Program {
    public static string Test(string input) {
        return Generator(input).Single();
    }
    static IEnumerable<string> Generator(string input) {
        yield return input;
    }
}"));
        }

        [Fact]
        public void ForeachArrayExplicitlyTyped()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
using System.Collections.Generic;
class Program {
    public static string Test(string input) {
        foreach (string x in new string[] { input }) {
            return x;
        }
        return string.Empty;
    }
}"));
        }

        [Fact]
        public void ForeachArrayImplicitlyTyped()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
using System.Collections.Generic;
class Program {
    public static string Test(string input) {
        foreach (var x in new[] { input }) {
            return x;
        }
        return string.Empty;
    }
}"));
        }

        [Fact]
        public void ForeachArrayImplicitConversionInLoopVarAssign()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
using System.Collections.Generic;
class Program {
    public static string Test(string input) {
        string[] arr = { input };
        foreach (IEnumerable<string> x in new string[][] { arr }) {
            foreach (string y in x)
                return y;
        }
        return string.Empty;
    }
}"));
        }

        [Fact]
        public void ForeachYieldReturn()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
using System.Collections.Generic;
class Program {
    public static string Test(string input) {
        foreach (var x in Generator(input))
            return x;
        return string.Empty;
    }
    static IEnumerable<string> Generator(string input) {
        yield return input;
    }
}"));
        }

        [Fact]
        public void ForeachCustomType()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
using System.Collections.Generic;
class Program {
    public static string Test(string input) {
        foreach (string x in new MyEnumerable(input)) {
            return x;
        }
        return string.Empty;
    }
}
class MyEnumerable
{
    string x;
    public MyEnumerable(string x) { this.x = x; }
    public MyEnumerator GetEnumerator() => new MyEnumerator(x);
}
class MyEnumerator : IEnumerator<string>
{
    string x;
    public MyEnumerator(string x) { this.x = x; }
    public string Current => x;
    public bool MoveNext() => true;
    public void Reset() {}
    public void Dispose() {}
    object System.Collections.IEnumerator.Current => x;
}
"));
        }

        [Fact]
        public void ArrayAsIList()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Collections.Generic;
class Program {
    public static string Test(string input) {
        var arr = new string[1];
        arr[0] = input;
        IList<string> list = arr;
        return list.GetEnumerator().Current;
    }
}"));
        }

        [Fact]
        public void NullableGenericStruct()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Collections.Generic;
struct Wrap<T> {
    public T Inner;
    public Wrap(T inner) { Inner = inner; }
}
class Program {
    public static string Test(string input) {
        Wrap<string>? wrapped = new Wrap<string>(input);
        return wrapped.Value.Inner;
    }
}"));
        }

        [Fact]
        public void Conditional1()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Collections.Generic;
class Program {
    static bool b;
    public static string Test(string input) {
        return b ? input : string.Empty;
    }
}"));
        }

        [Fact]
        public void Conditional2()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Collections.Generic;
class Program {
    static bool b;
    public static string Test(string input) {
        return b ? string.Empty : input;
    }
}"));
        }

        [Fact]
        public void MethodCallThroughInterface()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Collections.Generic;
interface I {
    string Identity(string input);
}
class Program : I {
    static I instance = new Program();
    public static string Test(string input) {
        return instance.Identity(input);
    }
    public string Identity(string input) => input;
}"));
        }

        [Fact]
        public void MethodCallThroughBaseClass()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Collections.Generic;
abstract class BaseClass {
    public abstract string Identity(string input);
}
class Program : BaseClass {
    static BaseClass instance = new Program();
    public static string Test(string input) {
        return instance.Identity(input);
    }
    public override string Identity(string input) => input;
}"));
        }

        [Fact]
        public void CustomDelegate()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
delegate string MyDelegate(string input);
class Program {
    public static string Test(string input) {
        MyDelegate f = delegate(string input) { return input; };
        return f(input);
    }
}"));
        }

        [Fact]
        public void LinqCallChain()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
class Program {
    public string Test(string input) {
        object[] o = { input };
        return (string)o.TakeWhile(obj => obj != this).FirstOrDefault(obj => obj is string);
    }
}"));
        }

        [Fact]
        public void AnonymousObject()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
class Program {
    public string Test(string input) {
        var obj = new { input, other=42 };
        return obj.input;
    }
}"));
        }

        [Fact]
        public void PropertyPattern()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System;
using System.Linq;
class Box {
    public string Value { get; }
    public Box(string val) { this.Value = val; }
}
class Program {
    public string Test(string input) {
        object obj = new Box(input);
        if (obj is Box { Value: var x })
            return x;
        else
            return string.Empty;
    }
}"));
        }

        [Fact]
        public void GenericImplicitOperator()
        {
            Assert.True(HasPathFromParameterToReturnType(@"
using System.Threading.Tasks;
static class Program {
    public static string Test(string input) {
        return new Box<string>(input);
    }
}
struct Box<T> {
    T val;
    public Box(T val) { this.val = val; }
    public static implicit operator T(Box<T> b) { return b.val; }
}"));
        }

        [Fact]
        public void SwitchExpression()
        {
            CheckPaths(@"
class Program {
    static int i;
    public static string Test(string input) {
        return i switch {
            0 => input,
            1 => string.Empty,
            _ => null
        };
    }
}", returnNullable: true, returnDependsOnInput: true);
        }

        [Fact]
        public void Unbox()
        {
            CheckPaths(@"
using System;
using System.Linq;
using System.Collections.Generic;
class Program {
    public static int Test(object input) {
        return (int)input;
    }
}", inputMustBeNonNull: true);
        }

        [Fact]
        public void UnboxNullable()
        {
            CheckPaths(@"
using System;
using System.Linq;
using System.Collections.Generic;
class Program {
    public static int? Test(object input) {
        return (int?)input;
    }
}", inputMustBeNonNull: false);
        }
    }
}
