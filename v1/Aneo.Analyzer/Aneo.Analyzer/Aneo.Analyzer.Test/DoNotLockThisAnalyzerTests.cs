using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Aneo.Analyzer.Test.CSharpCodeFixVerifier<
    Aneo.Analyzer.DoNotLockThisAnalyzer,
    Aneo.Analyzer.DoNotLockThisCodeFixProvider>;

namespace Aneo.Analyzer.Test
{
    [TestClass]
    public class DoNotLockThisAnalyzerTests
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
                class A
                {
                    void foo()
                    {
                        lock ({|#0:this|})
                        {
                        }
                    }
                }
            ";

            var fixtest = @"
                class A
                {
                    private object _locker = new object();
                    void foo()
                    {
                        lock (_locker)
                        {
                        }
                    }
                }
            ";

            var expected = VerifyCS.Diagnostic(DoNotLockThisAnalyzer.DiagnosticId)
                                   .WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task TestMethod3()
        {
            var test = @"
                class A
                {
                    private object _locker = new object();
                    void foo()
                    {
                        lock ({|#0:this|})
                        {
                        }
                    }
                }
            ";

            var fixtest = @"
                class A
                {
                    private object _locker = new object();
                    void foo()
                    {
                        lock (_locker)
                        {
                        }
                    }
                }
            ";

            var expected = VerifyCS.Diagnostic(DoNotLockThisAnalyzer.DiagnosticId)
                                   .WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task TestMethod4()
        {
            var test = @"
                using System.Threading;
                class A
                {
                    void foo()
                    {
                        Monitor.Enter({|#0:this|});
                    }
                }
            ";

            var fixtest = @"
                using System.Threading;
                class A
                {
                    private object _locker = new object();
                    void foo()
                    {
                        Monitor.Enter(_locker);
                    }
                }
            ";

            var expected = VerifyCS.Diagnostic(DoNotLockThisAnalyzer.DiagnosticId)
                .WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }

        [TestMethod]
        public async Task TestMethod5()
        {
            var test = @"
                using System.Threading;
                class A
                {
                    void foo()
                    {
                        Monitor.Exit({|#0:this|});
                    }
                }
            ";

            var fixtest = @"
                using System.Threading;
                class A
                {
                    private object _locker = new object();
                    void foo()
                    {
                        Monitor.Exit(_locker);
                    }
                }
            ";

            var expected = VerifyCS.Diagnostic(DoNotLockThisAnalyzer.DiagnosticId)
                .WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
