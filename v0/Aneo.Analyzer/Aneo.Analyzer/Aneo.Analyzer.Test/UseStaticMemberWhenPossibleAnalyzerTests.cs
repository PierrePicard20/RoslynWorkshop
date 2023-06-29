using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Aneo.Analyzer.Test.CSharpCodeFixVerifier<
    Aneo.Analyzer.UseStaticMemberWhenPossibleAnalyzer,
    Aneo.Analyzer.UseStaticMemberWhenPossibleFixProvider>;

namespace Aneo.Analyzer.Test
{
    [TestClass]
    public class UseStaticMemberWhenPossibleAnalyzerTests
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"using System;
            class A
            {
                public void {|#0:foo|}()
                {
                    Console.WriteLine();
                }
            }";

            var expected = VerifyCS.Diagnostic(UseStaticMemberWhenPossibleAnalyzer.DiagnosticId)
                                   .WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task TestMethod3()
        {
            var test = @"using System;
            class A
            {
                int x;
	            public void foo()
	            {
                    x=0;
                }
            }

            class B
            {
	            private A a = new A();
	            public void bar()
	            {
		            a.foo();	// instance member access
	            }
            }
            ";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod4()
        {
            var test = @"using System;
            class A
            {
                int x;
	            public void foo()
	            {
                    x=0;
                }
            }

            class B
            {
	            private A a = new A();
	            public void bar()
	            {
		            foo().a.foo();
	            }
	            public B foo()
	            {
                    return this;
                }
            }
            ";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod5()
        {
            var test = @"using System;
            class A
            {
                static int x;
	            public static void foo()
	            {
                    x=0;
                }
            }
            ";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod6()
        {
            var test = @"
            class B
            {
                public int x;
            }
            class A : B
            {
	            public void foo()
	            {
                    x=0;
                }
            }
            ";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
